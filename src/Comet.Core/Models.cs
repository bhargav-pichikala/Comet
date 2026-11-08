using System.Text.Json;
namespace Comet.Core;
public sealed record Emoji(string Id, string Name, string Text, string[] Aliases, string Category, string[] Keywords, int Tone, string BaseId);
public sealed record Expansion(string Shortcut, string Text, string Kind = "Emoji");
public sealed record GifItem(string Name, string Path);
public sealed record Suggestion(string Id, string Name, string Text, string Category, string Shortcut)
{
    public string Display => $"{Text.Replace("\r", "").Replace("\n", " ↵ ")}   {Name}";
}
public sealed class Settings
{
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public string Trigger { get; set; } = ":";
    public bool DoubleTrigger { get; set; }
    public bool StartAtLogin { get; set; }
    public string Theme { get; set; } = "System";
    public int MinQuery { get; set; } = 2;
    public int MaxResults { get; set; } = 7;
    public int SkinTone { get; set; }
    public bool TabConfirms { get; set; } = true;
    public bool ClosingTriggerConfirms { get; set; } = true;
    public bool Fuzzy { get; set; } = true;
    public bool LeaveOnClipboard { get; set; }
    public string PickerHotkey { get; set; } = "Ctrl+Shift+Space";
    public List<string> Exclusions { get; set; } = ["keepass", "keepassxc", "1password", "bitwarden", "credentialuibroker", "mstsc"];
    public List<Expansion> Expansions { get; set; } = [];
    public List<GifItem> Gifs { get; set; } = [];
    public HashSet<string> Favorites { get; set; } = [];
    public List<string> Recent { get; set; } = [];
    public Dictionary<string, int> Frequency { get; set; } = [];
    public void Validate()
    {
        if (Version != 1) throw new ArgumentException("Unsupported settings version.");
        if (Trigger.Length != 1 || Trigger[0] > 127 || char.IsLetterOrDigit(Trigger[0]) || char.IsWhiteSpace(Trigger[0])) throw new ArgumentException("Trigger must be one ASCII punctuation character.");
        if (MinQuery is < 1 or > 8 || MaxResults is < 3 or > 12 || SkinTone is < 0 or > 5) throw new ArgumentException("Invalid popup or skin-tone setting.");
        if (Theme is not ("System" or "Light" or "Dark")) throw new ArgumentException("Invalid theme.");
        if (PickerHotkey is not ("Ctrl+Shift+Space" or "Ctrl+Alt+E" or "Alt+Shift+E")) throw new ArgumentException("Unsupported hotkey.");
        if (Expansions.Count > 10000 || Gifs.Count > 1000) throw new ArgumentException("Library is too large.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in Expansions)
        {
            if (e.Shortcut.Length is < 1 or > 64 || !e.Shortcut.All(ShortcutParser.IsQueryChar) || e.Text.Length is < 1 or > 16000 || e.Kind is not ("Emoji" or "Snippet")) throw new ArgumentException("Invalid shortcut, type, or expansion length.");
            if (!names.Add(e.Shortcut)) throw new ArgumentException("Duplicate shortcut: " + e.Shortcut);
        }
        Exclusions = Exclusions.Select(ApplicationExclusions.Normalize).Where(x => x.Length > 0).Distinct().ToList();
        Recent = Recent.Distinct().Take(50).ToList();
    }
    public string Prefix => DoubleTrigger ? Trigger + Trigger : Trigger;
    public void Used(string id)
    {
        Recent.Remove(id); Recent.Insert(0, id); Recent = Recent.Take(50).ToList();
        Frequency[id] = Math.Min(Frequency.GetValueOrDefault(id) + 1, 1000000);
    }
}
public static class ApplicationExclusions
{
    public static string Normalize(string name) => System.IO.Path.GetFileNameWithoutExtension(name.Trim()).ToLowerInvariant();
    public static bool Contains(IEnumerable<string> names, string process) => names.Any(n => Normalize(n) == Normalize(process));
}
public sealed class SettingsManager(string path)
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public Settings Load()
    {
        if (!File.Exists(path)) return new();
        return Read(File.ReadAllText(path));
    }
    public static Settings Read(string text)
    {
        if (text.Length > 4_000_000) throw new ArgumentException("Settings file exceeds 4 MB.");
        var s = JsonSerializer.Deserialize<Settings>(text, Json) ?? throw new ArgumentException("Empty settings file.");
        s.Validate(); return s;
    }
    public void Save(Settings s)
    {
        s.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        using (var f = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        { JsonSerializer.Serialize(f, s, Json); f.Flush(true); }
        File.Move(temp, path, true);
    }
    public static Settings Clone(Settings s) => Read(JsonSerializer.Serialize(s, Json));
}
