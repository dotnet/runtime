// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

using ILLink.Tasks;

namespace CoreCLR.Tools;

internal static class ECallManifestGenerator
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
    };

    private static readonly ECallManifestJsonContext s_manifestJsonContext = new(s_jsonOptions);

    private static readonly Dictionary<string, string> s_primitives = new(StringComparer.Ordinal)
    {
        ["b"] = "System.Byte",
        ["u"] = "System.Char",
        ["d"] = "System.Double",
        ["f"] = "System.Single",
        ["i"] = "System.Int32",
        ["K"] = "System.UInt32",
        ["I"] = "System.IntPtr",
        ["U"] = "System.UIntPtr",
        ["l"] = "System.Int64",
        ["L"] = "System.UInt64",
        ["h"] = "System.Int16",
        ["H"] = "System.UInt16",
        ["v"] = "System.Void",
        ["B"] = "System.SByte",
        ["F"] = "System.Boolean",
        ["j"] = "System.Object",
        ["s"] = "System.String",
        ["T"] = "System.TypedReference",
    };

    private static readonly string[] s_ecallMacros =
    [
        "FCFuncStart",
        "FCFuncEnd",
        "FCFuncElement",
        "FCFuncElementSig",
        "FCDynamic",
        "FCDynamicSig",
        "FCClassElement",
    ];

    internal static int Run(string[] args)
    {
        try
        {
            if (args is ["-h" or "--help"])
            {
                Console.WriteLine("GenECalls --source <vm-directory> --output <directory> --target-os <browser|wasi> --defines <comma-separated-defines>");
                return 0;
            }

            Dictionary<string, List<string>> options = ParseArguments(args);
            string input = GetOption("--source");
            string output = GetOption("--output");
            string targetOS = GetOption("--target-os");
            string manifest = Generate(input, targetOS,
                GetOption("--defines").Split(',', StringSplitOptions.RemoveEmptyEntries));
            Directory.CreateDirectory(output);
            WriteIfChanged(Path.Combine(output, "ecall-manifest.json"), manifest);
            return 0;

            string GetOption(string name)
            {
                if (!options.TryGetValue(name, out List<string>? values))
                    throw new ArgumentException($"Missing required option: {name}");
                return values[0];
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or JsonException)
        {
            Console.Error.WriteLine($"GenECalls: error: {ex.Message}");
            return 1;
        }
    }

    private static Dictionary<string, List<string>> ParseArguments(string[] args)
    {
        var expanded = new List<string>();
        var responseFiles = new HashSet<string>(StringComparer.Ordinal);
        Expand(args);
        var options = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int i = 0; i < expanded.Count; i++)
        {
            string option = expanded[i];
            if (option is not ("--source" or "--output" or "--target-os" or "--defines"))
                throw new ArgumentException($"Unknown option: {option}");
            var values = new List<string>();
            if (i + 1 < expanded.Count && !expanded[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values.Add(expanded[++i]);
            }
            if (values.Count == 0)
                throw new ArgumentException($"Missing value for option: {option}");
            if (!options.TryAdd(option, values))
                throw new ArgumentException($"Repeated option: {option}");
        }
        return options;

        void Expand(IEnumerable<string> arguments)
        {
            foreach (string argument in arguments)
            {
                if (!argument.StartsWith('@'))
                {
                    expanded.Add(argument);
                    continue;
                }
                string path = Path.GetFullPath(argument[1..]);
                if (!responseFiles.Add(path))
                    throw new ArgumentException($"Recursive response file: {path}");
                Expand(File.ReadAllLines(path));
                responseFiles.Remove(path);
            }
        }
    }

    internal static List<string> ParseTypes(string text, IReadOnlyDictionary<string, string> types)
    {
        var tokens = new List<string>();
        for (int i = 0; i < text.Length;)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            int start = i++;
            if (IsIdentifierStart(c))
            {
                while (i < text.Length && IsIdentifierPart(text[i]))
                    i++;
            }
            else if (char.IsAsciiDigit(c))
            {
                while (i < text.Length && char.IsAsciiDigit(text[i]))
                    i++;
            }
            else if (c is not ('(' or ')' or ','))
            {
                throw new InvalidDataException($"Unsupported token in ECall metasig: {text}");
            }
            tokens.Add(text[start..i]);
        }
        int position = 0;
        var result = new List<string>();
        while (position != tokens.Count)
        {
            if (tokens[position] == "_")
                Take("_");
            else
                result.Add(ReadType());
        }
        return result;

        string Take(string? expected = null)
        {
            if (position == tokens.Count)
                throw new InvalidDataException($"Incomplete metasig: {text}");
            string token = tokens[position++];
            if (expected is not null && token != expected)
                throw new InvalidDataException($"Expected {expected}, found {token} in metasig: {text}");
            return token;
        }

        string ReadType()
        {
            string token = Take();
            if (s_primitives.TryGetValue(token, out string? primitive))
                return primitive;
            Take("(");
            string type;
            switch (token)
            {
                case "C" or "g":
                    string id = Take();
                    if (!types.TryGetValue(id, out string? name))
                        throw new InvalidDataException($"Unknown type {id} in metasig: {text}");
                    type = name;
                    break;
                case "a" or "P" or "r":
                    type = ReadType() + (token switch { "a" => "[]", "P" => "*", _ => "&" });
                    break;
                case "GI":
                    string definition = ReadType();
                    Take(",");
                    if (!int.TryParse(Take(), NumberStyles.None, CultureInfo.InvariantCulture, out int count))
                        throw new InvalidDataException($"Invalid generic argument count in metasig: {text}");
                    Take(",");
                    var arguments = new List<string>();
                    for (int i = 0; i < count; i++)
                        arguments.Add(ReadType());
                    type = definition + "<" + string.Join(",", arguments) + ">";
                    break;
                default:
                    throw new InvalidDataException($"Unsupported type {token} in ECall metasig: {text}");
            }
            Take(")");
            return type;
        }
    }

    internal static string Generate(string sourceDirectory, string targetOS, IEnumerable<string> defineConstants)
    {
        if (targetOS is not ("browser" or "wasi"))
            throw new ArgumentException($"Unsupported ECall target OS: {targetOS}");

        var defines = new HashSet<string>(defineConstants, StringComparer.Ordinal);
        Dictionary<string, string> namespaces = RuntimeHeaderParser.ReadNamespaces(
            File.ReadLines(Path.Combine(sourceDirectory, "namespace.h")));
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((int _, string line) in ReadHeader("corelib.h", ["DEFINE_CLASS"]))
        {
            if (!TryReadMacro(line, out string macro, out _) || macro != "DEFINE_CLASS")
                continue;
            string[] args = ReadMacro(line, "DEFINE_CLASS", 3);
            if (!namespaces.TryGetValue(args[1], out string? ns))
                throw new InvalidDataException($"Unknown namespace {args[1]} in corelib.h.");
            if (!types.TryAdd(args[0], ns + "." + args[2]))
                throw new InvalidDataException($"Duplicate binder type: {args[0]}");
        }

        var signatures = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach ((int _, string line) in ReadHeader("metasig.h", ["DEFINE_METASIG", "DEFINE_METASIG_T"]))
        {
            if (!TryReadMacro(line, out string macro, out _))
                continue;
            if (macro is not ("DEFINE_METASIG" or "DEFINE_METASIG_T"))
                continue;
            string body = ReadMacro(line, macro, 1)[0];
            if (!TryReadMacro(body, out string kind, out _))
                throw new InvalidDataException($"Invalid metasig definition: {line}");
            // Only SM and IM signatures can describe non-generic InternalCall methods.
            if (kind is not ("SM" or "IM"))
                continue;
            string[] args = ReadMacro(body, kind, 3);
            if (!signatures.TryAdd(kind + "_" + args[0], args))
                throw new InvalidDataException($"Duplicate metasig: {kind}_{args[0]}");
        }

        var tables = new Dictionary<string, List<ECallManifestEntry>>(StringComparer.Ordinal);
        var classes = new Dictionary<string, (string Name, string Namespace)>(StringComparer.Ordinal);
        var classNames = new HashSet<(string, string)>();
        List<ECallManifestEntry>? currentTable = null;
        foreach ((int lineNumber, string line) in ReadHeader("ecalllist.h", s_ecallMacros))
        {
            if (!TryReadMacro(line, out string macro, out _))
                throw new InvalidDataException($"Invalid ECall definition at ecalllist.h:{lineNumber}: {line}");
            switch (macro)
            {
                case "FCFuncStart":
                    string tableName = ReadMacro(line, macro, 1)[0];
                    if (currentTable is not null || !tables.TryAdd(tableName, currentTable = []))
                        throw new InvalidDataException($"Duplicate or nested ECall table: {tableName}");
                    break;
                case "FCFuncEnd":
                    ReadMacro(line, macro, 0);
                    if (currentTable is null)
                        throw new InvalidDataException("Unbalanced ECall table.");
                    currentTable = null;
                    break;
                case "FCClassElement":
                    string[] cls = ReadMacro(line, macro, 3);
                    string name = ReadName(cls[0]);
                    string ns = ReadNamespace(cls[1]);
                    if (currentTable is not null || !classes.TryAdd(cls[2], (name, ns)) || !classNames.Add((name, ns)))
                        throw new InvalidDataException($"Duplicate or nested ECall class mapping: {line}");
                    break;
                case "FCFuncElement" or "FCFuncElementSig" or "FCDynamic" or "FCDynamicSig":
                    if (currentTable is null)
                        throw new InvalidDataException($"ECall outside a table at ecalllist.h:{lineNumber}.");
                    bool hasSignature = macro.EndsWith("Sig", StringComparison.Ordinal);
                    string[] entry = ReadMacro(line, macro, hasSignature ? 3 : 2);
                    currentTable.Add(new ECallManifestEntry
                    {
                        Name = entry[0] == "COR_CTOR_METHOD_NAME" ? ".ctor" : ReadName(entry[0]),
                        Signature = hasSignature ? ReadSignature(entry[1]) : null,
                        Symbol = $"ecall_entry_{lineNumber}",
                    });
                    break;
                default:
                    throw new InvalidDataException($"Unsupported ECall macro at ecalllist.h:{lineNumber}: {macro}");
            }
        }
        if (currentTable is not null)
            throw new InvalidDataException("Unterminated ECall table.");

        var manifestClasses = new List<ECallManifestClass>();
        foreach (KeyValuePair<string, (string Name, string Namespace)> cls in classes)
        {
            if (!tables.Remove(cls.Key, out List<ECallManifestEntry>? entries))
                throw new InvalidDataException($"Missing ECall table: {cls.Key}");
            if (entries.Count != 0)
                manifestClasses.Add(new ECallManifestClass { Name = cls.Value.Name, Namespace = cls.Value.Namespace, Entries = entries.ToArray() });
        }
        if (tables.Count != 0)
            throw new InvalidDataException("ECall tables without class mappings.");
        manifestClasses.Sort(static (left, right) =>
        {
            int order = StringComparer.Ordinal.Compare(left.Name, right.Name);
            return order != 0 ? order : StringComparer.Ordinal.Compare(left.Namespace, right.Namespace);
        });

        var manifest = new ECallManifest { Version = 1, TargetOS = targetOS, Classes = manifestClasses.ToArray() };
        return JsonSerializer.Serialize(manifest, s_manifestJsonContext.ECallManifest) + "\n";

        IEnumerable<(int Line, string Text)> ReadHeader(string name, string[] macros)
        {
            string path = Path.Combine(sourceDirectory, name);
            var constants = new HashSet<string>(defines, StringComparer.Ordinal);
            constants.UnionWith(macros);
            var tracker = new RuntimeHeaderParser.DefineTracker(constants, message => throw new InvalidDataException(message), path);
            int lineNumber = 0;
            int startLine = 1;
            bool inComment = false;
            bool continuing = false;
            var continuedLine = new StringBuilder();
            foreach (string rawLine in File.ReadLines(path))
            {
                lineNumber++;
                if (!continuing)
                    startLine = lineNumber;
                if (rawLine.EndsWith('\\'))
                {
                    continuedLine.Append(rawLine.AsSpan(0, rawLine.Length - 1));
                    continuing = true;
                    continue;
                }
                continuing = false;
                continuedLine.Append(rawLine);
                string line = StripComments(continuedLine.ToString(), ref inComment).Trim();
                continuedLine.Clear();
                if (tracker.ProcessLine(line, startLine) || !tracker.IsActiveSection || line.Length == 0)
                    continue;
                if (line.StartsWith("#define ", StringComparison.Ordinal) || line.StartsWith("#undef ", StringComparison.Ordinal))
                    continue;
                if (line.StartsWith('#'))
                    throw new InvalidDataException($"Unsupported directive at {path}:{lineNumber}: {line}");
                // Native __LINE__ expansion uses the final physical line of continued macros.
                yield return (lineNumber, line);
            }
            if (continuing)
                throw new InvalidDataException($"Unterminated line continuation at {path}:{startLine}.");
            tracker.Complete();
            if (inComment)
                throw new InvalidDataException($"Unterminated comment in {path}.");
        }

        string ReadNamespace(string expression)
        {
            if (expression.StartsWith("g_", StringComparison.Ordinal) && expression.EndsWith("NS", StringComparison.Ordinal) &&
                namespaces.TryGetValue(expression[2..^2], out string? ns))
                return ns;
            return ReadName(expression);
        }

        string ReadSignature(string expression)
        {
            if (!expression.StartsWith("&gsig_", StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported ECall signature expression: {expression}");
            string name = expression["&gsig_".Length..];
            if (!signatures.TryGetValue(name, out string[]? sig))
                throw new InvalidDataException($"Missing ECall signature: {name}");
            List<string> result = ParseTypes(sig[2], types);
            if (result.Count != 1)
                throw new InvalidDataException($"Invalid return type for ECall signature: {name}");
            return (name.StartsWith("IM_", StringComparison.Ordinal) ? "instance" : "static") + " " + result[0] +
                "(" + string.Join(",", ParseTypes(sig[1], types)) + ")";
        }
    }

    private static string ReadName(string text)
    {
        if (text.Length < 3 || text[0] != '"' || text[^1] != '"' || text.AsSpan(1, text.Length - 2).ContainsAny('"', '\\'))
            throw new InvalidDataException($"Expected a nonempty string literal, found: {text}");
        return text[1..^1];
    }

    private static bool IsIdentifierStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    private static bool TryReadMacro(string line, out string macro, out string body)
    {
        macro = body = string.Empty;
        int open = line.IndexOf('(');
        if (open <= 0 || !line.EndsWith(')'))
            return false;
        ReadOnlySpan<char> name = line.AsSpan(0, open).TrimEnd();
        if (name.IsEmpty || !IsIdentifierStart(name[0]))
            return false;
        foreach (char c in name)
        {
            if (!IsIdentifierPart(c))
                return false;
        }
        macro = name.ToString();
        body = line[(open + 1)..^1].Trim();
        return true;
    }

    private static string[] ReadMacro(string line, string macro, int argumentCount)
    {
        if (!TryReadMacro(line, out string name, out string body) || name != macro)
            throw new InvalidDataException($"Invalid {macro} definition: {line}");
        var args = new List<string>();
        int depth = 0;
        int start = 0;
        bool quoted = false;
        for (int i = 0; i < body.Length; i++)
        {
            char c = body[i];
            if (c == '\\' && quoted)
                i++;
            else if (c == '"')
                quoted = !quoted;
            else if (!quoted)
            {
                if (c == '(')
                    depth++;
                else if (c == ')' && --depth < 0)
                    throw new InvalidDataException($"Unbalanced parentheses: {line}");
                else if (c == ',' && depth == 0)
                {
                    args.Add(body[start..i].Trim());
                    start = i + 1;
                }
            }
        }
        if (depth != 0 || quoted)
            throw new InvalidDataException($"Unbalanced macro arguments: {line}");
        if (body.Length != 0)
            args.Add(body[start..].Trim());
        if (args.Count != argumentCount)
            throw new InvalidDataException($"Expected {argumentCount} arguments for {macro}, found {args.Count}: {line}");
        return args.ToArray();
    }

    private static string StripComments(string line, ref bool inComment)
    {
        var result = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            char next = i + 1 < line.Length ? line[i + 1] : '\0';
            if (inComment)
            {
                if (c == '*' && next == '/')
                {
                    inComment = false;
                    i++;
                }
            }
            else if (c == '\\' && quoted && next != '\0')
            {
                result.Append(c).Append(next);
                i++;
            }
            else if (c == '"')
            {
                quoted = !quoted;
                result.Append(c);
            }
            else if (!quoted && c == '/' && next == '/')
                break;
            else if (!quoted && c == '/' && next == '*')
            {
                inComment = true;
                result.Append(' ');
                i++;
            }
            else
                result.Append(c);
        }
        return result.ToString();
    }

    internal static void WriteIfChanged(string path, string content)
    {
        if (!File.Exists(path) || File.ReadAllText(path) != content)
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
