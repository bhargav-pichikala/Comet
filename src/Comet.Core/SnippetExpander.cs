using System.Globalization;
using System.Text.RegularExpressions;
namespace Comet.Core;
public static partial class SnippetExpander
{
    [GeneratedRegex(@"\{\{\s*([a-z_0-9]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Variable();
    public static string Expand(string text,DateTimeOffset now,CultureInfo? culture=null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return Variable().Replace(text,m=>m.Groups[1].Value switch
        {
            "date_short"=>now.ToString("d",culture),
            "date_medium"=>now.ToString("dd MMM yyyy",culture),
            "date_long" or "date_full" or "best_date"=>now.ToString("D",culture),
            "time_short" or "best_time"=>now.ToString("t",culture),
            "time_medium" or "time_long"=>now.ToString("T",culture),
            "time_full"=>now.ToString("T",culture)+" "+now.ToString("zzz",culture),
            "time_24h"=>now.ToString("HH:mm",CultureInfo.InvariantCulture),
            "date_time_iso"=>now.ToString("O",CultureInfo.InvariantCulture),
            _=>m.Value
        });
    }
}
