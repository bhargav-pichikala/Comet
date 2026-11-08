namespace Comet.Core;

public sealed record PendingInsertion(long Token, Suggestion Item, ReplacementPlan Plan);

// Owned by the input thread. Completions may clear only their own operation, never a newer query.
public sealed class AutocompleteSession
{
    readonly ShortcutParser parser = new();
    long operation;
    public long Revision => parser.Revision;
    public string Candidate => parser.Candidate;
    public bool Active => parser.Active;
    public bool Inserting { get; private set; }
    public bool Visible { get; private set; }
    public IReadOnlyList<Suggestion> Results { get; private set; } = [];
    public int Selected { get; private set; }
    public string Query(string prefix) => parser.Query(prefix);
    public void Reset()
    {
        operation++; Inserting = false; parser.Reset(); ClearResults();
    }
    void ClearResults() { Results = []; Selected = 0; Visible = false; }
    public void Feed(char c, string prefix) { parser.Feed(c, prefix); ClearResults(); }
    public void Backspace() { parser.Backspace(); ClearResults(); }
    public bool ApplyResults(long revision, IReadOnlyList<Suggestion> results)
    {
        if (revision != Revision || !Active) return false;
        Results = results.ToArray(); Selected = 0; Visible = false; return true;
    }
    public void SetVisible(long revision, bool visible) { if (revision == Revision && Active) Visible = visible; }
    public void Move(int delta) => Selected = Navigation.Move(Selected, delta, Results.Count + 1);
    public PendingInsertion? BeginInsertion(int index, string prefix)
    {
        if (!Visible || !Active || index < 0 || index >= Results.Count) return null;
        var item = Results[index]; var plan = ReplacementPlan.Create(Candidate, prefix, item.Text);
        parser.Reset(); ClearResults(); Inserting = true;
        return new(++operation, item, plan);
    }
    public bool CompleteInsertion(long token)
    {
        if (token != operation) return false;
        Inserting = false;
        // Do not reset the parser here: the user may already be typing the next shortcut.
        return true;
    }
}

// Key suppression survives session cancellation until the corresponding physical key-up.
public sealed class KeySuppression
{
    readonly HashSet<uint> held = [];
    public bool IsSuppressed(uint key) => held.Contains(key);
    public void Down(uint key) => held.Add(key);
    public bool Up(uint key) => held.Remove(key);
}

public static class InjectedInputPolicy
{
    public const nuint Tag = 0x434F4D45;
    public static bool Ignore(uint flags, nuint extra, bool testMode) =>
        (flags & 0x10) != 0 && (!testMode || extra == Tag);
}

// UI-owned animation epochs make rapid dismiss/reopen safe without delayed Hide callbacks.
public sealed class PopupTransitionState
{
    long version;
    public bool Requested { get; private set; }
    public bool FadingOut { get; private set; }
    public bool Show()
    {
        bool restoreOpacity=FadingOut;
        Requested=true;FadingOut=false;version++;
        return restoreOpacity;
    }
    public long Hide() { Requested=false;FadingOut=true;return ++version; }
    public bool CanFinishHide(long epoch) => epoch==version && !Requested && FadingOut;
}

public static class FieldEligibility
{
    // An explicit read-only value provider wins over a more permissive text-pattern fallback.
    public static bool CanEdit(bool hasValue, bool valueReadOnly, bool hasText, bool? textReadOnly) =>
        hasValue ? !valueReadOnly : hasText && textReadOnly == false;
}
