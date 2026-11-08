using System.Reflection;
using System.Text.Json;
namespace Comet.Core;
public sealed class EmojiSearchEngine
{
    public IReadOnlyList<Emoji> Emojis { get; }
    readonly Dictionary<string, HashSet<int>> grams = new(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<int>> prefixes = new(StringComparer.Ordinal);
    readonly string[][] terms;
    readonly HashSet<(string, int)> variants;
    readonly Dictionary<string, IReadOnlyList<Suggestion>> cache = new();
    public EmojiSearchEngine()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Comet.Core.emoji.json")!;
        Emojis = JsonSerializer.Deserialize<List<Emoji>>(stream)!;
        variants = Emojis.Where(e => e.Tone > 0).Select(e => (e.BaseId, e.Tone)).ToHashSet();
        terms = new string[Emojis.Count][];
        for (int i = 0; i < Emojis.Count; i++)
        {
            var e = Emojis[i];
            terms[i] = e.Aliases.Concat(e.Keywords).Append(e.Name).Select(Norm).Distinct().ToArray();
            foreach (var term in terms[i])
            {
                foreach (var word in term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Append(term))
                {
                    for (int n = 1; n <= word.Length; n++) Add(prefixes, word[..n], i);
                    for (int n = 0; n < word.Length - 1; n++) Add(grams, word.Substring(n, 2), i);
                }
            }
        }
    }
    static void Add(Dictionary<string, HashSet<int>> index, string key, int i)
    { if (!index.TryGetValue(key, out var set)) index[key] = set = []; set.Add(i); }
    public static string Norm(string s) => s.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
    public void Invalidate() => cache.Clear();
    public IReadOnlyList<Suggestion> Search(string query, Settings s, int limit = 7, string category = "All")
    {
        var q = Norm(query); var key = $"{q}|{limit}|{category}|{s.SkinTone}|{s.Fuzzy}";
        if (cache.TryGetValue(key, out var cached)) return cached;
        var found = new Dictionary<string, (Suggestion Item, int Score)>();
        foreach (var ex in s.Expansions)
        {
            int score = Score(Norm(ex.Shortcut), q, s.Fuzzy);
            if (q.Length == 0 || score > 0) found["custom:" + ex.Shortcut] = (new("custom:" + ex.Shortcut, ex.Shortcut, ex.Text, ex.Kind, ex.Shortcut), score + 30);
        }
        var ids = new HashSet<int>();
        if (q.Length == 0) ids.UnionWith(Enumerable.Range(0, Emojis.Count));
        else
        {
            if (prefixes.TryGetValue(q, out var set)) ids.UnionWith(set);
            var hits = new Dictionary<int, int>();
            for (int n = 0; n < q.Length - 1; n++)
                if (grams.TryGetValue(q.Substring(n, 2), out var matches)) foreach (int id in matches) hits[id] = hits.GetValueOrDefault(id) + 1;
            ids.UnionWith(hits.OrderByDescending(x => x.Value).Take(500).Select(x => x.Key));
        }
        foreach (var i in ids)
        {
            var e = Emojis[i];
            if (category != "All" && category != "Recent" && category != "Favorites" && category != "Frequent" && e.Category != category) continue;
            if (category == "Recent" && !s.Recent.Contains(e.Id) || category == "Favorites" && !s.Favorites.Contains(e.Id)) continue;
            if (s.SkinTone != e.Tone && !(e.Tone == 0 && !variants.Contains((e.Id, s.SkinTone)))) continue;
            int score = q.Length == 0 ? 1 : terms[i].Max(t => Score(t, q, s.Fuzzy));
            if (score <= 0) continue;
            score += Math.Min(s.Frequency.GetValueOrDefault(e.Id), 15) + (s.Favorites.Contains(e.Id) ? 8 : 0);
            if (category == "Recent") score += 100 - s.Recent.IndexOf(e.Id);
            if (category == "Frequent") score += s.Frequency.GetValueOrDefault(e.Id);
            found[e.Id] = (new(e.Id, e.Name, e.Text, e.Category, e.Aliases[0]), score);
        }
        var result = found.Values.Where(x => category is "All" or "Recent" or "Favorites" or "Frequent" || x.Item.Category == category)
            .Where(x => category != "Favorites" || s.Favorites.Contains(x.Item.Id))
            .Where(x => category != "Recent" || s.Recent.Contains(x.Item.Id))
            .OrderByDescending(x => x.Score).ThenBy(x => x.Item.Name).Take(limit).Select(x => x.Item).ToList();
        if (cache.Count >= 256) cache.Clear(); cache[key] = result; return result;
    }
    static int Score(string term, string q, bool fuzzy)
    {
        if (term == q) return 120;
        if (term.StartsWith(q, StringComparison.Ordinal)) return 90;
        if (term.Contains(q, StringComparison.Ordinal)) return 65;
        if (!fuzzy || q.Length < 3) return 0;
        if (Math.Abs(term.Length - q.Length) <= 1 && Distance(term, q) <= 1) return 45;
        int at = 0; foreach (char c in term) if (at < q.Length && q[at] == c) at++;
        return at == q.Length && term.Length <= q.Length * 2 ? 20 : 0;
    }
    public static int Distance(string a, string b)
    {
        var prev = Enumerable.Range(0, b.Length + 1).ToArray();
        for (int i = 1; i <= a.Length; i++)
        {
            var next = new int[b.Length + 1]; next[0] = i;
            for (int j = 1; j <= b.Length; j++) next[j] = Math.Min(Math.Min(next[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            prev = next;
        }
        return prev[b.Length];
    }
}
