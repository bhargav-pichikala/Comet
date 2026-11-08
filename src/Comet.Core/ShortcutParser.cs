namespace Comet.Core;
// Retains only an ASCII shortcode candidate. Ordinary text is never buffered.
public sealed class ShortcutParser
{
    public string Candidate { get; private set; } = "";
    public long Revision { get; private set; }
    public bool Active => Candidate.Length > 0;
    private bool boundary = true;
    public static bool IsQueryChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '+';
    public void Reset(bool atBoundary = true) { Candidate = ""; boundary = atBoundary; Revision++; }
    public void Feed(char c, string prefix)
    {
        Revision++;
        if (!Active)
        {
            if (c == prefix[0] && boundary) Candidate = c.ToString();
            boundary = char.IsWhiteSpace(c) || c is '(' or '[' or '{'; return;
        }
        if (Candidate.Length < prefix.Length)
        {
            if (c == prefix[Candidate.Length]) Candidate += c;
            else Reset(char.IsWhiteSpace(c));
            return;
        }
        if (IsQueryChar(c) && Candidate.Length < prefix.Length + 64) Candidate += c;
        else Reset(char.IsWhiteSpace(c));
    }
    public void Backspace()
    {
        Revision++;
        if (Active) Candidate = Candidate[..^1];
        else boundary = false;
    }
    public string Query(string prefix) => Candidate.StartsWith(prefix, StringComparison.Ordinal) ? Candidate[prefix.Length..] : "";
}
public static class Navigation
{
    public static int Move(int current, int delta, int count) => count == 0 ? -1 : ((current + delta) % count + count) % count;
}
public sealed record ReplacementPlan(int DeleteCount, string Text, string Expected = "")
{
    public static ReplacementPlan Create(string candidate, string prefix, string text)
    {
        if (!candidate.StartsWith(prefix, StringComparison.Ordinal) || candidate.Length <= prefix.Length || !candidate[prefix.Length..].All(ShortcutParser.IsQueryChar)) throw new ArgumentException("Unsafe replacement candidate.");
        if (text.Length is < 1 or > 16000) throw new ArgumentException("Invalid insertion length.");
        return new(candidate.Length, text, candidate);
    }
    public string Apply(string before, int caret)
    {
        if (caret < DeleteCount || caret > before.Length) throw new ArgumentOutOfRangeException(nameof(caret));
        return before[..(caret - DeleteCount)] + Text + before[caret..];
    }
}
