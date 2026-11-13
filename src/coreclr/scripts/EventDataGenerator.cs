// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Generates the native eventing sources (FireEtw* entry points, EventPipe writers, LTTng and ETW
// providers) for CoreCLR and NativeAOT from ClrEtwAll.man.

#:package System.CommandLine@$(SystemCommandLineVersion)
#:property PublishAot=false

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var flavorOption = new Option<string>("--flavor") { Description = "Runtime flavor to generate for.", Required = true };
flavorOption.AcceptOnlyFromAmong("coreclr", "nativeaot");
var providerOption = new Option<string>("--provider") { Description = "Platform event provider. 'etw' implies Windows; 'lttng' and 'none' imply Unix-like targets.", Required = true };
providerOption.AcceptOnlyFromAmong("etw", "lttng", "none");
var manifestOption = new Option<FileInfo>("--manifest") { Description = "Path to ClrEtwAll.man.", Required = true };
var exclusionsOption = new Option<FileInfo>("--exclusion-list") { Description = "Path to ClrEtwAllMeta.lst.", Required = true };
var inclusionsOption = new Option<FileInfo?>("--inclusion-list") { Description = "Optional list restricting the generated events." };
var outputOption = new Option<DirectoryInfo>("--output") { Description = "Directory to generate into.", Required = true };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Print the paths of the files that would be generated." };

var rootCommand = new RootCommand("Generates native eventing sources from ClrEtwAll.man.")
{
    flavorOption, providerOption, manifestOption, exclusionsOption, inclusionsOption, outputOption, dryRunOption
};

rootCommand.Validators.Add(result =>
{
    if (result.GetValue(flavorOption) == "nativeaot" && result.GetValue(providerOption) == "lttng")
    {
        result.AddError("NativeAOT does not support the LTTng provider.");
    }
});

rootCommand.SetAction(result =>
{
    var generator = new EventDataGenerator(
        result.GetValue(flavorOption) == "nativeaot",
        result.GetValue(providerOption)!,
        result.GetValue(manifestOption)!.FullName,
        result.GetValue(exclusionsOption)!.FullName,
        result.GetValue(inclusionsOption)?.FullName,
        result.GetValue(outputOption)!.FullName,
        result.GetValue(dryRunOption));
    generator.Run();
    return 0;
});

return rootCommand.Parse(args).Invoke();

sealed class FunctionParameter(string winType, string name, string count, string? prop)
{
    public string WinType { get; } = winType;
    public string Name { get; } = name;
    public string? Prop { get; } = prop;

    // "win:count" if the parameter is passed by pointer, "win:null" otherwise. GUIDs are treated as structs.
    public string Count { get; } = count == "win:null" ? "win:null"
        : count.Length != 0 || winType == "win:GUID" || count == "win:count" ? "win:count"
        : "win:null";
}

sealed class FunctionSignature
{
    private readonly Dictionary<string, FunctionParameter> _lookup = [];

    public List<string> ParamList { get; } = [];

    public void Append(string variable, FunctionParameter parameter)
    {
        _lookup[variable] = parameter;
        ParamList.Add(variable);
    }

    public FunctionParameter? GetParam(string variable) => _lookup.GetValueOrDefault(variable);

    public IEnumerable<FunctionParameter> Parameters => ParamList.Select(p => _lookup[p]);
}

sealed class Template
{
    public Template(string name, FunctionSignature prototypes, Dictionary<string, List<string>> dependencies, Dictionary<string, string> structs, Dictionary<string, string> arrays)
    {
        Name = name;
        Structs = structs;
        Arrays = arrays;
        foreach (string variable in prototypes.ParamList)
        {
            foreach (string dependency in dependencies[variable])
            {
                if (Signature.GetParam(dependency) is null)
                {
                    Signature.Append(dependency, prototypes.GetParam(dependency)!);
                }
            }
        }
    }

    public string Name { get; }
    public FunctionSignature Signature { get; } = new();
    public Dictionary<string, string> Structs { get; }
    public Dictionary<string, string> Arrays { get; }

    public int NumParams => Signature.ParamList.Count;

    public int EstimatedSize
    {
        get
        {
            int total = 0;
            foreach (FunctionParameter parameter in Signature.Parameters)
            {
                total += parameter.WinType switch
                {
                    "win:Int64" or "win:Double" or "win:UInt64" or "win:Pointer" => 8,
                    "win:ULong" or "win:Int32" or "win:Boolean" or "win:UInt32" => 4,
                    "win:UInt16" => 2,
                    "win:UInt8" or "win:Binary" => 1,
                    "win:AnsiString" or "win:Struct" => 32,
                    "win:UnicodeString" => 64,
                    _ => 0,
                };
            }

            return Math.Clamp(total, 32, 1024);
        }
    }
}

sealed class EventExclusions
{
    public HashSet<string> NoStack { get; } = [];
    public HashSet<string> ExplicitStack { get; } = [];
    public HashSet<string> NoClrInstance { get; } = [];
}

sealed class EventDataGenerator
{
    private const string Indent = "    ";

    private const string WriteFailureCheck = """
            if (!success)
            {
                if (!fixedBuffer)
                    delete[] buffer;
                return ERROR_WRITE_FAULT;
            }


        """;

    private const string FreeBuffer = """

            if (!fixedBuffer)
                delete[] buffer;

        """;
    private const string MonoProfilerProvider = "Microsoft-DotNETRuntimeMonoProfiler";
    private const string StressProvider = "Microsoft-Windows-DotNETRuntimeStress";
    private static readonly string[] s_mcOutputs = ["ClrEtwAll.h", "ClrEtwAll.rc", "ClrEtwAll_MSG00001.bin", "ClrEtwAllTEMP.bin"];

    private static readonly Dictionary<string, string> s_coreClrPalTypes = new()
    {
        ["win:null"] = " ",
        ["win:Int64"] = "const int64_t",
        ["win:ULong"] = "const ULONG",
        ["win:count"] = "*",
        ["win:Struct"] = "const void",
        ["win:GUID"] = "const GUID",
        ["win:AnsiString"] = "LPCSTR",
        ["win:UnicodeString"] = "PCWSTR",
        ["win:Double"] = "const double",
        ["win:Int32"] = "const signed int",
        ["win:Boolean"] = "const BOOL",
        ["win:UInt64"] = "const uint64_t",
        ["win:UInt32"] = "const unsigned int",
        ["win:UInt16"] = "const unsigned short",
        ["win:UInt8"] = "const unsigned char",
        ["win:Pointer"] = "const void*",
        ["win:Binary"] = "const BYTE",
    };

    private static readonly Dictionary<string, string> s_nativeAotPalTypes = new(s_coreClrPalTypes)
    {
        ["win:UnicodeString"] = "const WCHAR*",
    };

    private static readonly Dictionary<string, string> s_coreClrEventPipeTypes = new()
    {
        ["BOOL"] = "BOOL",
        ["LPCGUID"] = "LPCGUID",
        ["UCHAR"] = "UCHAR",
        ["ULONG"] = "ULONG",
        ["ULONGLONG"] = "ULONGLONG",
        ["WCHAR"] = "WCHAR",
        ["BYTE"] = "BYTE",
    };

    private static readonly Dictionary<string, string> s_nativeAotEventPipeTypes = new(s_coreClrEventPipeTypes)
    {
        ["LPCGUID"] = "const GUID *",
    };

    private static readonly Dictionary<string, string> s_coreClrLttngTypes = new()
    {
        ["win:null"] = " ",
        ["win:Int64"] = "const int64_t",
        ["win:ULong"] = "const ULONG",
        ["win:count"] = "*",
        ["win:Struct"] = "const BYTE *",
        ["win:GUID"] = "const int",
        ["win:AnsiString"] = "const char*",
        ["win:UnicodeString"] = "const char*",
        ["win:Double"] = "const double",
        ["win:Int32"] = "const signed int",
        ["win:Boolean"] = "const BOOL",
        ["win:UInt64"] = "const uint64_t",
        ["win:UInt32"] = "const unsigned int",
        ["win:UInt16"] = "const unsigned short",
        ["win:UInt8"] = "const unsigned char",
        ["win:Pointer"] = "const size_t",
        ["win:Binary"] = "const BYTE",
    };

    private static readonly Dictionary<string, string> s_portableLttngTypes = new()
    {
        ["win:null"] = " ",
        ["win:Int64"] = "const int64_t",
        ["win:ULong"] = "const uint32_t",
        ["win:count"] = "*",
        ["win:Struct"] = "const uint8_t *",
        ["win:GUID"] = "const int32_t",
        ["win:AnsiString"] = "const char*",
        ["win:UnicodeString"] = "const ep_char8_t*",
        ["win:Double"] = "const double",
        ["win:Int32"] = "const int32_t",
        ["win:Boolean"] = "const bool",
        ["win:UInt64"] = "const uint64_t",
        ["win:UInt32"] = "const uint32_t",
        ["win:UInt16"] = "const uint16_t",
        ["win:UInt8"] = "const uint8_t",
        ["win:Pointer"] = "const void*",
        ["win:Binary"] = "const uint8_t",
    };

    private static readonly Dictionary<string, string> s_ctfTypes = new()
    {
        ["win:Int64"] = "ctf_integer",
        ["win:ULong"] = "ctf_integer",
        ["win:count"] = "ctf_sequence",
        ["win:Struct"] = "ctf_sequence",
        ["win:GUID"] = "ctf_sequence",
        ["win:AnsiString"] = "ctf_string",
        ["win:UnicodeString"] = "ctf_string",
        ["win:Double"] = "ctf_float",
        ["win:Int32"] = "ctf_integer",
        ["win:Boolean"] = "ctf_integer",
        ["win:UInt64"] = "ctf_integer",
        ["win:UInt32"] = "ctf_integer",
        ["win:UInt16"] = "ctf_integer",
        ["win:UInt8"] = "ctf_integer",
        ["win:Pointer"] = "ctf_integer",
        ["win:Binary"] = "ctf_sequence",
    };

    // Templates whose variable-length payload size is carried by an explicit element size argument.
    private static readonly Dictionary<string, Dictionary<string, string>> s_specialCaseSizes = new()
    {
        ["BulkType"] = new() { ["Values"] = "Values_ElementSize" },
        ["GCBulkRootCCW"] = new() { ["Values"] = "Values_ElementSize" },
        ["GCBulkRCW"] = new() { ["Values"] = "Values_ElementSize" },
        ["GCBulkRootStaticVar"] = new() { ["Values"] = "Values_ElementSize" },
    };

    private const int MaxLttngArgs = 9;

    private readonly bool _nativeAot;
    private readonly string _provider;
    private readonly string _manifestPath;
    private readonly string _exclusionPath;
    private readonly string _outputDir;
    private readonly bool _dryRun;
    private readonly XDocument _manifest;
    private readonly Dictionary<string, HashSet<string>> _inclusionList;
    private readonly EventExclusions _exclusions;
    private readonly List<string> _outputs = [];

    public EventDataGenerator(bool nativeAot, string provider, string manifestPath, string exclusionPath, string? inclusionPath, string outputDir, bool dryRun)
    {
        _nativeAot = nativeAot;
        _provider = provider;
        _manifestPath = manifestPath;
        _exclusionPath = exclusionPath;
        _outputDir = outputDir;
        _dryRun = dryRun;
        _manifest = XDocument.Load(manifestPath);
        _inclusionList = ParseInclusionList(inclusionPath);
        _exclusions = ParseExclusionList(exclusionPath);
    }

    private bool IsWindows => _provider == "etw";
    private bool Extern => !_nativeAot && !IsWindows;
    private bool WriteXplatHeader => !_nativeAot && !IsWindows;
    private Dictionary<string, string> PalTypes => _nativeAot ? s_nativeAotPalTypes : s_coreClrPalTypes;
    private Dictionary<string, string> EventPipeTypes => _nativeAot ? s_nativeAotEventPipeTypes : s_coreClrEventPipeTypes;
    private Dictionary<string, string> LttngTypes => _nativeAot ? s_portableLttngTypes : s_coreClrLttngTypes;

    public void Run()
    {
        CheckConsistency();

        GenerateEtmDummyHeader();
        GeneratePlatformIndependentFiles();
        GenerateEventPipeFiles();

        if (IsWindows)
        {
            GenerateEtwFiles();
        }

        if (_provider == "lttng")
        {
            GenerateLttngFiles();
        }

        if (_dryRun)
        {
            foreach (string output in _outputs)
            {
                Console.WriteLine(output);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Manifest helpers

    private static IEnumerable<XElement> ElementsByTagName(XContainer node, string tag) => node.Descendants().Where(e => e.Name.LocalName == tag);

    private static IEnumerable<XElement> ChildrenByTagName(XElement node, string tag) => node.Elements().Where(e => e.Name.LocalName == tag);

    private static string Attr(XElement element, string name) => element.Attribute(name)?.Value ?? "";

    private IEnumerable<XElement> Providers => ElementsByTagName(_manifest, "provider");

    private static bool IsDigits(string value) => value.Length != 0 && value.All(char.IsAsciiDigit);

    private static Dictionary<string, Template> ParseTemplates(XElement providerNode)
    {
        Dictionary<string, Template> allTemplates = [];
        foreach (XElement templateNode in ElementsByTagName(providerNode, "template"))
        {
            Dictionary<string, string> structCounts = [];
            Dictionary<string, string> arrays = [];
            string templateName = Attr(templateNode, "tid");
            Dictionary<string, List<string>> dependencies = [];
            FunctionSignature prototypes = new();
            List<XElement> dataNodes = ChildrenByTagName(templateNode, "data").ToList();

            foreach (XElement dataNode in dataNodes)
            {
                foreach (XAttribute attribute in dataNode.Attributes())
                {
                    string attributeName = attribute.Name.LocalName;
                    if (attributeName is not ("map" or "outType" or "name" or "inType" or "count" or "length"))
                    {
                        throw new InvalidDataException($"unknown attribute: {attributeName} in template:{templateName}");
                    }
                }
            }

            foreach (XElement dataNode in dataNodes)
            {
                string variable = Attr(dataNode, "name");
                string winType = Attr(dataNode, "inType");
                string winCount = Attr(dataNode, "count");
                string winLength = Attr(dataNode, "length");

                string? prop = null;
                List<string> dependency = [variable];
                if (winLength.Length != 0)
                {
                    if (winCount.Length != 0)
                    {
                        throw new InvalidDataException($"both count and length property found on: {variable}in template: {templateName}");
                    }

                    winCount = winLength;
                }

                if (IsDigits(winCount) && int.Parse(winCount) == 1)
                {
                    winCount = "";
                }

                if (winCount.Length != 0)
                {
                    if (IsDigits(winCount))
                    {
                        prop = winCount;
                    }
                    else if (prototypes.GetParam(winCount) is not null)
                    {
                        prop = winCount;
                        dependency.Insert(0, winCount);
                        arrays[variable] = winCount;
                    }
                }

                if (winType == "win:GUID")
                {
                    prop = "sizeof(GUID)/sizeof(int)";
                }

                dependencies[variable] = dependency;
                prototypes.Append(variable, new FunctionParameter(winType, variable, winCount, prop));
            }

            foreach (XElement structNode in ChildrenByTagName(templateNode, "struct"))
            {
                string structName = Attr(structNode, "name");
                string countVarName = Attr(structNode, "count");

                if (countVarName != "Count" || !prototypes.ParamList.Contains(countVarName))
                {
                    throw new InvalidDataException($"Struct '{structName}' in template '{templateName}' must have a count attribute referring to a 'Count' field.");
                }

                structCounts[structName] = countVarName;
                dependencies[structName] = [countVarName, structName];
                prototypes.Append(structName, new FunctionParameter("win:Struct", structName, "win:count", countVarName));
            }

            allTemplates[templateName] = new Template(templateName, prototypes, dependencies, structCounts, arrays);
        }

        return allTemplates;
    }

    private static Template? GetTemplate(Dictionary<string, Template> templates, XElement eventNode)
    {
        string templateName = Attr(eventNode, "template");
        return templateName.Length != 0 ? templates[templateName] : null;
    }

    private static Dictionary<string, HashSet<string>> ParseInclusionList(string? path)
    {
        Dictionary<string, HashSet<string>> inclusionList = [];
        if (path is null || !File.Exists(path))
        {
            return inclusionList;
        }

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] tokens = line.Split(':');
            if (tokens.Length > 2)
            {
                throw new InvalidDataException($"Invalid Entry {line}in {path}");
            }

            (string providerName, string eventName) = tokens.Length == 2 ? (tokens[0], tokens[1]) : ("*", tokens[0]);
            if (!inclusionList.TryGetValue(providerName, out HashSet<string>? events))
            {
                inclusionList[providerName] = events = [];
            }

            events.Add(eventName);
        }

        return inclusionList;
    }

    private static EventExclusions ParseExclusionList(string path)
    {
        EventExclusions exclusions = new();
        if (!File.Exists(path))
        {
            return exclusions;
        }

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] tokens = line.Split(':');
            if (tokens.Contains("nomac"))
            {
                continue;
            }

            if (tokens.Length > 5)
            {
                throw new InvalidDataException($"Invalid Entry {line}in {path}");
            }

            string eventProvider = tokens[2].Length != 0 ? tokens[2] : "*";
            string eventTask = tokens[1].Length != 0 ? tokens[1] : "*";
            string eventSymbol = tokens[4].Length != 0 ? tokens[4] : "*";
            string entry = $"{eventProvider}:{eventTask}:{eventSymbol}";

            switch (tokens[0].ToLowerInvariant())
            {
                case "nostack": exclusions.NoStack.Add(entry); break;
                case "stack": exclusions.ExplicitStack.Add(entry); break;
                case "noclrinstanceid": exclusions.NoClrInstance.Add(entry); break;
            }
        }

        return exclusions;
    }

    private bool IncludeProvider(string providerName) =>
        providerName != MonoProfilerProvider && !(_nativeAot && providerName == StressProvider);

    private bool IncludeEvent(string providerName, string eventName)
    {
        if (_inclusionList.Count == 0)
        {
            return true;
        }

        return (_inclusionList.TryGetValue(providerName, out HashSet<string>? events) && (events.Contains(eventName) || events.Contains("*")))
            || (_inclusionList.TryGetValue("*", out HashSet<string>? anyProvider) && (anyProvider.Contains(eventName) || anyProvider.Contains("*")));
    }

    // Provider names as used in symbols ("DotNETRuntime_Private") and file names ("dotnetruntimeprivate").
    private static string ProviderShortName(string providerName) => providerName.Replace("Windows-", "").Replace("Microsoft-", "");
    private static string ProviderPrettyName(string providerName) => ProviderShortName(providerName).Replace('-', '_');
    private static string ProviderFileName(string providerName) => ProviderShortName(providerName).Replace("-", "").ToLowerInvariant();

    // Matches when the entry applies to the event; the "bit" is set when no entry applies.
    private static bool GetStackWalkBit(string eventProvider, string taskName, string eventSymbol, HashSet<string> entries)
    {
        foreach (string entry in entries)
        {
            string[] tokens = entry.Split(':');
            if (tokens.Length != 3)
            {
                throw new InvalidDataException($"Error, possible error in the script which introduced the entry {entry}");
            }

            if ((tokens[0] == eventProvider || tokens[0] == "*")
                && (tokens[1] == taskName || tokens[1] == "*")
                && (tokens[2] == eventSymbol || tokens[2] == "*"))
            {
                return false;
            }
        }

        return true;
    }

    private void CheckConsistency()
    {
        foreach (XElement providerNode in Providers)
        {
            Dictionary<int, bool> stackSupportSpecified = [];
            string eventProvider = Attr(providerNode, "name");
            Dictionary<string, Template> allTemplates = ParseTemplates(providerNode);

            foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
            {
                string taskName = Attr(eventNode, "task");
                string eventSymbol = Attr(eventNode, "symbol");
                string eventTemplate = Attr(eventNode, "template");
                int eventValue = int.Parse(Attr(eventNode, "value"));

                if (GetStackWalkBit(eventProvider, taskName, eventSymbol, _exclusions.NoClrInstance)
                    && allTemplates.TryGetValue(eventTemplate, out Template? template))
                {
                    FunctionParameter? parameter = template.Signature.GetParam("ClrInstanceID");
                    if (parameter?.WinType != "win:UInt16")
                    {
                        throw new InvalidDataException($"{_exclusionPath}:No ClrInstanceID field of type win:UInt16 for event symbol {eventSymbol}");
                    }
                }

                // Either all or none of the versions of an event must be on the nostack/stack lists.
                bool noStackBit = GetStackWalkBit(eventProvider, taskName, eventSymbol, _exclusions.NoStack);
                bool explicitStackBit = GetStackWalkBit(eventProvider, taskName, eventSymbol, _exclusions.ExplicitStack);
                bool specified = !noStackBit || !explicitStackBit;
                if (!stackSupportSpecified.GetValueOrDefault(eventValue))
                {
                    stackSupportSpecified[eventValue] = specified;
                }
                else if (!specified)
                {
                    throw new InvalidDataException($"{_exclusionPath}: Error processing event :{eventSymbol}(ID{eventValue}): This file must contain either ALL versions of this event or NO versions of this event. Currently some, but not all, versions of this event are present\n");
                }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Output

    private void WriteFile(string relativePath, string content)
    {
        string path = Path.Combine(_outputDir, relativePath);
        _outputs.Add(path.Replace('\\', '/'));
        if (_dryRun)
        {
            return;
        }

        UpdateFile(path, Encoding.ASCII.GetBytes(NormalizeNewLines(content)));
    }

    // Generated text uses \n internally; files get the platform's native line endings.
    private static string NormalizeNewLines(string content)
    {
        content = content.Replace("\r\n", "\n").Replace('\r', '\n');
        return OperatingSystem.IsWindows() ? content.Replace("\n", "\r\n") : content;
    }

    // Leaves the file untouched when the content is unchanged so incremental builds stay incremental.
    private static void UpdateFile(string path, byte[] content)
    {
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    // ---------------------------------------------------------------------------------------------
    // Platform independent files: clretwallmain.*, clreventpipewriteevents.h, clrproviders.h, clrxplatevents.h, etmdummy.h

    private void AppendParameterDeclarations(List<string> tokens, Template template, Dictionary<string, string> types, bool omitNullCount)
    {
        foreach (string paramName in template.Signature.ParamList)
        {
            FunctionParameter parameter = template.Signature.GetParam(paramName)!;
            string countType = types[parameter.Count];
            if (template.Structs.ContainsKey(paramName))
            {
                tokens.Add($"{Indent}int {paramName}_ElementSize,\n");
            }

            tokens.Add(Indent);
            tokens.Add(types[parameter.WinType]);
            if (!omitNullCount || countType != " ")
            {
                tokens.Add(countType);
            }

            tokens.Add(" ");
            tokens.Add(parameter.Name);
            tokens.Add(",\n");
        }
    }

    private static List<string> ArgumentList(Template? template)
    {
        List<string> line = [];
        if (template is not null)
        {
            foreach (string paramName in template.Signature.ParamList)
            {
                FunctionParameter parameter = template.Signature.GetParam(paramName)!;
                if (template.Structs.ContainsKey(paramName))
                {
                    line.Add(parameter.Name + "_ElementSize");
                    line.Add(", ");
                }

                line.Add(parameter.Name);
                line.Add(",");
            }

            RemoveLast(line);
        }

        return line;
    }

    private static void RemoveLast(List<string> tokens)
    {
        if (tokens.Count > 0)
        {
            tokens.RemoveAt(tokens.Count - 1);
        }
    }

    private string GenerateClrAllEvents(XElement providerNode, Dictionary<string, Template> allTemplates, string fileType)
    {
        string providerName = Attr(providerNode, "name");
        bool isHeader = fileType is "header" or "header-impl";
        var sb = new StringBuilder();
        foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
        {
            string eventName = Attr(eventNode, "symbol");
            if (!IncludeEvent(providerName, eventName))
            {
                continue;
            }

            if (IncludeProvider(providerName))
            {
                if (fileType == "header-impl")
                {
                    sb.Append("inline ");
                }

                sb.Append($"{EventPipeTypes["BOOL"]} EventEnabled{eventName}(void)");
                if (fileType == "header")
                {
                    sb.Append(";\n");
                }
                else if (fileType == "source-impl-noop")
                {
                    sb.Append(" { return 0; }\n");
                }
                else
                {
                    sb.Append($" {{return EventPipeEventEnabled{eventName}()");
                    if (!IsWindows)
                    {
                        // NativeAOT does not support non-Windows eventing other than EventPipe.
                        if (!_nativeAot)
                        {
                            sb.Append($$"""

                                #if defined(FEATURE_EVENTSOURCE_XPLAT)
                                    || (XplatEventLogger::IsEventLoggingEnabled() && EventXplatEnabled{{eventName}}())
                                #endif // FEATURE_EVENTSOURCE_XPLAT

                                """);
                        }

                        sb.Append(";}\n\n");
                    }
                    else
                    {
                        sb.Append($" || EventXplatEnabled{eventName}();}}\n\n");
                    }
                }
            }

            Template? template = GetTemplate(allTemplates, eventNode);
            List<string> fnPrototype = [];
            if (!_nativeAot)
            {
                fnPrototype.Add("inline ");
            }

            fnPrototype.Add($"{EventPipeTypes["ULONG"]} FireEtw{eventName}(\n");
            if (template is not null)
            {
                AppendParameterDeclarations(fnPrototype, template, PalTypes, omitNullCount: false);
            }

            string line = string.Concat(ArgumentList(template));
            string guidType = EventPipeTypes["LPCGUID"];
            fnPrototype.Add($"{Indent}{guidType} ActivityId{(isHeader ? " = nullptr," : ",")}\n");
            fnPrototype.Add($"{Indent}{guidType} RelatedActivityId{(isHeader ? " = nullptr" : "")}");
            fnPrototype.Add("\n)");

            sb.AppendJoin("", fnPrototype);
            if (fileType == "header")
            {
                sb.Append(";\n\n");
            }
            else if (fileType == "source-impl-noop")
            {
                sb.Append("\n{ return ERROR_SUCCESS; }\n\n");
            }
            else
            {
                sb.Append("\n{\n");
                sb.Append($"{Indent}{EventPipeTypes["ULONG"]} status = EventPipeWriteEvent{eventName}({line}");
                if (line.Length != 0)
                {
                    sb.Append(',');
                }

                sb.Append("ActivityId,RelatedActivityId);\n");

                if (!_nativeAot)
                {
                    if (!IsWindows)
                    {
                        sb.Append("#if defined(FEATURE_EVENTSOURCE_XPLAT)\n");
                    }

                    sb.Append($"{Indent}status &= FireEtXplat{eventName}({line});\n");
                    if (!IsWindows)
                    {
                        sb.Append("#endif // FEATURE_EVENTSOURCE_XPLAT\n");
                    }
                }
                else
                {
                    bool runtimeProvider = providerName is "Microsoft-Windows-DotNETRuntime" or "Microsoft-Windows-DotNETRuntimePrivate" or "Microsoft-Windows-DotNETRuntimeRundown";
                    sb.Append(runtimeProvider ? $"#ifndef TARGET_UNIX\n{Indent}status &= " : "return ");
                    sb.Append($"FireEtXplat{eventName}({line});\n");
                    if (runtimeProvider)
                    {
                        sb.Append("#endif\n");
                    }
                }

                sb.Append($"{Indent}return status;\n}}\n\n");
            }
        }

        return sb.ToString();
    }

    private string GenerateClrXplatEvents(XElement providerNode, Dictionary<string, Template> allTemplates)
    {
        string externC = Extern ? "extern \"C\" " : "";
        var sb = new StringBuilder();
        foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
        {
            string eventName = Attr(eventNode, "symbol");
            sb.Append($"{externC}{EventPipeTypes["BOOL"]} EventXplatEnabled{eventName}();\n");

            List<string> fnPrototype = [$"{externC}{EventPipeTypes["ULONG"]} FireEtXplat{eventName}(\n"];
            Template? template = GetTemplate(allTemplates, eventNode);
            if (template is not null)
            {
                List<string> parameters = [];
                AppendParameterDeclarations(parameters, template, PalTypes, omitNullCount: false);
                RemoveLast(parameters);
                fnPrototype.AddRange(parameters);
            }

            fnPrototype.Add("\n);\n");
            sb.AppendJoin("", fnPrototype);
        }

        return sb.ToString();
    }

    private string GenerateClrEventPipeWriteEvents(XElement providerNode, Dictionary<string, Template> allTemplates)
    {
        string providerName = Attr(providerNode, "name");
        string externC = Extern ? "extern \"C\" " : "";
        var sb = new StringBuilder();
        foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
        {
            string eventName = Attr(eventNode, "symbol");
            if (!IncludeEvent(providerName, eventName))
            {
                continue;
            }

            sb.Append($"{externC}{EventPipeTypes["BOOL"]} EventPipeEventEnabled{eventName}(void);\n");

            List<string> writeEvent = [$"{externC}{EventPipeTypes["ULONG"]} EventPipeWriteEvent{eventName}(\n"];
            Template? template = GetTemplate(allTemplates, eventNode);
            if (template is not null)
            {
                AppendParameterDeclarations(writeEvent, template, PalTypes, omitNullCount: false);
            }

            writeEvent.Add($"{Indent}{EventPipeTypes["LPCGUID"]} ActivityId = nullptr,\n");
            writeEvent.Add($"{Indent}{EventPipeTypes["LPCGUID"]} RelatedActivityId = nullptr");
            writeEvent.Add("\n);\n");
            sb.AppendJoin("", writeEvent);
        }

        return sb.ToString();
    }

    private void GenerateEtmDummyHeader()
    {
        var sb = new StringBuilder(Prologs.Eventing).Append('\n');
        foreach (XElement providerNode in Providers)
        {
            Dictionary<string, Template> allTemplates = ParseTemplates(providerNode);
            foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
            {
                List<string> line = [];
                Template? template = GetTemplate(allTemplates, eventNode);
                if (template is not null)
                {
                    foreach (string paramName in template.Signature.ParamList)
                    {
                        string name = template.Signature.GetParam(paramName)!.Name;
                        if (template.Structs.ContainsKey(paramName))
                        {
                            line.Add(name + "_ElementSize");
                            line.Add(", ");
                        }

                        line.Add(name);
                        line.Add(", ");
                    }

                    RemoveLast(line);
                }

                sb.Append($"#define FireEtw{Attr(eventNode, "symbol")}(").AppendJoin("", line).Append(") 0\n");
            }

            sb.Append('\n');
        }

        WriteFile("etmdummy.h", sb.ToString());
    }

    private string EventPipeTraceContextTypedef =>
        $$"""

        #if !defined(EVENTPIPE_TRACE_CONTEXT_DEF)
        #define EVENTPIPE_TRACE_CONTEXT_DEF
        typedef struct _EVENTPIPE_TRACE_CONTEXT
        {
            const {{EventPipeTypes["WCHAR"]}} * Name;
            {{EventPipeTypes["UCHAR"]}} Level;
            bool IsEnabled;
            {{EventPipeTypes["ULONGLONG"]}} EnabledKeywordsBitmask;
        } EVENTPIPE_TRACE_CONTEXT, *PEVENTPIPE_TRACE_CONTEXT;
        #endif // EVENTPIPE_TRACE_CONTEXT_DEF

        """;

    private const string LttngTraceContextTypedef = """

        #if !defined(LTTNG_TRACE_CONTEXT_DEF)
        #define LTTNG_TRACE_CONTEXT_DEF
        typedef struct _LTTNG_TRACE_CONTEXT
        {
            WCHAR const * Name;
            UCHAR Level;
            bool IsEnabled;
            ULONGLONG EnabledKeywordsBitmask;
        } LTTNG_TRACE_CONTEXT, *PLTTNG_TRACE_CONTEXT;
        #endif // LTTNG_TRACE_CONTEXT_DEF

        """;

    private const string DotNetTraceContextTypedefWindows = """

        #if !defined(DOTNET_TRACE_CONTEXT_DEF)
        #define DOTNET_TRACE_CONTEXT_DEF
        typedef struct _DOTNET_TRACE_CONTEXT
        {
            PMCGEN_TRACE_CONTEXT EtwProvider;
            EVENTPIPE_TRACE_CONTEXT EventPipeProvider;
        } DOTNET_TRACE_CONTEXT, *PDOTNET_TRACE_CONTEXT;
        #endif // DOTNET_TRACE_CONTEXT_DEF

        """;

    private const string DotNetTraceContextTypedefUnix = """

        #if !defined(DOTNET_TRACE_CONTEXT_DEF)
        #define DOTNET_TRACE_CONTEXT_DEF
        typedef struct _DOTNET_TRACE_CONTEXT
        {
            EVENTPIPE_TRACE_CONTEXT EventPipeProvider;
        #if defined(FEATURE_EVENTSOURCE_XPLAT)
            PLTTNG_TRACE_CONTEXT LttngProvider;
        #endif
        } DOTNET_TRACE_CONTEXT, *PDOTNET_TRACE_CONTEXT;
        #endif // DOTNET_TRACE_CONTEXT_DEF

        """;

    private void WriteClrEventsFile(string fileName, string fileType)
    {
        var sb = new StringBuilder(Prologs.Eventing);
        sb.Append("#include <minipal/guid.h>\n\n");
        sb.Append(fileType switch
        {
            "header-impl" => """
                #include "clrxplatevents.h"
                #include "clreventpipewriteevents.h"

                """,
            "header" => """
                #ifndef CLR_ETW_ALL_MAIN_H
                #define CLR_ETW_ALL_MAIN_H


                """,
            "source-impl" => """
                #include <common.h>
                #include <Pal.h>
                #include "clretwallmain.h"
                #include "clreventpipewriteevents.h"
                #ifdef FEATURE_ETW
                #include "ClrEtwAll.h"
                #endif


                """,
            _ => """
                #include <CommonTypes.h>
                #include <CommonMacros.h>

                #include <Pal.h>

                #ifndef ERROR_SUCCESS
                #define ERROR_SUCCESS 0L
                #endif


                """,
        });

        bool coreClrWindows = IsWindows && !_nativeAot;
        if (coreClrWindows)
        {
            sb.Append(EventPipeTraceContextTypedef);
            sb.Append(DotNetTraceContextTypedefWindows).Append('\n');
        }

        foreach (XElement providerNode in Providers)
        {
            sb.Append(GenerateClrAllEvents(providerNode, ParseTemplates(providerNode), fileType));
            if (coreClrWindows)
            {
                sb.Append($"constexpr EVENTPIPE_TRACE_CONTEXT {Attr(providerNode, "symbol")}_EVENTPIPE_Context = {{ W(\"{Attr(providerNode, "name")}\"), 0, false, 0 }};\n");
            }
        }

        if (fileType == "header")
        {
            sb.Append("#endif // __CLR_ETW_ALL_MAIN_H__\n");
        }

        WriteFile(fileName, sb.ToString());
    }

    private static int ConvertToLevelId(string level) => level switch
    {
        "win:LogAlways" => 0,
        "win:Critical" => 1,
        "win:Error" => 2,
        "win:Warning" => 3,
        "win:Informational" => 4,
        "win:Verbose" => 5,
        _ => throw new InvalidDataException($"unknown level {level}"),
    };

    private static ulong ParseMask(string mask) =>
        mask.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToUInt64(mask[2..], 16) : ulong.Parse(mask);

    private void GenerateClrProviders()
    {
        var sb = new StringBuilder("""

            typedef struct _EVENT_DESCRIPTOR
            {
                int const Level;
                ULONGLONG const Keyword;
            } EVENT_DESCRIPTOR;

            """);
        sb.Append(EventPipeTraceContextTypedef);
        sb.Append("#if defined(FEATURE_EVENTSOURCE_XPLAT)\n");
        sb.Append(LttngTraceContextTypedef);
        sb.Append("#endif // FEATURE_EVENTSOURCE_XPLAT\n");
        sb.Append(DotNetTraceContextTypedefUnix).Append('\n');

        List<string> allProviders = [];
        foreach (XElement providerNode in Providers)
        {
            string providerName = Attr(providerNode, "name");
            string providerSymbol = Attr(providerNode, "symbol");
            sb.Append($$"""
                __attribute__((weak)) EVENTPIPE_TRACE_CONTEXT {{providerSymbol}}_EVENTPIPE_Context = { W("{{providerName}}"), 0, false, 0 };
                #if defined(FEATURE_EVENTSOURCE_XPLAT)
                __attribute__((weak)) LTTNG_TRACE_CONTEXT {{providerSymbol}}_LTTNG_Context = { W("{{providerName}}"), 0, false, 0 };
                #endif // FEATURE_EVENTSOURCE_XPLAT
                // Keywords

                """);
            Dictionary<string, ulong> keywordsToMask = [];
            foreach (XElement keywordNode in ElementsByTagName(providerNode, "keyword"))
            {
                string keywordMask = Attr(keywordNode, "mask");
                sb.Append($"#define {Attr(keywordNode, "symbol")} {keywordMask}\n");
                keywordsToMask[Attr(keywordNode, "name")] = ParseMask(keywordMask);
            }

            foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
            {
                ulong mask = 0;
                foreach (string keyword in Attr(eventNode, "keywords").Split(' '))
                {
                    if (keyword.Length != 0)
                    {
                        mask |= keywordsToMask[keyword];
                    }
                }

                sb.Append($"constexpr EVENT_DESCRIPTOR {Attr(eventNode, "symbol")} = {{ {ConvertToLevelId(Attr(eventNode, "level"))}, 0x{mask:x} }};\n");
            }

            allProviders.Add($"&{providerSymbol}_LTTNG_Context");
        }

        sb.Append($$"""
            #if defined(FEATURE_EVENTSOURCE_XPLAT)
            #define NB_PROVIDERS {{allProviders.Count}}
            constexpr LTTNG_TRACE_CONTEXT * ALL_LTTNG_PROVIDERS_CONTEXT[NB_PROVIDERS] = { {{string.Join(", ", allProviders)}} };
            #endif // FEATURE_EVENTSOURCE_XPLAT

            """);

        WriteFile("clrproviders.h", sb.ToString());
    }

    private void GeneratePlatformIndependentFiles()
    {
        // NativeAOT needs the header and the implementation separated, plus a no-op implementation.
        if (_nativeAot)
        {
            WriteClrEventsFile("clretwallmain.cpp", "source-impl");
            WriteClrEventsFile("clretwallmain.h", "header");
            WriteClrEventsFile("disabledclretwallmain.cpp", "source-impl-noop");
        }
        else
        {
            WriteClrEventsFile("clretwallmain.h", "header-impl");
        }

        if (WriteXplatHeader)
        {
            GenerateClrProviders();
        }

        var writeEvents = new StringBuilder(Prologs.Eventing).Append('\n');
        foreach (XElement providerNode in Providers)
        {
            writeEvents.Append(GenerateClrEventPipeWriteEvents(providerNode, ParseTemplates(providerNode))).Append('\n');
        }

        WriteFile("clreventpipewriteevents.h", writeEvents.ToString());

        if (WriteXplatHeader)
        {
            var xplatEvents = new StringBuilder(Prologs.Eventing).Append('\n');
            foreach (XElement providerNode in Providers)
            {
                xplatEvents.Append(GenerateClrXplatEvents(providerNode, ParseTemplates(providerNode))).Append('\n');
            }

            WriteFile("clrxplatevents.h", xplatEvents.ToString());
        }
    }

    // ---------------------------------------------------------------------------------------------
    // EventPipe: eventpipe/eventpipehelpers.cpp and eventpipe/<provider>.cpp

    private void GenerateEventPipeFiles()
    {
        string externC = Extern ? "extern \"C\" " : "";
        List<XElement> providers = Providers.Where(p => IncludeProvider(Attr(p, "name"))).ToList();

        var helper = new StringBuilder(Prologs.EventPipe);
        helper.Append(_nativeAot ? Templates.NativeAotEventPipeHelperPrefix : Templates.CoreClrEventPipeHelperPrefix);
        foreach (XElement providerNode in providers)
        {
            helper.Append($"{externC}void Init{ProviderPrettyName(Attr(providerNode, "name"))}(void);\n\n");
        }

        helper.Append($$"""
            {{externC}}void InitProvidersAndEvents(void);

            void InitProvidersAndEvents(void)
            {

            """);
        foreach (XElement providerNode in providers)
        {
            helper.Append($"    Init{ProviderPrettyName(Attr(providerNode, "name"))}();\n");
        }

        helper.Append("}\n");
        WriteFile("eventpipe/eventpipehelpers.cpp", helper.ToString());

        Dictionary<string, ulong> keywordMap = [];
        foreach (XElement keywordNode in ElementsByTagName(_manifest, "keyword"))
        {
            keywordMap[Attr(keywordNode, "name")] = ParseMask(Attr(keywordNode, "mask"));
        }

        foreach (XElement providerNode in providers)
        {
            string providerName = Attr(providerNode, "name");
            string prettyName = ProviderPrettyName(providerName);

            var impl = new StringBuilder(Prologs.EventPipe);
            impl.Append(_nativeAot ? Templates.NativeAotEventPipeImplPrefix : Templates.CoreClrEventPipeImplPrefix).Append('\n');
            impl.Append($"const {EventPipeTypes["WCHAR"]}* {prettyName}Name = W(\"{providerName}\");\n");
            impl.Append($"EventPipeProvider *EventPipeProvider{prettyName} = nullptr;\n");
            impl.Append(GenerateEventPipeWriteEventsImpl(providerNode, ParseTemplates(providerNode), keywordMap)).Append('\n');
            if (_nativeAot && providerName == "Microsoft-Windows-DotNETRuntime")
            {
                impl.Append(Templates.NativeAotEventPipeImplSuffix);
            }

            WriteFile($"eventpipe/{ProviderFileName(providerName)}.cpp", impl.ToString());
        }
    }

    private string GenerateEventPipeWriteSignature(string eventName, Template? template)
    {
        var sb = new StringBuilder();
        if (Extern)
        {
            sb.Append("extern \"C\" ");
        }

        sb.Append($"{EventPipeTypes["ULONG"]} EventPipeWriteEvent{eventName}(");
        if (template is not null)
        {
            sb.Append('\n');
            List<string> parameters = [];
            AppendParameterDeclarations(parameters, template, PalTypes, omitNullCount: true);
            sb.AppendJoin("", parameters);
        }

        sb.Append($"{Indent}{EventPipeTypes["LPCGUID"]} ActivityId,\n");
        sb.Append($"{Indent}{EventPipeTypes["LPCGUID"]} RelatedActivityId)");
        return sb.ToString();
    }

    private string GenerateEventPipeWriteEventsImpl(XElement providerNode, Dictionary<string, Template> allTemplates, Dictionary<string, ulong> keywordMap)
    {
        string providerName = Attr(providerNode, "name");
        string prettyName = ProviderPrettyName(providerName);
        string externC = Extern ? "extern \"C\" " : "";
        List<XElement> eventNodes = ElementsByTagName(providerNode, "event").ToList();
        var sb = new StringBuilder();

        foreach (XElement eventNode in eventNodes)
        {
            string eventName = Attr(eventNode, "symbol");
            if (IncludeEvent(providerName, eventName))
            {
                sb.Append($"EventPipeEvent *EventPipeEvent{eventName} = nullptr;\n");
            }
        }

        sb.Append('\n');

        string isEnabledFunc = _nativeAot ? "ep_event_is_enabled" : "EventPipeAdapter::EventIsEnabled";
        foreach (XElement eventNode in eventNodes)
        {
            string eventName = Attr(eventNode, "symbol");
            if (!IncludeEvent(providerName, eventName))
            {
                continue;
            }

            sb.Append($$"""
                {{EventPipeTypes["BOOL"]}} EventPipeEventEnabled{{eventName}}(void)
                {
                    return {{isEnabledFunc}}(EventPipeEvent{{eventName}});
                }


                """);

            Template? template = GetTemplate(allTemplates, eventNode);
            sb.Append(GenerateEventPipeWriteSignature(eventName, template));
            sb.Append($$"""

                {
                    if (!EventPipeEventEnabled{{eventName}}())
                        return ERROR_SUCCESS;

                """);

            if (template is not null)
            {
                sb.Append(GenerateEventPipeWriteEventBody(template, eventName));
                sb.Append("}\n\n");
            }
            else
            {
                sb.Append(_nativeAot
                    ? $"    ep_write_event (EventPipeEvent{eventName}, (uint8_t *) NULL, 0, (const uint8_t *)ActivityId, (const uint8_t *)RelatedActivityId);\n"
                    : $"    EventPipeAdapter::WriteEvent(EventPipeEvent{eventName}, (BYTE*) nullptr, 0, ActivityId, RelatedActivityId);\n");
                sb.Append("""

                        return ERROR_SUCCESS;
                    }


                    """);
            }
        }

        string callbackName = $"EventPipeEtwCallback{prettyName}";
        string callbackExpr = _nativeAot
            ? $"reinterpret_cast<EventPipeCallback>({callbackName})"
            : $$"""

                #ifdef FEATURE_EVENT_TRACE
                        reinterpret_cast<EventPipeCallback>({{callbackName}})
                #else
                        nullptr
                #endif
                {{Indent}}
                """;
        string createProviderFunc = _nativeAot ? "create_provider" : "EventPipeAdapter::CreateProvider";
        string addEventFunc = _nativeAot ? "ep_provider_add_event" : "EventPipeAdapter::AddEvent";

        sb.Append($$"""
            {{externC}}void Init{{prettyName}}(void);

            {{externC}}void Init{{prettyName}}(void)
            {
                EventPipeProvider{{prettyName}} = {{createProviderFunc}}({{prettyName}}Name, {{callbackExpr}});

            """);
        foreach (XElement eventNode in eventNodes)
        {
            string eventName = Attr(eventNode, "symbol");
            ulong keywordsMask = 0;
            foreach (string keyword in Attr(eventNode, "keywords").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                keywordsMask |= keywordMap[keyword];
            }

            if (!IncludeEvent(providerName, eventName))
            {
                continue;
            }

            string level = Attr(eventNode, "level").Replace("win:", "EP_EVENT_LEVEL_").ToUpperInvariant();
            bool needStack = !_exclusions.NoStack.Any(entry => entry.Split(':')[2] == eventName);
            sb.Append($"    EventPipeEvent{eventName} = {addEventFunc}(EventPipeProvider{prettyName},{Attr(eventNode, "value")},{keywordsMask},{Attr(eventNode, "version")},{level},{(needStack ? "true" : "false")}{(_nativeAot ? ",NULL,0" : "")});\n");
        }

        sb.Append('}');
        return sb.ToString();
    }

    private string PayloadSize(Template template, string paramName, FunctionParameter parameter, bool isStruct)
    {
        if (s_specialCaseSizes.TryGetValue(template.Name, out Dictionary<string, string>? special) && special.TryGetValue(paramName, out string? size))
        {
            return $"(int)({size})";
        }

        return isStruct
            ? $"(int){paramName}_ElementSize * (int){parameter.Prop}"
            : $"sizeof({LttngTypes[parameter.WinType]}) * (int){parameter.Prop}";
    }

    private List<string> PackParameters(Template template)
    {
        List<string> packList = [];
        foreach (string paramName in template.Signature.ParamList)
        {
            FunctionParameter parameter = template.Signature.GetParam(paramName)!;
            if (template.Structs.ContainsKey(paramName) || template.Arrays.ContainsKey(paramName))
            {
                string size = PayloadSize(template, paramName, parameter, template.Structs.ContainsKey(paramName));
                packList.Add($"    success &= WriteToBuffer((const BYTE *){paramName}, {size}, buffer, offset, size, fixedBuffer);");
            }
            else if (parameter.WinType == "win:GUID")
            {
                packList.Add($"    success &= WriteToBuffer(*{parameter.Name}, buffer, offset, size, fixedBuffer);");
            }
            else
            {
                packList.Add($"    success &= WriteToBuffer({parameter.Name}, buffer, offset, size, fixedBuffer);");
            }
        }

        return packList;
    }

    private string GenerateEventPipeWriteEventBody(Template template, string eventName)
    {
        List<string> packList = [];

        // ETW translates NULL strings to "NULL", so EventPipe does the same.
        foreach (FunctionParameter parameter in template.Signature.Parameters)
        {
            if (parameter.WinType == "win:UnicodeString")
            {
                packList.Add($"    if (!{parameter.Name}) {{ {parameter.Name} = W(\"NULL\"); }}");
            }
        }

        List<string> writes = PackParameters(template);
        packList.AddRange(writes);
        bool emittedWriteToBuffer = writes.Count != 0;

        string byteType = EventPipeTypes["BYTE"];
        int estimatedSize = template.EstimatedSize;
        var sb = new StringBuilder();
        sb.Append($$"""

                size_t size = {{estimatedSize}};
                {{byteType}} stackBuffer[{{estimatedSize}}];
                {{byteType}} *buffer = stackBuffer;
                size_t offset = 0;

            """);
        if (emittedWriteToBuffer)
        {
            sb.Append("""
                    bool fixedBuffer = true;
                    bool success = true;

                """);
        }

        sb.Append('\n');
        sb.AppendJoin("\n", packList).Append("\n\n");
        if (emittedWriteToBuffer)
        {
            sb.Append(WriteFailureCheck);
        }

        sb.Append(_nativeAot
            ? $"    ep_write_event (EventPipeEvent{eventName}, (uint8_t *)buffer, (uint32_t)offset, reinterpret_cast<const uint8_t*>(ActivityId), reinterpret_cast<const uint8_t*>(RelatedActivityId));\n"
            : $"    EventPipeAdapter::WriteEvent(EventPipeEvent{eventName}, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);\n");
        if (emittedWriteToBuffer)
        {
            sb.Append(FreeBuffer).Append('\n');
        }

        sb.Append("""

                return ERROR_SUCCESS;

            """);
        return sb.ToString();
    }

    // ---------------------------------------------------------------------------------------------
    // LTTng: lttng/tp<provider>.h, lttng/eventprov<provider>.cpp, lttng/traceptprov<provider>.cpp

    private static bool ShouldPackTemplate(Template template) =>
        template.NumParams > MaxLttngArgs || template.Structs.Count > 0 || template.Arrays.Count > 0;

    private string GenerateLttngArgList(Template template)
    {
        string args;
        if (ShouldPackTemplate(template))
        {
            args = """
                        const unsigned int, length, \
                        const char *, __data__ \

                """;
        }
        else
        {
            List<string> argList = [];
            foreach (FunctionParameter parameter in template.Signature.Parameters)
            {
                string countType = LttngTypes[parameter.Count];
                argList.Add($"        {LttngTypes[parameter.WinType]}{(countType != " " ? countType : "")}, {parameter.Name}");
            }

            args = string.Join(", \\\n", argList) + " \\\n";
        }

        return $$"""
            TP_ARGS( \
            {{args}})

            """;
    }

    private string GenerateLttngFieldList(Template template)
    {
        string fieldList;
        if (ShouldPackTemplate(template))
        {
            fieldList = """
                        ctf_integer(ULONG, length, length)
                        ctf_sequence(char, __data__, __data__, ULONG, length)
                """;
        }
        else
        {
            List<string> fields = [];
            foreach (FunctionParameter parameter in template.Signature.Parameters)
            {
                string typeName = LttngTypes[parameter.WinType].Replace("const ", "");
                string name = parameter.Name;
                string ctfType;
                string fieldBody;
                if (!string.IsNullOrEmpty(parameter.Prop))
                {
                    // An explicit struct is treated as a sequence.
                    ctfType = "ctf_sequence";
                    fieldBody = string.Join(", ", typeName, name, name, "size_t", parameter.Prop);
                }
                else
                {
                    ctfType = s_ctfTypes[parameter.WinType];
                    fieldBody = ctfType switch
                    {
                        "ctf_string" => $"{name}, {name}",
                        "ctf_integer" or "ctf_float" => $"{typeName}, {name}, {name}",
                        "ctf_sequence" => throw new InvalidDataException("ctf_sequence needs to have its memory expilicitly laid out"),
                        _ => throw new InvalidDataException($"no such ctf intrinsic called: {ctfType}"),
                    };
                }

                fields.Add($"        {ctfType}({fieldBody})");
            }

            fieldList = string.Join("\n", fields);
        }

        return $$"""
                 TP_FIELDS(
            {{fieldList}}
                )
            )

            """;
    }

    private string GenerateLttngHeader(string providerName, Dictionary<string, Template> allTemplates, List<XElement> eventNodes)
    {
        var sb = new StringBuilder();
        foreach ((string templateName, Template template) in allTemplates)
        {
            sb.Append($$"""

                #define {{templateName}}_TRACEPOINT_ARGS \

                """);
            sb.Append(GenerateLttngArgList(template));
            sb.Append($$"""
                TRACEPOINT_EVENT_CLASS(
                    {{providerName}},
                    {{templateName}},
                    {{templateName}}_TRACEPOINT_ARGS,

                """);
            sb.Append(GenerateLttngFieldList(template));
            sb.Append($$"""

                #define {{templateName}}T_TRACEPOINT_INSTANCE(name) \
                TRACEPOINT_EVENT_INSTANCE(\
                    {{providerName}},\
                    {{templateName}},\
                    name ,\
                    {{templateName}}_TRACEPOINT_ARGS \
                )
                """);
        }

        // An empty template specifies just the event name in the event stream.
        sb.Append($$"""


            TRACEPOINT_EVENT_CLASS(
                {{providerName}},
                emptyTemplate ,
                TP_ARGS(),
                TP_FIELDS()
            )
            #define T_TRACEPOINT_INSTANCE(name) \
            TRACEPOINT_EVENT_INSTANCE(\
                {{providerName}},\
                emptyTemplate,\
                name ,\
                TP_ARGS()\
            )

            """);

        foreach (XElement eventNode in eventNodes)
        {
            string eventName = Attr(eventNode, "symbol");
            if (eventName.Length == 0)
            {
                throw new InvalidDataException($"{eventNode} event does not have a symbol");
            }

            sb.Append($"{Attr(eventNode, "template")}T_TRACEPOINT_INSTANCE({eventName})\n");
        }

        sb.Append($$"""

            #endif /* LTTNG_CORECLR_H{{providerName}} */
            #include <lttng/tracepoint-event.h>
            """);
        return sb.ToString();
    }

    private string GenerateLttngMethodBody(Template? template, string providerName, string eventName)
    {
        if (template is null)
        {
            return $"\n    do_tracepoint({providerName}, {eventName});\n";
        }

        var sb = new StringBuilder();
        foreach (FunctionParameter parameter in template.Signature.Parameters)
        {
            if (parameter.WinType == "win:UnicodeString")
            {
                string p = parameter.Name;
                sb.Append($$"""
                        size_t {{p}}_path_size = 0;
                        LttngStringBuffer {{p}}_PS;
                        size_t {{p}}_full_name_path_size = (lttng_strlen16({{p}}) + 1)*sizeof(WCHAR);
                        CHAR* {{p}}_full_name = {{p}}_PS.Open({{p}}_full_name_path_size );
                        if ({{p}}_full_name == NULL )    { return ERROR_WRITE_FAULT; }

                    """);
            }
        }

        sb.Append('\n');

        if (!ShouldPackTemplate(template))
        {
            List<string> lineBody = [$"    do_tracepoint({providerName},\n        {eventName}"];
            foreach (FunctionParameter parameter in template.Signature.Parameters)
            {
                string p = parameter.Name;
                string ctfType = s_ctfTypes.GetValueOrDefault(parameter.Count) ?? s_ctfTypes[parameter.WinType];
                string line = "        ";
                if (ctfType == "ctf_string" && parameter.WinType == "win:UnicodeString")
                {
                    // Convert the UTF-16 string to UTF-8.
                    sb.Append($$"""
                            {{p}}_path_size = minipal_convert_utf16_to_utf8((const CHAR16_T*){{p}}, lttng_strlen16({{p}}) + 1, {{p}}_full_name, {{p}}_full_name_path_size, 0);
                            _ASSERTE({{p}}_path_size < {{p}}_full_name_path_size );
                            if( {{p}}_path_size == 0 ){ return ERROR_INVALID_PARAMETER; }

                        """);
                    lineBody.Add(line + p + "_full_name");
                }
                else if (ctfType == "ctf_sequence" || parameter.WinType == "win:Pointer")
                {
                    string countType = LttngTypes[parameter.Count];
                    lineBody.Add($"{line}({LttngTypes[parameter.WinType]}{(countType != " " ? countType : "")}) {p}");
                }
                else
                {
                    lineBody.Add(line + p);
                }
            }

            sb.AppendJoin(",\n", lineBody).Append(");\n");
            return sb.ToString();
        }

        List<string> packList = PackParameters(template);
        bool emittedWriteToBuffer = packList.Count != 0;
        int estimatedSize = template.EstimatedSize;
        var packed = new StringBuilder($$"""

                size_t size = {{estimatedSize}};
                char stackBuffer[{{estimatedSize}}];
                char *buffer = stackBuffer;
                size_t offset = 0;

            """);
        if (emittedWriteToBuffer)
        {
            packed.Append("""

                    bool fixedBuffer = true;
                    bool success = true;

                """);
        }

        packed.AppendJoin("\n", packList).Append("\n\n");
        if (emittedWriteToBuffer)
        {
            packed.Append(WriteFailureCheck);
            packed.Append($"    do_tracepoint({providerName}, {eventName}, offset, buffer);\n");
            packed.Append(FreeBuffer);
        }

        return packed.ToString();
    }

    private string GenerateLttngTpProvider(string providerName, List<XElement> eventNodes, Dictionary<string, Template> allTemplates)
    {
        var sb = new StringBuilder(Templates.LttngStrlen16);
        foreach (XElement eventNode in eventNodes)
        {
            string eventName = Attr(eventNode, "symbol");
            sb.Append($"extern \"C\" BOOL  EventXplatEnabled{eventName}(){{ return tracepoint_enabled({providerName}, {eventName}); }}\n\n");

            List<string> fnPrototype = [$"extern \"C\" ULONG  FireEtXplat{eventName}(\n"];
            Template? template = GetTemplate(allTemplates, eventNode);
            if (template is not null)
            {
                List<string> parameters = [];
                AppendParameterDeclarations(parameters, template, PalTypes, omitNullCount: true);
                RemoveLast(parameters);
                fnPrototype.AddRange(parameters);
            }

            fnPrototype.Add(")\n{\n");
            sb.AppendJoin("", fnPrototype);
            sb.Append($$"""
                    if (!EventXplatEnabled{{eventName}}())
                        return ERROR_SUCCESS;

                """);
            sb.Append(GenerateLttngMethodBody(template, providerName, eventName));
            sb.Append("""

                    return ERROR_SUCCESS;
                }


                """);
        }

        return sb.ToString();
    }

    private void GenerateLttngFiles()
    {
        foreach (XElement providerNode in Providers)
        {
            string providerName = ProviderPrettyName(Attr(providerNode, "name"));
            string fileName = ProviderFileName(Attr(providerNode, "name"));
            string headerName = $"tp{fileName}.h";
            List<XElement> eventNodes = ElementsByTagName(providerNode, "event").ToList();
            Dictionary<string, Template> allTemplates = ParseTemplates(providerNode);

            var header = new StringBuilder(Prologs.Lttng).Append('\n');
            header.Append($$"""

                #include "palrt.h"
                #include "pal.h"

                #undef TRACEPOINT_PROVIDER

                #define TRACEPOINT_PROVIDER {{providerName}}


                #undef TRACEPOINT_INCLUDE
                #define TRACEPOINT_INCLUDE "./{{headerName}}"

                #if !defined(LTTNG_CORECLR_H{{providerName}}) || defined(TRACEPOINT_HEADER_MULTI_READ)

                #define LTTNG_CORECLR_H{{providerName}}

                #include <lttng/tracepoint.h>


                """);
            header.Append(GenerateLttngHeader(providerName, allTemplates, eventNodes)).Append('\n');
            WriteFile($"lttng/{headerName}", header.ToString());

            var impl = new StringBuilder(Prologs.Lttng).Append('\n');
            impl.Append(Templates.LttngImplIncludes);
            impl.Append($"#include \"{headerName}\"\n\n");
            impl.Append(Templates.LttngImplPrefix);
            impl.Append(GenerateLttngTpProvider(providerName, eventNodes, allTemplates)).Append('\n');
            WriteFile($"lttng/eventprov{fileName}.cpp", impl.ToString());

            var tracepoints = new StringBuilder(Prologs.Lttng).Append('\n');
            tracepoints.Append($$"""

                #define TRACEPOINT_CREATE_PROBES
                #include "./{{headerName}}"

                """);
            WriteFile($"lttng/traceptprov{fileName}.cpp", tracepoints.ToString());
        }
    }

    // ---------------------------------------------------------------------------------------------
    // ETW (Windows): etw/* compiled by mc.exe, etw/etwmacros.h and clrxplatevents.h

    private void GenerateEtwFiles()
    {
        string etwDir = Path.Combine(_outputDir, "etw");
        if (_dryRun)
        {
            foreach (string output in s_mcOutputs)
            {
                _outputs.Add(Path.Combine(etwDir, output).Replace('\\', '/'));
            }
        }
        else
        {
            string tempDir = Path.Combine(_outputDir, "etw_temp");
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }

            Directory.CreateDirectory(tempDir);
            RunMessageCompiler(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "etwmacros.h"), NormalizeNewLines(GenerateEtwMacroHeader()), Encoding.ASCII);
            UpdateDirectory(tempDir, etwDir);
            Directory.Delete(tempDir, recursive: true);
        }

        WriteFile("clrxplatevents.h", """

            #ifndef _CLR_XPLAT_EVENTS_H_
            #define _CLR_XPLAT_EVENTS_H_

            #include "etw/etwmacros.h"
            #include "etw/ClrEtwAll.h"

            #endif //_CLR_XPLAT_EVENTS_H_

            """);

        if (_dryRun)
        {
            _outputs.Add(Path.Combine(etwDir, "etwmacros.h").Replace('\\', '/'));
        }
    }

    private void RunMessageCompiler(string tempDir)
    {
        var startInfo = new ProcessStartInfo("mc.exe") { UseShellExecute = false };
        foreach (string argument in new[] { "-h", tempDir, "-r", tempDir, "-b", "-co", "-um", "-p", "FireEtXplat", _manifestPath })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start mc.exe");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"mc.exe failed with exit code {process.ExitCode}");
        }

        string headerPath = Path.Combine(tempDir, "ClrEtwAll.h");
        string header = File.ReadAllText(headerPath, Encoding.Latin1).Replace("\r\n", "\n").Replace('\r', '\n');
        header = header.Replace("EventEnabled", "EventXplatEnabled");
        header = Regex.Replace(header, @"\bPVOID\b", "void*");

        // The MC code generator miscomputes the size of events with variable size arrays of variable
        // size arrays (GCBulkType). See https://github.com/dotnet/coreclr/pull/25454.
        int count = 0;
        header = Regex.Replace(header, @"_Arg0 \* _Arg2_Len_", _ => { count++; return "_Arg2_Len_"; });
        if (count != 1)
        {
            throw new InvalidOperationException("The workaround for https://github.com/dotnet/coreclr/pull/25454 could not be applied. Has the generated code changed or perhaps the underlying issue has been fixed?");
        }

        File.WriteAllText(headerPath, NormalizeNewLines(header), Encoding.Latin1);
    }

    private string GenerateEtwMacroHeader()
    {
        const int MaxEventBytesPerProvider = 64;
        List<XElement> providers = Providers.ToList();
        var sb = new StringBuilder(Prologs.Etw).Append('\n');
        sb.Append($"#define NO_OF_ETW_PROVIDERS {providers.Count}\n");
        sb.Append($"#define MAX_BYTES_PER_ETW_PROVIDER {MaxEventBytesPerProvider}\n");
        sb.Append("EXTERN_C inline constexpr BYTE etwStackSupportedEvents[NO_OF_ETW_PROVIDERS][MAX_BYTES_PER_ETW_PROVIDER] = \n{\n");

        foreach (XElement providerNode in providers)
        {
            int[] stackSupportedEvents = new int[MaxEventBytesPerProvider];
            string eventProvider = Attr(providerNode, "name");
            foreach (XElement eventNode in ElementsByTagName(providerNode, "event"))
            {
                string taskName = Attr(eventNode, "task");
                string eventSymbol = Attr(eventNode, "symbol");
                int eventValue = int.Parse(Attr(eventNode, "value"));
                int bitPosition = eventValue % 8;

                // The bit is set iff a stack should be collected for the event.
                bool noStackBit = GetStackWalkBit(eventProvider, taskName, eventSymbol, _exclusions.NoStack);
                bool explicitStackBit = GetStackWalkBit(eventProvider, taskName, eventSymbol, _exclusions.ExplicitStack);
                if (noStackBit || !explicitStackBit)
                {
                    stackSupportedEvents[eventValue / 8] |= 1 << bitPosition;
                }
            }

            sb.Append("\t{").AppendJoin(", ", stackSupportedEvents).Append("},\n");
        }

        sb.Append("};\n");
        return sb.ToString();
    }

    // Mirrors the source directory into the destination, only touching files whose content changed.
    private static void UpdateDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        HashSet<string> sourceFiles = Directory.GetFiles(sourceDir).Select(f => Path.GetFileName(f)).ToHashSet();
        HashSet<string> sourceDirs = Directory.GetDirectories(sourceDir).Select(d => Path.GetFileName(d)).ToHashSet();

        foreach (string file in sourceFiles)
        {
            UpdateFile(Path.Combine(destinationDir, file), File.ReadAllBytes(Path.Combine(sourceDir, file)));
        }

        foreach (string file in Directory.GetFiles(destinationDir))
        {
            if (!sourceFiles.Contains(Path.GetFileName(file)))
            {
                File.Delete(file);
            }
        }

        foreach (string dir in Directory.GetDirectories(destinationDir))
        {
            if (!sourceDirs.Contains(Path.GetFileName(dir)))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        foreach (string dir in sourceDirs)
        {
            UpdateDirectory(Path.Combine(sourceDir, dir), Path.Combine(destinationDir, dir));
        }
    }
}

static class Prologs
{
    public const string Eventing = """

        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.

        /******************************************************************

        DO NOT MODIFY. AUTOGENERATED FILE.
        This file is generated using the logic from <root>/src/scripts/genEventing.py

        ******************************************************************/

        """;

    public const string EventPipe = """
        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.

        /******************************************************************

        DO NOT MODIFY. AUTOGENERATED FILE.
        This file is generated using the logic from <root>/src/scripts/genEventPipe.py

        ******************************************************************/


        """;

    public const string Lttng = """

        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.

        /******************************************************************

        DO NOT MODIFY. AUTOGENERATED FILE.
        This file is generated using the logic from <root>/src/scripts/genLttngProvider.py

        ******************************************************************/

        """;

    public const string Etw = """

        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.

        /******************************************************************

        DO NOT MODIFY. AUTOGENERATED FILE.
        This file is generated using the logic from <root>/src/scripts/genEtwProvider.py

        ******************************************************************/

        """;
}

static class Templates
{
    private const string TypeAdaptionDefines = """
        #ifndef W
        #define W(str) L##str
        #endif
        #ifndef SL
        #define SL(str) str
        #endif
        #ifndef ERROR_SUCCESS
        #define ERROR_SUCCESS 0L
        #endif
        #ifndef ERROR_WRITE_FAULT
        #define ERROR_WRITE_FAULT 29L
        #endif
        """;

    private const string NativeAotEventPipeIncludes = """
        #include <common.h>
        #include <gcenv.h>

        #include <eventtrace_context.h>
        #include <gcheaputilities.h>

        #include <eventpipe/ep.h>
        #include <eventpipe/ep-provider.h>
        #include <eventpipe/ep-config.h>
        #include <eventpipe/ep-event.h>
        #include <eventpipe/ep-event-instance.h>
        #include <eventpipe/ep-session.h>
        #include <eventpipe/ep-session-provider.h>
        #include <eventpipe/ep-metadata-generator.h>
        #include <eventpipe/ep-event-payload.h>
        #include <eventpipe/ep-buffer-manager.h>
        """;

    private const string ResizeBuffer = """
        bool ResizeBuffer(BYTE *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer)
        {
            newSize = (size_t)(newSize * 1.5);
            _ASSERTE(newSize > size); // check for overflow

            if (newSize < 32)
                newSize = 32;

            BYTE *newBuffer = new (nothrow) BYTE[newSize];

            if (newBuffer == NULL)
                return false;

            memcpy(newBuffer, buffer, currLen);

            if (!fixedBuffer)
                delete[] buffer;

            buffer = newBuffer;
            size = newSize;
            fixedBuffer = false;

            return true;
        }

        bool WriteToBuffer(const BYTE *src, size_t len, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (!src) return true;
            if (offset + len > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + len, fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, src, len);
            offset += len;
            return true;
        }
        """;

    private const string WriteAnsiStringToBuffer = """
        bool WriteToBuffer(const char *str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (!str) return true;
            size_t len = strlen(str) + 1;
            if (offset + len > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + len, fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, str, len);
            offset += len;
            return true;
        }
        """;

    public const string CoreClrEventPipeHelperPrefix = $$"""

        #include "common.h"
        #include <stdlib.h>
        #include <string.h>

        #ifndef TARGET_UNIX
        #include <windef.h>
        #include <crtdbg.h>
        #else
        #include "pal.h"
        #endif //TARGET_UNIX

        {{ResizeBuffer}}

        bool WriteToBuffer(PCWSTR str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (!str) return true;
            size_t byteCount = (u16_strlen(str) + 1) * sizeof(*str);

            if (offset + byteCount > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + byteCount, fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, str, byteCount);
            offset += byteCount;
            return true;
        }

        {{WriteAnsiStringToBuffer}}


        """;

    public const string NativeAotEventPipeHelperPrefix = $$"""

        {{NativeAotEventPipeIncludes}}


        {{TypeAdaptionDefines}}


        {{ResizeBuffer}}

        bool WriteToBuffer(const WCHAR*  str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (!str) return true;
            size_t byteCount = (ep_rt_utf16_string_len(reinterpret_cast<const ep_char16_t*>(str)) + 1) * sizeof(*str);

            if (offset + byteCount > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + byteCount, fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, str, byteCount);
            offset += byteCount;
            return true;
        }

        {{WriteAnsiStringToBuffer}}

        EventPipeProvider * create_provider(const WCHAR* providerName, EventPipeCallback callback, void* pCallbackContext = nullptr)
        {
            ep_char8_t *providerNameUTF8 = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(providerName));
            EventPipeProvider * provider = ep_create_provider (providerNameUTF8, callback, pCallbackContext);
            ep_rt_utf8_string_free (providerNameUTF8);
            return provider;
        }


        """;

    private const string WriteValueToBuffer = """
        template <typename T>
        bool WriteToBuffer(const T &value, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (sizeof(T) + offset > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + sizeof(T), fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, (char *)&value, sizeof(T));
            offset += sizeof(T);
            return true;
        }
        """;

    public const string CoreClrEventPipeImplPrefix = $$"""
        #include <common.h>
        #include "eventpipeadapter.h"

        bool ResizeBuffer(BYTE *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer);
        bool WriteToBuffer(PCWSTR str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);
        bool WriteToBuffer(const char *str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);
        bool WriteToBuffer(const BYTE *src, size_t len, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);

        {{WriteValueToBuffer}}

        """;

    public const string NativeAotEventPipeImplPrefix = $$"""

        {{NativeAotEventPipeIncludes}}


        {{TypeAdaptionDefines}}


        bool ResizeBuffer(BYTE *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer);
        bool WriteToBuffer(const WCHAR* str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);
        bool WriteToBuffer(const BYTE *src, size_t len, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);

        EventPipeProvider * create_provider(const WCHAR*, EventPipeCallback, void* pCallbackContext = nullptr);

        {{WriteValueToBuffer}}


        """;

    public const string NativeAotEventPipeImplSuffix = """

        bool DotNETRuntimeProvider_IsEnabled(unsigned char level, unsigned long long keyword)
        {
            if (!ep_enabled())
                return false;

            EVENTPIPE_TRACE_CONTEXT& context = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.EventPipeProvider;
            if (!context.IsEnabled)
                return false;

            if (level > context.Level)
                return false;

            return (keyword == (ULONGLONG)0) || (keyword & context.EnabledKeywordsBitmask) != 0;
        }

        bool DotNETRuntimeRundownProvider_IsEnabled(unsigned char level, unsigned long long keyword)
        {
            if (!ep_enabled())
                return false;

            EVENTPIPE_TRACE_CONTEXT& context = MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider;
            if (!context.IsEnabled)
                return false;

            if (level > context.Level)
                return false;

            return (keyword == (ULONGLONG)0) || (keyword & context.EnabledKeywordsBitmask) != 0;
        }

        """;

    public const string LttngImplIncludes = """

        #define TRACEPOINT_DEFINE
        #define TRACEPOINT_PROBE_DYNAMIC_LINKAGE

        #include "stdlib.h"
        #include "pal_mstypes.h"
        #include "pal_error.h"
        #include "pal.h"
        #include <minipal/utf8.h>

        """;

    public const string LttngImplPrefix = """
        #ifndef tracepoint_enabled

        extern "C" bool XplatEventLoggerIsEnabled();

        #define tracepoint_enabled(provider, name) XplatEventLoggerIsEnabled()
        #define do_tracepoint tracepoint
        #endif

        bool ResizeBuffer(char *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer);
        bool WriteToBuffer(PCWSTR str, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);
        bool WriteToBuffer(const char *str, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);
        bool WriteToBuffer(const BYTE *src, size_t len, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);

        template <typename T>
        bool WriteToBuffer(const T &value, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
        {
            if (sizeof(T) + offset > size)
            {
                if (!ResizeBuffer(buffer, size, offset, size + sizeof(T), fixedBuffer))
                    return false;
            }

            memcpy(buffer + offset, (char *)&value, sizeof(T));
            offset += sizeof(T);
            return true;
        }

        // Buffer for converting a string argument to UTF-8; long strings are moved to the heap.
        class LttngStringBuffer
        {
            char m_stackBuffer[MAX_PATH];
            char* m_buffer = m_stackBuffer;

        public:
            LttngStringBuffer() = default;
            LttngStringBuffer(const LttngStringBuffer&) = delete;
            LttngStringBuffer& operator=(const LttngStringBuffer&) = delete;

            ~LttngStringBuffer()
            {
                if (m_buffer != m_stackBuffer)
                    free(m_buffer);
            }

            char* Open(size_t size)
            {
                if (size > sizeof(m_stackBuffer))
                    m_buffer = (char*)malloc(size);
                return m_buffer;
            }
        };


        """;

    public const string LttngStrlen16 = """
        // Local implementation of WCHAR (UTF-16) string length
        static size_t lttng_strlen16(const WCHAR* string)
        {
            size_t nChar = 0;
            while (*string++)
                nChar++;
            return nChar;
        }

        """;
}
