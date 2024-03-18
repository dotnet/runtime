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

internal sealed class PInvokeTableGenerator
{
    private readonly Dictionary<Assembly, bool> _assemblyDisableRuntimeMarshallingAttributeCache = new();

    private LogAdapter Log { get; set; }
    private readonly Func<string, string> _fixupSymbolName;
    private readonly HashSet<string> signatures = new();
    private readonly List<PInvoke> pinvokes = new();
    private readonly List<PInvokeCallback> callbacks = new();
    private readonly PInvokeCollector _pinvokeCollector;

    public PInvokeTableGenerator(Func<string, string> fixupSymbolName, LogAdapter log)
    {
        Log = log;
        _fixupSymbolName = fixupSymbolName;
        _pinvokeCollector = new(log);
    }

    public void ScanAssembly(Assembly asm)
    {
        foreach (Type type in asm.GetTypes())
            _pinvokeCollector.CollectPInvokes(pinvokes, callbacks, signatures, type);
    }

    public IEnumerable<string> Generate(string[] pinvokeModules, string outputPath)
    {
        var modules = new Dictionary<string, string>();
        foreach (var module in pinvokeModules)
            modules[module] = module;

        using TempFileName tmpFileName = new();
        using (var w = File.CreateText(tmpFileName.Path))
        {
            EmitPInvokeTable(w, modules, pinvokes);
            EmitNativeToInterp(w, callbacks);
        }

        if (Utils.CopyIfDifferent(tmpFileName.Path, outputPath, useHash: false))
            Log.LogMessage(MessageImportance.Low, $"Generating pinvoke table to '{outputPath}'.");
        else
            Log.LogMessage(MessageImportance.Low, $"PInvoke table in {outputPath} is unchanged.");

        return signatures;
    }

    private void EmitPInvokeTable(StreamWriter w, Dictionary<string, string> modules, List<PInvoke> pinvokes)
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
                // Special case for __Internal and * modules to indicate static linking wihtout specifying the module
                modules.Add(pinvoke.Module, pinvoke.Module);
                Log.LogMessage(MessageImportance.Low, $"Adding module {pinvoke.Module} for static linking");
            }
        }

        w.WriteLine(
            $"""
            // GENERATED FILE, DO NOT MODIFY");

            """);

        var pinvokesGroupedByEntryPoint = pinvokes
                                            .Where(l => modules.ContainsKey(l.Module))
                                            .OrderBy(l => l.EntryPoint)
                                            .GroupBy(CEntryPoint);
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

        foreach (var module in modules.Keys)
        {
            var assemblies_pinvokes = pinvokes
                .Where(l => l.Module == module && !l.Skip)
                .OrderBy(l => l.EntryPoint)
                .GroupBy(d => d.EntryPoint)
                .Select(l => $"{{\"{EscapeLiteral(l.Key)}\", {CEntryPoint(l.First())}}}, "
                    + "// " + string.Join(", ", l.Select(c => c.Method.DeclaringType!.Module!.Assembly!.GetName()!.Name!).Distinct().OrderBy(n => n)))
                .Append("{NULL, NULL}");

            w.Write(
                $$"""
                static PinvokeImport {{_fixupSymbolName(module)}}_imports [] = {
                    {{string.Join("\n    ", assemblies_pinvokes)}}
                };

                """);
        }

        w.Write(
            $$"""

            static void *pinvoke_tables[] = {
                {{string.Join(", ", modules.Keys.Select(m => $"(void*){_fixupSymbolName(m)}_imports"))}}
            };

            static char *pinvoke_names[] =  {
                {{string.Join(", ", modules.Keys.Select(m => $"\"{EscapeLiteral(m)}\""))}}
            };

            """);

        static bool ShouldTreatAsVariadic(PInvoke[] candidates)
        {
            if (candidates.Length < 2)
                return false;

            PInvoke first = candidates[0];
            if (TryIsMethodGetParametersUnsupported(first.Method, out _))
                return false;

            int firstNumArgs = first.Method.GetParameters().Length;
            return candidates
                        .Skip(1)
                        .Any(c => !TryIsMethodGetParametersUnsupported(c.Method, out _) &&
                                    c.Method.GetParameters().Length != firstNumArgs);
        }
    }

    private string CEntryPoint(PInvoke pinvoke)
    {
        if (pinvoke.WasmLinkage)
        {
            // We mangle the name to avoid collisions with symbols in other modules
            return _fixupSymbolName($"{pinvoke.Module}_{pinvoke.EntryPoint}");
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
        else if (t.IsPointer || IsFunctionPointer(t))
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
    private static bool TryIsMethodGetParametersUnsupported(MethodInfo method, [NotNullWhen(true)] out string? reason)
    {
        try
        {
            method.GetParameters();
        }
        catch (NotSupportedException nse)
        {
            reason = nse.Message;
            return true;
        }
        catch
        {
            // not concerned with other exceptions
        }

        reason = null;
        return false;
    }

    private string? GenPInvokeDecl(PInvoke pinvoke)
    {
        var method = pinvoke.Method;

        if (method.Name == "EnumCalendarInfo")
        {
            // FIXME: System.Reflection.MetadataLoadContext can't decode function pointer types
            // https://github.com/dotnet/runtime/issues/43791
            return $"int {_fixupSymbolName(pinvoke.EntryPoint)} (int, int, int, int, int);";
        }

        if (TryIsMethodGetParametersUnsupported(pinvoke.Method, out string? reason))
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

    private string CEntryPoint(PInvokeCallback export)
    {
        if (export.EntryPoint is not null)
        {
            return _fixupSymbolName(export.EntryPoint);
        }

        var method = export.Method;
        // EntryPoint wasn't specified generate a name for the entry point
        return _fixupSymbolName($"wasm_native_to_interp_{method.DeclaringType!.Module!.Assembly!.GetName()!.Name!}_{method.DeclaringType.Name}_{method.Name}");
    }

    private string DelegateKey(PInvokeCallback export)
    {
        // FIXME: this is a hack, we need to encode this better
        // and allow reflection in the interp case but either way
        // it needs to match the key generated in get_native_to_interp
        var method = export.Method;
        string module_symbol = method.DeclaringType!.Module!.Assembly!.GetName()!.Name!;
        return $"\"{module_symbol}_{method.DeclaringType.Name}_{method.Name}\"".Replace('.', '_');
    }

#pragma warning disable SYSLIB1045 // framework doesn't support GeneratedRegexAttribute
    private static string EscapeLiteral(string s) => Regex.Replace(s, @"(\\|\"")", @"\$1");
#pragma warning restore SYSLIB1045

    private void EmitNativeToInterp(StreamWriter w, List<PInvokeCallback> callbacks)
    {
        // Generate native->interp entry functions
        // These are called by native code, so they need to obtain
        // the interp entry function/arg from a global array
        // They also need to have a signature matching what the
        // native code expects, which is the native signature
        // of the delegate invoke in the [MonoPInvokeCallback]
        // attribute.
        // Only blittable parameter/return types are supposed.
        int cb_index = 0;

        // Arguments to interp entry functions in the runtime
        w.WriteLine($"InterpFtnDesc wasm_native_to_interp_ftndescs[{callbacks.Count}] = {{}};");

        var callbackNames = new HashSet<string>();
        foreach (var cb in callbacks)
        {
            var sb = new StringBuilder();
            var method = cb.Method;
            bool is_void = method.ReturnType.Name == "Void";

            // The signature of the interp entry function
            // This is a gsharedvt_in signature
            sb.Append($"typedef void (*WasmInterpEntrySig_{cb_index}) (");

            if (!is_void)
            {
                sb.Append("int*, ");
            }
            foreach (var p in method.GetParameters())
            {
                sb.Append("int*, ");
            }
            // Extra arg
            sb.Append("int*);\n");

            cb.EntryName = CEntryPoint(cb);
            if (callbackNames.Contains(cb.EntryName))
            {
                Error($"Two callbacks with the same name '{cb.EntryName}' are not supported.");
            }
            callbackNames.Add(cb.EntryName);
            if (cb.EntryPoint is not null)
            {
                sb.Append($"__attribute__((export_name(\"{EscapeLiteral(cb.EntryPoint)}\")))\n");
            }
            sb.Append($"{MapType(method.ReturnType)} {cb.EntryName} (");
            int pindex = 0;
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(", ");
                sb.Append($"{MapType(p.ParameterType)} arg{pindex}");
                pindex++;
            }
            sb.Append(") { \n");
            if (!is_void)
                sb.Append($"  {MapType(method.ReturnType)} res;\n");

            //sb.Append($"  printf(\"{entry_name} called\\n\");\n");
            sb.Append($"  ((WasmInterpEntrySig_{cb_index})wasm_native_to_interp_ftndescs [{cb_index}].func) (");
            if (!is_void)
            {
                sb.Append("(int*)&res, ");
                pindex++;
            }
            int aindex = 0;
            foreach (var p in method.GetParameters())
            {
                sb.Append($"(int*)&arg{aindex}, ");
                aindex++;
            }

            sb.Append($"wasm_native_to_interp_ftndescs [{cb_index}].arg);\n");

            if (!is_void)
                sb.Append("  return res;\n");
            sb.Append("}\n");
            w.WriteLine(sb);
            cb_index++;
        }

        w.Write(
            $$"""

            static void *wasm_native_to_interp_funcs[] = {
                {{string.Join(", ", callbacks.Select(cb => cb.EntryName))}}
            };

            // these strings need to match the keys generated in get_native_to_interp
            static const char *wasm_native_to_interp_map[] = {
                {{string.Join(", ", callbacks.Select(DelegateKey))}}
            };

            """);
    }

    private bool HasAssemblyDisableRuntimeMarshallingAttribute(Assembly assembly)
    {
        if (!_assemblyDisableRuntimeMarshallingAttributeCache.TryGetValue(assembly, out var value))
        {
            _assemblyDisableRuntimeMarshallingAttributeCache[assembly] = value = assembly
                .GetCustomAttributesData()
                .Any(d => d.AttributeType.Name == "DisableRuntimeMarshallingAttribute");
        }

        return value;
    }

    private static readonly Dictionary<Type, bool> _blittableCache = new();

    public static bool IsFunctionPointer(Type type)
    {
        object? bIsFunctionPointer = type.GetType().GetProperty("IsFunctionPointer")?.GetValue(type);
        return (bIsFunctionPointer is bool b) && b;
    }

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

            if (IsFunctionPointer(type))
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
