// Parses a full cdacstress log file and for each verification point
// (PASS/FAIL/DAC_MISMATCH) compares cDAC vs DAC refs side by side.
// Shows only points where they don't fully match.
// Run with: dotnet run <logfile>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

var file = args.Length > 0 ? args[0] : @"C:\Users\maxcharlamb\AppData\Local\Temp\cdac-ba-detail.txt";
var lines = File.ReadAllLines(file);

// Parse all verification points
var points = new List<VerifyPoint>();
VerifyPoint? current = null;

foreach (var line in lines)
{
    var trimmed = line.Trim();
    if (trimmed.StartsWith("[PASS]") || trimmed.StartsWith("[FAIL]") || trimmed.StartsWith("[DAC_MISMATCH]"))
    {
        current = new VerifyPoint { Header = trimmed };
        points.Add(current);
        continue;
    }
    if (current is null) continue;

    if (TryParseRef(trimmed, "cDAC [", out var cr))
        current.CdacRefs.Add(cr);
    else if (TryParseRef(trimmed, "DAC  [", out var dr))
        current.DacRefs.Add(dr);
    else if (TryParseRef(trimmed, "RT   [", out var rr))
        current.RtRefs.Add(rr);
}

int totalPoints = points.Count;
int passPoints = points.Count(p => p.Header.StartsWith("[PASS]"));
int failPoints = points.Count(p => p.Header.StartsWith("[FAIL]"));
int mismatchPoints = points.Count(p => p.Header.StartsWith("[DAC_MISMATCH]"));

Console.WriteLine($"Total verification points: {totalPoints}");
Console.WriteLine($"  PASS: {passPoints}, FAIL: {failPoints}, DAC_MISMATCH: {mismatchPoints}");
Console.WriteLine();

// For each non-PASS point, do detailed comparison
foreach (var point in points.Where(p => !p.Header.StartsWith("[PASS]")))
{
    Console.WriteLine($"--- {point.Header} ---");
    Console.WriteLine($"  cDAC: {point.CdacRefs.Count} refs, DAC: {point.DacRefs.Count} refs, RT: {point.RtRefs.Count} refs");
    
    if (point.DacRefs.Count == 0)
    {
        Console.WriteLine("  (No DAC refs to compare)");
        Console.WriteLine();
        continue;
    }

    // Match cDAC to DAC by (Address, Object, Flags) for stack refs,
    // then by (Object, Flags) for register refs (Address=0)
    var dacUsed = new bool[point.DacRefs.Count];
    var cdacUsed = new bool[point.CdacRefs.Count];

    // Phase 1: exact (Address, Object, Flags) for Address != 0
    for (int i = 0; i < point.CdacRefs.Count; i++)
    {
        if (point.CdacRefs[i].Address == 0) continue;
        for (int j = 0; j < point.DacRefs.Count; j++)
        {
            if (dacUsed[j]) continue;
            if (point.CdacRefs[i].Address == point.DacRefs[j].Address &&
                point.CdacRefs[i].Object == point.DacRefs[j].Object &&
                point.CdacRefs[i].Flags == point.DacRefs[j].Flags)
            {
                cdacUsed[i] = true;
                dacUsed[j] = true;
                break;
            }
        }
    }

    // Phase 2: (Object, Flags) for Address=0 refs
    for (int i = 0; i < point.CdacRefs.Count; i++)
    {
        if (cdacUsed[i]) continue;
        for (int j = 0; j < point.DacRefs.Count; j++)
        {
            if (dacUsed[j]) continue;
            if (point.CdacRefs[i].Object == point.DacRefs[j].Object &&
                point.CdacRefs[i].Flags == point.DacRefs[j].Flags)
            {
                cdacUsed[i] = true;
                dacUsed[j] = true;
                break;
            }
        }
    }

    var cdacUnmatched = new List<int>();
    var dacUnmatched = new List<int>();
    for (int i = 0; i < point.CdacRefs.Count; i++)
        if (!cdacUsed[i]) cdacUnmatched.Add(i);
    for (int j = 0; j < point.DacRefs.Count; j++)
        if (!dacUsed[j]) dacUnmatched.Add(j);

    if (cdacUnmatched.Count == 0 && dacUnmatched.Count == 0)
    {
        Console.WriteLine("  All refs matched!");
    }
    else
    {
        Console.WriteLine($"  Unmatched: {cdacUnmatched.Count} cDAC-only, {dacUnmatched.Count} DAC-only");
        
        if (cdacUnmatched.Count > 0)
        {
            Console.WriteLine("  cDAC-only refs:");
            foreach (var i in cdacUnmatched)
            {
                var r = point.CdacRefs[i];
                Console.WriteLine($"    [{i}] Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags=0x{r.Flags:X} Src=0x{r.Source:X} SP=0x{r.SP:X}");
            }
        }
        if (dacUnmatched.Count > 0)
        {
            Console.WriteLine("  DAC-only refs:");
            foreach (var j in dacUnmatched)
            {
                var r = point.DacRefs[j];
                Console.WriteLine($"    [{j}] Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags=0x{r.Flags:X} Src=0x{r.Source:X}");
            }
        }
        
        // Per-frame comparison
        Console.WriteLine();
        Console.WriteLine("  === Per-frame comparison ===");
        
        var cdacFrames = point.CdacRefs
            .Select((r, i) => (r, i))
            .GroupBy(x => x.r.Source)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.r, x.i)).ToList());
        
        var dacFrames = point.DacRefs
            .Select((r, i) => (r, i))
            .GroupBy(x => x.r.Source)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.r, x.i)).ToList());
        
        var allSources = cdacFrames.Keys.Union(dacFrames.Keys).OrderBy(x => x).ToList();
        
        foreach (var src in allSources)
        {
            bool hasCdac = cdacFrames.TryGetValue(src, out var cdacForFrame);
            bool hasDac = dacFrames.TryGetValue(src, out var dacForFrame);
            int cc = hasCdac ? cdacForFrame!.Count : 0;
            int dc = hasDac ? dacForFrame!.Count : 0;
            
            string label;
            if (!hasCdac) label = "DAC_ONLY";
            else if (!hasDac) label = "CDAC_ONLY";
            else if (cc == dc) label = "MATCH_COUNT";
            else label = "DIFF_COUNT";
            
            // Check if Source looks like a Frame address (stack addr) vs managed IP
            bool isFrameSource = src < 0x7FF000000000;
            string srcType = isFrameSource ? "Frame" : "IP";
            
            Console.WriteLine($"  Src=0x{src:X} ({srcType}): cDAC={cc} DAC={dc} [{label}]");
            
            // For DIFF or ONLY cases, show the refs
            if (label != "MATCH_COUNT")
            {
                if (hasCdac)
                {
                    foreach (var (r, idx) in cdacForFrame!)
                        Console.WriteLine($"    cDAC[{idx}] Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags=0x{r.Flags:X}");
                }
                if (hasDac)
                {
                    foreach (var (r, idx) in dacForFrame!)
                        Console.WriteLine($"    DAC [{idx}] Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags=0x{r.Flags:X}");
                }
            }
        }
    }
    Console.WriteLine();
}

static bool TryParseRef(string line, string prefix, out Ref r)
{
    r = default;
    if (!line.StartsWith(prefix)) return false;

    var m = Regex.Match(line, @"Address=0x(\w+)\s+Object=0x(\w+)\s+Flags=0x(\w+)\s+Source=0x(\w+)");
    if (!m.Success) return false;

    r.Address = ulong.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
    r.Object = ulong.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
    r.Flags = uint.Parse(m.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
    r.Source = ulong.Parse(m.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);

    var spMatch = Regex.Match(line, @"SP=0x(\w+)");
    if (spMatch.Success)
        r.SP = ulong.Parse(spMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);

    return true;
}

struct Ref
{
    public ulong Address;
    public ulong Object;
    public uint Flags;
    public ulong Source;
    public ulong SP;
}

class VerifyPoint
{
    public string Header = "";
    public List<Ref> CdacRefs = new();
    public List<Ref> DacRefs = new();
    public List<Ref> RtRefs = new();
}
