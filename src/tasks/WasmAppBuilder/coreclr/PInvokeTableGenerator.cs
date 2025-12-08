// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WasmAppBuilder;
using JoinedString;

internal sealed class PInvokeTableGenerator
{
    private LogAdapter Log { get; set; }
    private readonly Func<string, string> _fixupSymbolName;
    private readonly HashSet<string> signatures = new();
    private readonly List<PInvoke> pinvokes = new();
    private readonly List<PInvokeCallback> callbacks = new();
    private readonly PInvokeCollector _pinvokeCollector;
    private readonly bool _isLibraryMode;

    public PInvokeTableGenerator(Func<string, string> fixupSymbolName, LogAdapter log, bool isLibraryMode = false)
    {
        Log = log;
        _fixupSymbolName = fixupSymbolName;
        _pinvokeCollector = new(log);
        _isLibraryMode = isLibraryMode;
    }

    public void ScanAssembly(Assembly asm)
    {
        foreach (Type type in asm.GetTypes())
            _pinvokeCollector.CollectPInvokes(pinvokes, callbacks, signatures, type);
    }

    public IEnumerable<string> Generate(string[] pinvokeModules, string outputPath)
    {
        var modules = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var module in pinvokeModules)
            modules[module] = module;

        using TempFileName tmpFileName = new();
        using (var w = new JoinedStringStreamWriter(tmpFileName.Path, false))
        {
            // WASM-TODO: the generator is WIP, so we disable pinvoke table generation
            // EmitPInvokeTable(w, modules, pinvokes);
            EmitNativeToInterp(w, callbacks);
        }

        if (Utils.CopyIfDifferent(tmpFileName.Path, outputPath, useHash: false))
            Log.LogMessage(MessageImportance.Low, $"Generating pinvoke table to '{outputPath}'.");
        else
            Log.LogMessage(MessageImportance.Low, $"PInvoke table in {outputPath} is unchanged.");

        return signatures;
    }

    private void EmitPInvokeTable(StreamWriter w, SortedDictionary<string, string> modules, List<PInvoke> pinvokes)
    {
        foreach (var pinvoke in pinvokes)
        {
            if (modules.ContainsKey(pinvoke.Module))
                continue;
            // Handle special modules, and add them to the list of modules
            // otherwise, skip them and throw an exception at runtime if they
            // are called.
            if (pinvoke.WasmLinkage)
            {
                // WasmLinkage means we needs to import the module
                modules.Add(pinvoke.Module, pinvoke.Module);
                Log.LogMessage(MessageImportance.Low, $"Adding module {pinvoke.Module} for WasmImportLinkage");
            }
            else if (pinvoke.Module == "*" || pinvoke.Module == "__Internal")
            {
                // Special case for __Internal and * modules to indicate static linking without specifying the module
                modules.Add(pinvoke.Module, pinvoke.Module);
                Log.LogMessage(MessageImportance.Low, $"Adding module {pinvoke.Module} for static linking");
            }
            else
            {
                Log.Warning("WASM0066", $"PInvoke module '{pinvoke.Module}' for method '{pinvoke.Method.DeclaringType}::{pinvoke.Method.Name}' is not in the list of allowed modules. It is also not special treated module.");
            }
        }

        w.WriteLine(
            $"""
            // GENERATED FILE, DO NOT MODIFY (PInvokeTableGenerator.cs)
            """);

        var pinvokesGroupedByEntryPoint = pinvokes
                                            .Where(l => modules.ContainsKey(l.Module))
                                            .OrderBy(l => l.EntryPoint, StringComparer.Ordinal)
                                            .GroupBy(CEntryPoint, StringComparer.Ordinal);
        var comparer = new PInvokeComparer();
        foreach (IGrouping<string, PInvoke> group in pinvokesGroupedByEntryPoint)
        {
            var candidates = group.Distinct(comparer).ToArray();
            PInvoke first = candidates[0];
            if (ShouldTreatAsVariadic(candidates))
            {
                string imports = string.Join(Environment.NewLine,
                                            candidates.Select(
                                                p => $"    {p.Method} (in [{p.Method.DeclaringType?.Assembly.GetName().Name}] {p.Method.DeclaringType})"));
                Log.Warning("WASM0001", $"Found a native function ({first.EntryPoint}) with varargs in {first.Module}." +
                                 " Calling such functions is not supported, and will fail at runtime." +
                                $" Managed DllImports: {Environment.NewLine}{imports}");

                foreach (var c in candidates)
                    c.Skip = true;

                continue;
            }

            var decls = new HashSet<string>();
            foreach (var candidate in candidates)
            {
                var decl = GenPInvokeDecl(candidate);
                if (decl is null || decls.Contains(decl))
                    continue;

                w.WriteLine(decl);
                decls.Add(decl);
            }
        }

        var moduleImports = new Dictionary<string, List<string>>();
        foreach (var module in modules.Keys)
        {
            // the order here is not important, because we use hash tables, we want it to be stable though
            var imports = pinvokes
                .Where(l => l.Module == module && !l.Skip)
                .OrderBy(l => l.EntryPoint, StringComparer.Ordinal)
                .GroupBy(d => d.EntryPoint, StringComparer.Ordinal)
                .Select(l => $"{{\"{EscapeLiteral(l.Key)}\", {CEntryPoint(l.First())}}}, // {ListRefs(l)}{w.NewLine}    ")
                .ToList();

            moduleImports[module] = imports;
            w.Write(
                $$"""

                static PinvokeImport {{_fixupSymbolName(module)}}_imports [] = {
                    {{string.Join("", imports)}}{NULL, NULL}
                };

                """);
        }

        w.Write(
            $$"""

            static PinvokeTable pinvoke_tables[] = {
                {{modules.Keys.Join($",{w.NewLine}    ", m => $"{{\"{EscapeLiteral(m)}\", {_fixupSymbolName(m)}_imports, {moduleImports[m].Count}}}")}}
            };

            """);

        static bool ShouldTreatAsVariadic(PInvoke[] candidates)
        {
            if (candidates.Length < 2)
                return false;

            PInvoke first = candidates[0];
            if (!TryIsMethodGetParametersSupported(first.Method, out _))
                return false;

            int firstNumArgs = first.Method.GetParameters().Length;
            return candidates
                        .Skip(1)
                        // detect possible vararg entrypoint usage
                        // where the same entrypoint is used with different
                        // number of arguments
                        .Any(c => TryIsMethodGetParametersSupported(c.Method, out _) &&
                                    c.Method.GetParameters().Length != firstNumArgs);
        }

        static string ListRefs(IGrouping<string, PInvoke> l) =>
            string.Join(", ", l.Select(c => c.Method.DeclaringType!.Module!.Assembly!.GetName()!.Name!).Distinct().OrderBy(n => n));
    }

    private string CEntryPoint(PInvoke pinvoke)
    {
        if (pinvoke.WasmLinkage)
        {
            // We mangle the name to avoid collisions with symbols in other modules
            string namespaceName = pinvoke.Method.DeclaringType?.Namespace ?? string.Empty;
            return _fixupSymbolName($"{namespaceName}#{pinvoke.Module}#{pinvoke.EntryPoint}");
        }
        return _fixupSymbolName(pinvoke.EntryPoint);
    }

    private static string MapType(Type t) => t.Name switch
    {
        "Void" => "void",
        nameof(Double) => "double",
        nameof(Single) => "float",
        nameof(Int64) => "int64_t",
        nameof(UInt64) => "uint64_t",
        nameof(Int32) => "int32_t",
        nameof(UInt32) => "uint32_t",
        nameof(Int16) => "int32_t",
        nameof(UInt16) => "uint32_t",
        nameof(Char) => "int32_t",
        nameof(Boolean) => "int32_t",
        nameof(SByte) => "int32_t",
        nameof(Byte) => "uint32_t",
        nameof(IntPtr) => "void *",
        nameof(UIntPtr) => "void *",
        _ => PickCTypeNameForUnknownType(t)
    };

    private static string PickCTypeNameForUnknownType(Type t)
    {
        // Pass objects by-reference (their address by-value)
        if (!t.IsValueType)
            return "void *";
        // Pass pointers and function pointers by-value
        else if (t.IsPointer || t.IsFunctionPointer)
            return "void *";
        else if (t.IsPrimitive)
            throw new NotImplementedException("No native type mapping for type " + t);

        // https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md#function-signatures
        // Any struct or union that recursively (including through nested structs, unions, and arrays)
        //  contains just a single scalar value and is not specified to have greater than natural alignment.
        // FIXME: Handle the scenario where there are fields of struct types that contain no members
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fields.Length == 1)
            return MapType(fields[0].FieldType);
        else
            return "void *";
    }

    // FIXME: System.Reflection.MetadataLoadContext can't decode function pointer types
    // https://github.com/dotnet/runtime/issues/43791
    private static bool TryIsMethodGetParametersSupported(MethodInfo method, [NotNullWhen(false)] out string? reason)
    {
        try
        {
            method.GetParameters();
        }
        catch (NotSupportedException nse)
        {
            reason = nse.Message;
            return false;
        }
        catch
        {
            // not concerned with other exceptions
        }

        reason = null;
        return true;
    }

    private string? GenPInvokeDecl(PInvoke pinvoke)
    {
        var method = pinvoke.Method;

        if (!TryIsMethodGetParametersSupported(pinvoke.Method, out string? reason))
        {
            // Don't use method.ToString() or any of it's parameters, or return type
            // because at least one of those are unsupported, and will throw
            Log.Warning("WASM0001", $"Skipping pinvoke '{pinvoke.Method.DeclaringType!.FullName}::{pinvoke.Method.Name}' because '{reason}'.");

            pinvoke.Skip = true;
            return null;
        }

        var realReturnType = method.ReturnType;
        var realParameterTypes = method.GetParameters().Select(p => MapType(p.ParameterType)).ToList();

        SignatureMapper.TypeToChar(realReturnType, Log, out bool resultIsByRef);
        if (resultIsByRef) {
            realReturnType = typeof(void);
            realParameterTypes.Insert(0, "void *");
        }

        return
            $$"""
            {{(pinvoke.WasmLinkage ? $"__attribute__((import_module(\"{EscapeLiteral(pinvoke.Module)}\"),import_name(\"{EscapeLiteral(pinvoke.EntryPoint)}\")))" : "")}}
            {{(pinvoke.WasmLinkage ? "extern " : "")}}{{MapType(realReturnType)}} {{CEntryPoint(pinvoke)}} ({{string.Join(", ", realParameterTypes)}});
            """;
    }

    private static string EscapeLiteral(string? input)
    {
        if (input == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            sb.Append(c switch
            {
                '\\' => "\\\\",
                '\"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                // take special care with surrogate pairs to avoid
                // potential decoding issues in generated C literals
                _ when char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1])
                    => $"\\U{char.ConvertToUtf32(c, input[++i]):X8}",
                _ when char.IsControl(c) || c > 127
                    => $"\\u{(int)c:X4}",
                _ => c.ToString()
            });
        }

        return sb.ToString();
    }

    // this is eqivalent to `ULONG HashString(LPCWSTR szStr)` in CoreCLR runtime, src/coreclr/inc/utilcode.h
    private static uint HashString(string str)
    {
        uint hash = 5381;
        foreach (char c in str)
        {
            hash = ((hash << 5) + hash) ^ (uint)c;
        }

        return hash;
    }

    private void EmitNativeToInterp(StreamWriter w, List<PInvokeCallback> callbacks)
    {
        // Generate native->interp entry functions
        // These are called by native code, so they need to obtain
        // the interp entry function/arg from a global array
        // They also need to have a signature matching what the
        // native code expects, which is the native signature
        // of the delegate invoke in the [MonoPInvokeCallback]
        // or [UnmanagedCallersOnly] attribute.
        // Only blittable parameter/return types are supposed.
        w.Write(
            $$"""
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.
            //

            //
            // GENERATED FILE, DON'T EDIT
            // Generated by coreclr InterpToNativeGenerator
            //

            #include <callhelpers.hpp>

            // WASM-TODO: The method lookup would ideally be fully qualified assembly and then methodDef token.
            // The current approach has limitations with overloaded methods.
            extern "C" void LookupMethodByName(const char* fullQualifiedTypeName, const char* methodName, MethodDesc** ppMD);
            extern "C" void ExecuteInterpretedMethodFromUnmanaged(MethodDesc* pMD, int8_t* args, size_t argSize, int8_t* ret, PCODE callerIp);

            """);

        var callbackNames = new HashSet<string>();
        var keys = new HashSet<string>();
        int cb_index = 0;
        callbacks = callbacks.OrderBy(c => c, new PInvokeCallbackComparer()).ToList();
        foreach (var cb in callbacks)
        {
            cb.EntrySymbol = FixedSymbolName(cb, Log);

            if (callbackNames.Contains(cb.EntrySymbol))
            {
                Error($"Two callbacks with the same symbol '{cb.EntrySymbol}' are not supported.");
            }
            callbackNames.Add(cb.EntrySymbol);
            if (keys.Contains(cb.Key))
            {
                Error($"Two callbacks with the same Name and number of arguments '{cb.Key}' are not supported.");
            }
            keys.Add(cb.Key);

            // The signature of the interp entry function
            // This is a gsharedvt_in signature
            var entryArgs = new List<string>();
            if (!cb.IsVoid)
            {
                entryArgs.Add("(int*)&result");
            }
            entryArgs.AddRange(cb.Parameters.Select((_, i) => $"(int*)&arg{i}"));
            entryArgs.Add($"(int*)wasm_native_to_interp_ftndescs [{cb_index}].arg");

            var argsArgs = cb.Parameters.Length > 0 ? "(int8_t*)args, sizeof(args)" : "nullptr, 0";
            var argsDeclaration = cb.Parameters.Length > 0
                ? $"\n    int64_t args[{cb.Parameters.Length}] = {{ {cb.Parameters.Join(", ", (info, i) => $"(int64_t)arg{i}")} }};\n"
                : string.Empty;
            var parametersDeclaration = cb.Parameters.Join(", ", (info, i) => $"{MapType(info.ParameterType)} arg{i}");
            var exportFunction = cb.IsExport ?
                $$"""


                extern "C" void {{cb.EntryPoint}}({{parametersDeclaration}})
                {
                    Call_{{cb.EntrySymbol}}({{cb.Parameters.Join(", ", (info, i) => $"arg{i}")}});
                }
                """ : string.Empty;
            w.Write(
                $$"""

                static MethodDesc* MD_{{cb.EntrySymbol}} = nullptr;
                static {{
                MapType(cb.ReturnType)}} Call_{{cb.EntrySymbol}}({{parametersDeclaration}})
                {{{argsDeclaration}}
                    // Lazy lookup of MethodDesc for the function export scenario.
                    if (!MD_{{cb.EntrySymbol}})
                    {
                        LookupMethodByName("{{cb.TypeFullName}}, {{cb.AssemblyName}}", "{{cb.MethodName}}", &MD_{{cb.EntrySymbol}});
                    }{{
                    (!cb.IsVoid ? $"{w.NewLine}{w.NewLine}    {MapType(cb.ReturnType)} result;" : "")}}
                    ExecuteInterpretedMethodFromUnmanaged(MD_{{cb.EntrySymbol}}, {{argsArgs}}, {{(cb.IsVoid ? "nullptr" : "(int8_t*)&result")}}, (PCODE)&Call_{{cb.EntrySymbol}});{{
                    (!cb.IsVoid ? $"{w.NewLine}    return result;" : "")}}
                }{{exportFunction}}

                """);
            cb_index++;
        }

        w.Write(
            $$"""

            extern const ReverseThunkMapEntry g_ReverseThunks[] =
            {
            {{callbacks.Join($",{w.NewLine}", cb => ThunkMapEntryLine(cb, Log))}}
            };

            const size_t g_ReverseThunksCount = sizeof(g_ReverseThunks) / sizeof(g_ReverseThunks[0]);

            """);
    }

    private string FixedSymbolName(PInvokeCallback cb, LogAdapter Log)
    {
        var paramTypes = cb.Parameters.Length > 0 ? cb.Parameters.Join("_", (info, i) => SignatureMapper.TypeToNameType(info.ParameterType, Log)).ToString() : "Void";
        var sig = $"{paramTypes}_Ret{SignatureMapper.TypeToNameType(cb.ReturnType, Log)}";

        return _fixupSymbolName($"{cb.EntryName}_{sig}");
    }


    private string ThunkMapEntryLine(PInvokeCallback cb, LogAdapter Log)
    {
        var fsName = FixedSymbolName(cb, Log);

        return $"    {{ {cb.Token ^ HashString(cb.AssemblyFQName)}, {HashString(cb.Key)}, {{ &MD_{fsName}, (void*)&Call_{cb.EntrySymbol} }} }} /* alternate key source: {cb.Key} */";
    }

    private static readonly Dictionary<Type, bool> _blittableCache = new();

    public static bool IsBlittable(Type type, LogAdapter log)
    {
        // We maintain a cache of results in order to only produce log messages the first time
        //  we analyze a given type. Otherwise, each (successful) use of a user-defined type
        //  in a callback or pinvoke would generate duplicate messages.
        lock (_blittableCache)
            if (_blittableCache.TryGetValue(type, out bool blittable))
                return blittable;

        bool result = IsBlittableUncached(type, log);
        lock (_blittableCache)
            _blittableCache[type] = result;
        return result;

        static bool IsBlittableUncached(Type type, LogAdapter log)
        {
            if (type.IsPrimitive || type.IsByRef || type.IsPointer || type.IsEnum)
                return true;

            if (type.IsFunctionPointer)
                return true;

            // HACK: SkiaSharp has pinvokes that rely on this
            if (HasAttribute(type, "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"))
                return true;

            if (type.Name == "__NonBlittableTypeForAutomatedTests__")
                return false;

            if (!type.IsValueType)
            {
                log.InfoHigh("WASM0060", "Type {0} is not blittable: Not a ValueType", type);
                return false;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (!type.IsLayoutSequential && (fields.Length > 1))
            {
                log.InfoHigh("WASM0061", "Type {0} is not blittable: LayoutKind is not Sequential", type);
                return false;
            }

            foreach (var ft in fields)
            {
                if (!IsBlittable(ft.FieldType, log))
                {
                    log.InfoHigh("WASM0062", "Type {0} is not blittable: Field {1} is not blittable", type, ft.Name);
                    return false;
                }
                // HACK: Skip literals since they're complicated
                // Ideally we would block initonly fields too since the callee could mutate them, but
                //  we rely on being able to pass types like System.Guid which are readonly
                if (ft.IsLiteral)
                {
                    log.InfoHigh("WASM0063", "Type {0} is not blittable: Field {1} is literal", type, ft.Name);
                    return false;
                }
            }

            return true;
        }
    }

    public static bool HasAttribute(MemberInfo element, params string[] attributeNames)
    {
        foreach (CustomAttributeData cattr in CustomAttributeData.GetCustomAttributes(element))
        {
            try
            {
                for (int i = 0; i < attributeNames.Length; ++i)
                {
                    if (cattr.AttributeType.FullName == attributeNames[i] ||
                        cattr.AttributeType.Name == attributeNames[i])
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Assembly not found, ignore
            }
        }
        return false;
    }

    private static void Error(string msg) => throw new LogAsErrorException(msg);
}
