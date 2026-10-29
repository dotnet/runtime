// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json;

using CoreCLR.Tools;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.PortableCallHelpers;

internal sealed class ECallTableGenerator
{
    private const string DescriptorSymbolPrefix = "ecall_entry_";

    private sealed class Entry(string name, string signature, string symbol)
    {
        public readonly string Name = name;
        public readonly string Signature = signature;
        public readonly string Symbol = symbol;
    }

    private sealed class Class(string name, string ns, ImmutableArray<Entry> entries)
    {
        public readonly string Name = name;
        public readonly string Namespace = ns;
        public readonly ImmutableArray<Entry> Entries = entries;
    }

    private readonly List<Class> _classes = [];
    private readonly Dictionary<(string Namespace, string Name), Class> _classMap = [];
    private readonly HashSet<Entry> _retainedEntries = [];

    public ECallTableGenerator(string manifestPath, string targetOS)
    {
        if (!File.Exists(manifestPath))
            throw new LogAsErrorException($"ECall manifest not found: '{manifestPath}'. Use the manifest from the native runtime being linked.");

        try
        {
            ECallManifest manifest = JsonSerializer.Deserialize(File.ReadAllText(manifestPath), ECallManifestJsonContext.Default.ECallManifest)
                ?? throw new JsonException("ECall manifest value '$' must not be null.");
            if (manifest.Version != 1 || manifest.TargetOS != targetOS)
                throw new LogAsErrorException($"ECall manifest '{manifestPath}' has an incompatible format or target OS; expected version 1 for '{targetOS}'.");

            var symbols = new HashSet<string>(StringComparer.Ordinal);
            foreach (ECallManifestClass manifestClass in manifest.Classes ?? throw new JsonException("ECall manifest value 'classes' must not be null."))
            {
                if (manifestClass is null)
                    throw new JsonException("ECall manifest value 'classes[]' must not be null.");
                string name = ReadString(manifestClass.Name, "name");
                string ns = ReadString(manifestClass.Namespace, "namespace");
                if (_classMap.ContainsKey((ns, name)))
                    throw new LogAsErrorException($"Duplicate ECall class '{ns}.{name}' in '{manifestPath}'.");

                ECallManifestEntry[] manifestEntries = manifestClass.Entries ?? throw new JsonException("ECall manifest value 'entries' must not be null.");
                ImmutableArray<Entry>.Builder entries = ImmutableArray.CreateBuilder<Entry>(manifestEntries.Length);
                foreach (ECallManifestEntry manifestEntry in manifestEntries)
                {
                    if (manifestEntry is null)
                        throw new JsonException("ECall manifest value 'entries[]' must not be null.");
                    var entry = new Entry(ReadString(manifestEntry.Name, "name"),
                        manifestEntry.Signature, ReadString(manifestEntry.Symbol, "symbol"));
                    if (!entry.Symbol.StartsWith(DescriptorSymbolPrefix, StringComparison.Ordinal)
                        || entry.Symbol.Length == DescriptorSymbolPrefix.Length
                        || entry.Symbol.AsSpan(DescriptorSymbolPrefix.Length).IndexOfAnyExceptInRange('0', '9') >= 0
                        || !symbols.Add(entry.Symbol))
                        throw new LogAsErrorException($"Invalid or duplicate ECall descriptor symbol '{entry.Symbol}' in '{manifestPath}'.");
                    entries.Add(entry);
                }
                var cls = new Class(name, ns, entries.MoveToImmutable());
                _classMap.Add((ns, name), cls);
                _classes.Add(cls);
            }
        }
        catch (JsonException ex)
        {
            throw new LogAsErrorException($"Invalid ECall manifest '{manifestPath}': {ex.Message}");
        }

        _classes.Sort(static (left, right) =>
        {
            int order = StringComparer.Ordinal.Compare(left.Name, right.Name);
            return order != 0 ? order : StringComparer.Ordinal.Compare(left.Namespace, right.Namespace);
        });
    }

    public void ScanType(EcmaType type)
    {
        string ns = type.Namespace.ToString();
        string className = type.Name.ToString();
        foreach (MethodDesc method in type.GetMethods())
        {
            if (!method.IsInternalCall)
                continue;

            string methodName = method.Name.ToString();
            string signature = null;
            if (_classMap.TryGetValue((ns, className), out Class cls))
            {
                foreach (Entry entry in cls.Entries)
                {
                    if (entry.Name == methodName && entry.Signature is not null)
                    {
                        signature = GetSignatureKey(method.Signature);
                        break;
                    }
                }
            }
            Retain(ns, className, methodName, signature);
        }
    }

    internal void Retain(string ns, string className, string methodName, string signature)
    {
        Entry match = null;
        if (_classMap.TryGetValue((ns, className), out Class cls))
        {
            foreach (Entry entry in cls.Entries)
            {
                if (entry.Name != methodName || (entry.Signature is not null && entry.Signature != signature))
                    continue;
                if (match is not null)
                    throw new LogAsErrorException($"Ambiguous ECall mapping for '{ns}.{className}::{methodName}' ({signature}).");
                match = entry;
            }
        }

        if (match is not null)
            _retainedEntries.Add(match);
    }

    public void Emit(TextWriter writer)
    {
        writer.WriteLine("// Generated from the native ECall manifest and the bundled System.Private.CoreLib.");
        writer.WriteLine("#include <ecall-table.h>");
        writer.WriteLine();
        int classCount = 0;
        foreach (Class cls in _classes)
        {
            bool retained = false;
            foreach (Entry entry in cls.Entries)
            {
                if (_retainedEntries.Contains(entry))
                {
                    writer.WriteLine($"extern \"C\" const ECFunc {entry.Symbol};");
                    retained = true;
                }
            }
            if (!retained)
                continue;
            writer.WriteLine($"static const ECFunc* const ecall_class_{classCount}[] = {{");
            foreach (Entry entry in cls.Entries)
            {
                if (_retainedEntries.Contains(entry))
                    writer.WriteLine($"    &{entry.Symbol},");
            }
            writer.WriteLine("    nullptr");
            writer.WriteLine("};");
            classCount++;
        }

        writer.WriteLine("extern \"C\" {");
        writer.WriteLine("extern const ECClass c_rgECClasses[] = {");
        int classIndex = 0;
        foreach (Class cls in _classes)
        {
            foreach (Entry entry in cls.Entries)
            {
                if (_retainedEntries.Contains(entry))
                {
                    writer.WriteLine($"    {{ {Quote(cls.Name)}, {Quote(cls.Namespace)}, ecall_class_{classIndex++} }},");
                    break;
                }
            }
        }
        writer.WriteLine("    { nullptr, nullptr, nullptr }");
        writer.WriteLine("};");
        writer.WriteLine($"extern const int c_nECClasses = {classCount};");
        writer.WriteLine("}");
    }

    internal static string GetSignatureKey(MethodSignature signature)
    {
        // ECMA-335 II.23.2.1: preserve the declared signature, not its lowered WASM ABI.
        var result = new StringBuilder(signature.IsStatic ? "static " : "instance ");
        AppendTypeName(result, signature.ReturnType);
        result.Append('(');
        for (int i = 0; i < signature.Length; i++)
        {
            if (i != 0)
                result.Append(',');
            AppendTypeName(result, signature[i]);
        }
        result.Append(')');
        return result.ToString();
    }

    private static void AppendTypeName(StringBuilder result, TypeDesc type)
    {
        if (type.IsSzArray || type.IsPointer || type.IsByRef)
        {
            AppendTypeName(result, ((ParameterizedType)type).ParameterType);
            result.Append(type.IsSzArray ? "[]" : type.IsPointer ? "*" : "&");
            return;
        }

        if (type.GetTypeDefinition() is not MetadataType definition)
            throw new LogAsErrorException($"Unsupported type '{type}' in an ECall signature.");
        if (definition.ContainingType is not null)
        {
            AppendTypeName(result, definition.ContainingType);
            result.Append('+');
        }
        else if (!definition.Namespace.IsEmpty)
        {
            result.Append(definition.Namespace.ToString());
            result.Append('.');
        }
        result.Append(definition.Name.ToString());
        if (type.HasInstantiation)
        {
            result.Append('<');
            for (int i = 0; i < type.Instantiation.Length; i++)
            {
                if (i != 0)
                    result.Append(',');
                AppendTypeName(result, type.Instantiation[i]);
            }
            result.Append('>');
        }
    }

    private static string ReadString(string value, string property)
    {
        if (string.IsNullOrEmpty(value))
            throw new LogAsErrorException($"ECall manifest property '{property}' must be a nonempty string.");
        return value;
    }

    private static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        foreach (byte b in Encoding.UTF8.GetBytes(value))
        {
            if (b is (byte)'"' or (byte)'\\')
                result.Append('\\').Append((char)b);
            else if (b is >= 0x20 and <= 0x7e)
                result.Append((char)b);
            else
                // Three-digit octal escapes preserve UTF-8 bytes without consuming subsequent digits.
                result.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
        }
        result.Append('"');
        return result.ToString();
    }
}
