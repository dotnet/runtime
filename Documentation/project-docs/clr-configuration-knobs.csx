// This script generates documentation about the various configuration options that
// are available and how to use them.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

char dirSep = Path.DirectorySeparatorChar;
var clrConfig = $"..{dirSep}..{dirSep}src{dirSep}inc{dirSep}clrconfigvalues.h";
var jitConfig = $"..{dirSep}..{dirSep}src{dirSep}jit{dirSep}jitconfigvalues.h";
Console.Out.NewLine = "\n";
Console.Error.NewLine = "\n";

public struct Knob
{
    private string _name;
    private string _class;

    public bool Retail;
    public string Category;
    public string Type;
    public string DefaultValue;
    public string Description;
    public string Flags;
    public int Line;
    public string File;

    public string Name
    {
        get => _name;
        set
        {
            value = value ?? String.Empty;
            int indexOfUnderscore = value.IndexOf("_");
            if (indexOfUnderscore > 0 && (value.Contains("INTERNAL") || value.Contains("EXTERNAL") || value.Contains("UNSUPPORTED")))
            {
                _name = value.Substring(indexOfUnderscore + 1);
            }
            else
            {
                _name = value;
            }
        }
    }

    public string Class
    {
        get => _class;
        set
        {
            value = value ?? String.Empty;
            int indexOfUnderscore = value.IndexOf("_");
            if (indexOfUnderscore > 0 && (value.Contains("INTERNAL") || value.Contains("EXTERNAL") || value.Contains("UNSUPPORTED")))
            {
                _class = value.Substring(0, indexOfUnderscore);
            }
            else
            {
                _class = value;
            }
        }
    }

    public string Location => $"{File}:{Line}";

    private const string StringType = "STRING";
    private const string DWORD_Type = "DWORD";
    private const string SpaceSeparatedValues = "SSV";

    private static Regex s_cppString = new Regex("\"([^\"\\\\]|\\\\.)*\"", RegexOptions.Compiled);

    enum Fields
    {
        Unknown = 0,
        Symbol,
        Name,
        DefaultValue,
        Description,
        LookupOptions
    }

    public Knob(string line, bool isRetail, bool isClrConfigFile, StreamReader reader, ref int lineNum, out string nextLine)
    {
        nextLine = null;

        _name = String.Empty;
        _class = String.Empty;

        Retail = isRetail;
        Category = String.Empty;
        Type = String.Empty;
        DefaultValue = String.Empty;
        Description = String.Empty;
        Flags = String.Empty;
        Line = -1;
        File = isClrConfigFile ? "clrconfigvalues.h" : "jitconfigvalues.h";

        string[] parts0 = null;
        string[] parts1 = null;
        string[] parts2 = null;

        // Split on first open parenthesis
        parts0 = line.Split(new char[]{'('}, 2, StringSplitOptions.None);

        if (parts0.Length > 1)
        {
            Fields[] numParts1 = null;
            switch(parts0[0])
            {
                // CONFIG_DWORD_INFO(symbol, name, defaultValue, description)
                case "CONFIG_DWORD_INFO":
                case "RETAIL_CONFIG_DWORD_INFO":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.DefaultValue, Fields.Description };
                    parts1 = parts0[1].Split(new[] { ',' }, 4);
                    Type = DWORD_Type;
                    break;

                // CONFIG_DWORD_INFO_DIRECT_ACCESS(symbol, name, description)
                case "CONFIG_DWORD_INFO_DIRECT_ACCESS":
                case "RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.Description };
                    parts1 = parts0[1].Split(new[] { ',' }, 3);
                    Type = DWORD_Type;
                    break;

                // CONFIG_STRING_INFO(symbol, name, description)
                case "CONFIG_STRING_INFO":
                case "RETAIL_CONFIG_STRING_INFO":
                case "CONFIG_STRING_INFO_DIRECT_ACCESS":
                case "RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.Description };
                    parts1 = parts0[1].Split(new[] { ',' }, 3);
                    Type = StringType;
                    break;

                // CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions)
                case "CONFIG_DWORD_INFO_EX":
                case "RETAIL_CONFIG_DWORD_INFO_EX":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.DefaultValue, Fields.Description, Fields.LookupOptions };
                    parts1 = parts0[1].Split(new[] { ',' }, 4);
                    Type = DWORD_Type;
                    break;

                // CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions)
                case "CONFIG_STRING_INFO_EX":
                case "RETAIL_CONFIG_STRING_INFO_EX":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.Description, Fields.LookupOptions };
                    parts1 = parts0[1].Split(new[] { ',' }, 3);
                    Type = StringType;
                    break;

                // CONFIG_INTEGER(symbol, name, defaultValue) description in single line comments
                case "CONFIG_INTEGER":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name, Fields.DefaultValue };
                    parts1 = parts0[1].Split(new[] { ',' }, 3);
                    Type = DWORD_Type;
                    break;

                // CONFIG_STRING(symbol, name) description in single line comments
                case "CONFIG_STRING":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name };
                    parts1 = parts0[1].Split(new[] { ',' }, 2);
                    Type = StringType;
                    break;

                // CONFIG_METHODSET(symbol, name) description in single line comments
                case "CONFIG_METHODSET":
                    numParts1 = new Fields[] { Fields.Symbol, Fields.Name };
                    parts1 = parts0[1].Split(new[] { ',' }, 2);
                    Type = SpaceSeparatedValues;
                    break;

                default:
                    throw new ArgumentException($"Unsupported C++ macro definition: {parts0[0]}", nameof(line));
            }

            // Parse Description
            if (numParts1[parts1.Length - 1] == Fields.Description)
            {
                var descMatch = s_cppString.Match(parts1[parts1.Length - 1]);
                Description = descMatch.Value.Substring(1, descMatch.Length - 2);

                // Parse Flags if present
                if (parts1.Length < numParts1.Length && numParts1[parts1.Length] == Fields.LookupOptions)
                {
                    var tempFlags = parts1[parts1.Length - 1].Substring(descMatch.Index + descMatch.Length).TrimStart(' ', ',');
                    Flags = tempFlags.Replace("CLRConfig::", null).Replace(")", null).Replace("|", "\\|");
                }
            }

            // Parse Description in jitconfigvalues.h file which is in single line C++ comments
            else if (!isClrConfigFile)
            {
                int commentIndex = line.IndexOf(" // ");
                var description = commentIndex >= 0 ? line.Substring(commentIndex + 4) : String.Empty;
                if (description.Length > 0)
                {
                    nextLine = reader.ReadLine();
                    lineNum++;
                    while (nextLine != null)
                    {
                        var workLine = nextLine.Trim();
                        if (workLine.StartsWith("// "))
                        {
                            description += workLine.Substring(2).TrimEnd();
                            nextLine = reader.ReadLine();
                            lineNum++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                Description = description;
            }

            Description = Description.Replace("|", "\\|");

            // Parse DefaultValue
            int indexOfDefaultValue = -1;
            if ((indexOfDefaultValue = Array.IndexOf<Fields>(numParts1, Fields.DefaultValue)) >= 0 && indexOfDefaultValue < parts1.Length)
            {
                if (isClrConfigFile)
                {
                    DefaultValue = parts1[indexOfDefaultValue].TrimEnd(')');
                }
                else
                {
                    int indexOfCloseParenth = parts1[indexOfDefaultValue].IndexOf(")", StringComparison.Ordinal);
                    DefaultValue = parts1[indexOfDefaultValue].Substring(0, indexOfCloseParenth);
                }

                DefaultValue = DefaultValue.Trim();
            }

            // Parse Name
            var nameMatch = s_cppString.Match(parts1[1]);
            Name = nameMatch.Captures.Count > 0 ? nameMatch.Value.Substring(1, nameMatch.Length - 2) : String.Empty;

            // Parse Class
            if (parts1[0].Length > 0)
            {
                Class = Name.Length > 0 ? parts1[0].Replace(Name, null) : parts1[0];
                Class = Class.TrimEnd('_');
            }

            Retail = isRetail;

            if (Flags == null)
            {
                Flags = String.Empty;
            }

        }
    }

    public override string ToString()
    {
        return $"{{Knob: Name: {Name}, Category: {Category}, Retail: {Retail}, Class: {Class}, Type: {Type}, DefaultValue: {DefaultValue}, Description: {Description}, Flags: {Flags}, Location: {File}:{Line}}}";
    }
}

public static void ParseConfigFile(string filePath, bool isClrConfigFile, SortedDictionary<string, SortedDictionary<string, Knob>> categorizedKnobsDictionary)
{
    using (StreamReader clrReader = new StreamReader(filePath, new UTF8Encoding(false)))
    {
        SortedDictionary<string, Knob> knobsDictionary = null;
        string currentCategory = null;

        int lineNumber = 1;
        string line = clrReader.ReadLine();
        while (line != null)
        {
            bool isRetail = false;
            string nextLine = null;

            if (line.StartsWith("CONFIG_", StringComparison.Ordinal) || (isRetail = line.StartsWith("RETAIL_CONFIG_", StringComparison.Ordinal)))
            {
                var clrKnob = new Knob(line, isRetail, isClrConfigFile, clrReader, ref lineNumber, out nextLine);
                clrKnob.Category = currentCategory;
                clrKnob.Line = lineNumber;
                if (!knobsDictionary.ContainsKey(clrKnob.Name))
                {
                    knobsDictionary.Add(clrKnob.Name, clrKnob);
                }
                else
                {
                    Knob dupKnob = knobsDictionary[clrKnob.Name];
                    if (dupKnob.File != clrKnob.File && !dupKnob.Retail)
                    {
                        WriteLine($"Duplicate: {dupKnob.Location} {dupKnob.Name} -> {clrKnob.Location} {clrKnob.Name}");
                    }
                }
            }
            else if (line.StartsWith("///", StringComparison.Ordinal) && (line = line.Trim()).Length > 3)
            {
                line = line.Replace("///", null).Trim();
                if (!categorizedKnobsDictionary.ContainsKey(line))
                {
                    knobsDictionary = new SortedDictionary<string, Knob>();
                    categorizedKnobsDictionary.Add(line, knobsDictionary);
                    currentCategory = line;
                }
                else
                {
                    knobsDictionary = categorizedKnobsDictionary[line];
                    currentCategory = line;
                }
            }

            if (nextLine != null)
            {
                line = nextLine;
            }
            else
            {
                line = clrReader.ReadLine();
                lineNumber++;
            }
        }
    }
}

public static class ConfigKnobsDoc
{
    public static string IntroSection =
    "There are two primary ways to configure runtime behavior: CoreCLR hosts can pass in key-value string pairs during runtime initialization, or users can set special variables in the environment or registry. Today, the set of configuration options that can be set via the former method is relatively small, but moving forward, we expect to add more options there. Each set of options is described below.\n";

    public static string HostConfigurationKnobsPart1 =
        "## Host Configuration Knobs\nThese can be passed in by a host during initialization. Note that the values are all passed in as strings, so if the type is boolean, the value would be the string \"true\" or \"false\", and if it's a numeric value, it would be in the form \"123\".\n";

    public static string HostConfigurationKnobsPart2 =
        "\nName | Description | Type\n" +
        "-----|-------------|------\n" +
        "`System.GC.Concurrent` | Enable concurrent GC | boolean\n" +
        "`System.GC.Server` | Enable server GC | boolean\n" +
        "`System.GC.RetainVM` | Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS | boolean\n" +
        "`System.Runtime.TieredCompilation` | Enable tiered compilation | boolean\n" +
        "`System.Threading.ThreadPool.MinThreads` | Override MinThreads for the ThreadPool worker pool | numeric\n" +
        "`System.Threading.ThreadPool.MaxThreads` | Override MaxThreads for the ThreadPool worker pool | numeric\n\n\n";

    public static string ClrConfigSectionHeader =
        "## Environment/Registry Configuration Knobs\n";

    public static string ClrConfigSectionInfo =
        "This table was machine-generated using `clr-configuration-knobs.csx` script from repository commit [GIT_SHORT_HASH](https://github.com/dotnet/coreclr/commit/GIT_LONG_HASH) on DATE_CREATED. It might be out of date. To generate latest documentation run `{dotnet} csi clr-configuration-knobs.csx` from this file directory.\n";

    public static string ClrConfigSectionUsage =
        "When using these configurations from environment variables, the variables need to have the `COMPlus_` prefix in their names. e.g. To set DumpJittedMethods to 1, add the environment variable `COMPlus_DumpJittedMethods=1`.\n\nSee also [Setting configuration variables](../building/viewing-jit-dumps.md#setting-configuration-variables) for more information.\n";

    public static string ClrConfigTableHeader =
        "\nName | Description | Type | Class | Default Value | Flags \n" +
        "-----|-------------|------|-------|---------------|-------\n";

    public static string PalConfigurationKnobs =
        "## PAL Configuration Knobs\n" +
        "All the names below need to be prefixed by `COMPlus_`.\n\n" +
        "Name | Description | Type | Default Value\n" +
        "-----|-------------|------|---------------\n" +
        "`DefaultStackSize` | Overrides the default stack size for secondary threads | `STRING` | `0`\n" +
        "`DbgEnableMiniDump` | If set to 1, enables this core dump generation. The default is NOT to generate a dump | `DWORD` | `0`\n" +
        "`DbgMiniDumpName` | If set, use as the template to create the dump path and file name. The pid can be placed in the name with %d. | `STRING` | `_/tmp/coredump.%d_`\n" +
        "`DbgMiniDumpType` | If set to 1 generates _MiniDumpNormal_, 2 _MiniDumpWithPrivateReadWriteMemory_, 3 _MiniDumpFilterTriage_, 4 _MiniDumpWithFullMemory_ | `DWORD` | `1`\n" +
        "`CreateDumpDiagnostics` | If set to 1, enables the _createdump_ utilities diagnostic messages (TRACE macro) | `DWORD` | `0`\n";

    /// <summary>
    /// Writes documentation file "clr-configuration-knobs.md"
    /// </summary>
    /// <param name="knobs"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string WriteFile(SortedDictionary<string, SortedDictionary<string, Knob>> knobs, string filePath = "clr-configuration-knobs.md")
    {
        int count = 0;
        var hashLong = GetRepoHeadHash();
        var date = DateTime.UtcNow.ToShortDateString();

        using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
        {
            writer.NewLine = "\n";

            writer.WriteLine(IntroSection);

            writer.WriteLine(HostConfigurationKnobsPart1);

            writer.WriteLine(HostConfigurationKnobsPart2);

            writer.WriteLine(ClrConfigSectionHeader);

            ClrConfigSectionInfo = ClrConfigSectionInfo.Replace("GIT_LONG_HASH", hashLong);
            ClrConfigSectionInfo = ClrConfigSectionInfo.Replace("GIT_SHORT_HASH", hashLong.Substring(0, 7));
            ClrConfigSectionInfo = ClrConfigSectionInfo.Replace("DATE_CREATED", date);

            writer.WriteLine(ClrConfigSectionInfo);

            writer.WriteLine(ClrConfigSectionUsage);

            writer.WriteLine("#### Tables");
            int index = 1;
            foreach(string category in knobs.Keys)
            {
                writer.WriteLine($"{index++}. [{category} Configuration Knobs](#{EscapeMdId(category)}-configuration-knobs)");
            }

            writer.WriteLine();

            foreach (string category in knobs.Keys)
            {
                writer.WriteLine($"#### {category} Configuration Knobs");
                writer.Write(ClrConfigTableHeader);

                var catKnobs = knobs[category];

                foreach (string key in catKnobs.Keys)
                {
                    var knob = catKnobs[key];
                    writer.Write($"`{knob.Name}` | {knob.Description} | `{knob.Type}` | ");
                    writer.Write(knob.Class.Length > 0 ? $"`{knob.Class}` | " : " | ");
                    writer.Write(knob.DefaultValue.Length > 0 ? $"`{knob.DefaultValue}` | " : " | ");
                    writer.WriteLine($"{ knob.Flags}");
                    count++;
                }

                writer.WriteLine();
            }

            writer.WriteLine();

            writer.WriteLine(PalConfigurationKnobs);
        }
        return $"Success: {count} parsed configuration knobs, documentation file {filePath} has been updated: commit {hashLong.Substring(0, 7)} date {date}.";
    }

    private static string EscapeMdId(string value)
    {
        var buffer = value.ToLower().ToCharArray();
        StringBuilder sb = new StringBuilder(value.Length);

        for (int i = 0; i < buffer.Length; i++)
        {
            switch (buffer[i])
            {
                case '|':
                case '(':
                case ')':
                case '{':
                case '}':
                case '[':
                case ']':
                case '?':
                case '!':
                case '\\':
                case '/':
                    break;

                case ' ':
                    sb.Append('-');
                    break;

                default:
                    sb.Append(buffer[i]);
                    break;
            }
        }

        return sb.ToString();
    }

    public static string GetRepoHeadHash()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c \"git rev-parse HEAD\"",
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);

        if (process.WaitForExit(20000))
        {
            string result = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (process.ExitCode == 0)
            {
                return result.Trim();
            }
            else
            {
                throw new InvalidOperationException(error);
            }
        }
        else
        {
            throw new TimeoutException("\"git rev-parse HEAD\" command timed out");
        }
    }
}


var knobsDictionary = new SortedDictionary<string, SortedDictionary<string, Knob>>(StringComparer.Create(CultureInfo.InvariantCulture, false));

WriteLine($"Processing header file {clrConfig}");
ParseConfigFile(clrConfig, true, knobsDictionary);

WriteLine($"Processing header file {jitConfig}");
ParseConfigFile(jitConfig, false, knobsDictionary);

WriteLine(ConfigKnobsDoc.WriteFile(knobsDictionary));
