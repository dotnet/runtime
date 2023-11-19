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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal sealed class PInvokeTableGenerator
{
    private readonly Dictionary<Assembly, bool> _assemblyDisableRuntimeMarshallingAttributeCache = new();

    private TaskLoggingHelper Log { get; set; }
    private readonly Func<string, string> _fixupSymbolName;
    private readonly HashSet<string> signatures = new();
    private readonly List<PInvoke> pinvokes = new();
    private readonly List<PInvokeCallback> callbacks = new();
    private readonly PInvokeCollector _pinvokeCollector;

    public PInvokeTableGenerator(Func<string, string> fixupSymbolName, TaskLoggingHelper log)
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

        string tmpFileName = Path.GetTempFileName();
        try
        {
            using (var w = File.CreateText(tmpFileName))
            {
                EmitPInvokeTable(w, modules, pinvokes);
                EmitNativeToInterp(w, callbacks);
            }

            if (Utils.CopyIfDifferent(tmpFileName, outputPath, useHash: false))
                Log.LogMessage(MessageImportance.Low, $"Generating pinvoke table to '{outputPath}'.");
            else
                Log.LogMessage(MessageImportance.Low, $"PInvoke table in {outputPath} is unchanged.");
        }
        finally
        {
            File.Delete(tmpFileName);
        }

        return signatures;
    }

    private void EmitPInvokeTable(StreamWriter w, Dictionary<string, string> modules, List<PInvoke> pinvokes)
    {


        foreach (var pinvoke in pinvokes) {
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

        var pinvokesGroupedByEntryPoint = pinvokes
                                            .Where(l => modules.ContainsKey(l.Module))
                                            .OrderBy(l => l.EntryPoint)
                                            .GroupBy(TransformEntryPoint);

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
                Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0, $"Found a native function ({first.EntryPoint}) with varargs in {first.Module}." +
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
                if (decl == null || decls.Contains(decl))
                    continue;

                w.WriteLine(decl);
                decls.Add(decl);
            }
        }

        w.WriteLine("// GENERATED FILE, DO NOT MODIFY");
        w.WriteLine();

        foreach (var module in modules.Keys)
        {
            w.WriteLine($"static PinvokeImport {_fixupSymbolName(module)}_imports [] = {{");

            var assemblies_pinvokes = pinvokes.
                Where(l => l.Module == module && !l.Skip).
                OrderBy(l => l.EntryPoint).
                GroupBy(d => d.EntryPoint).
                Select(l => "{\"" + EscapeLiteral(l.Key) + "\", " + TransformEntryPoint(l.First()) + "}, " +
                                "// " + string.Join(", ", l.Select(c => c.Method.DeclaringType!.Module!.Assembly!.GetName()!.Name!).Distinct().OrderBy(n => n)));

            foreach (var pinvoke in assemblies_pinvokes)
            {
                w.WriteLine(pinvoke);
            }

            w.WriteLine("{NULL, NULL}");
            w.WriteLine("};");
        }
        w.WriteLine($"static void *pinvoke_tables[] = {{ {string.Join(", ", modules.Keys.Select(m => $"(void*){_fixupSymbolName(m)}_imports"))} }};");

        w.WriteLine($"static char *pinvoke_names[] = {{ {string.Join(", ", modules.Keys.Select(m => $"\"{EscapeLiteral(m)}\""))} }};");

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

    private string TransformEntryPoint(PInvoke pinvoke)
    {
        if (pinvoke.WasmLinkage)
        {
            // We mangle the name to avoid collisions with symbols in other modules
            return _fixupSymbolName($"{pinvoke.Module}_{pinvoke.EntryPoint}");
        }
        return _fixupSymbolName(pinvoke.EntryPoint);
    }

    private string SymbolNameForMethod(MethodInfo method)
    {
        StringBuilder sb = new();
        Type? type = method.DeclaringType;
        sb.Append($"{type!.Module!.Assembly!.GetName()!.Name!}_");
        sb.Append($"{(type!.IsNested ? type!.FullName : type!.Name)}_");
        sb.Append(method.Name);

        return _fixupSymbolName(sb.ToString());
    }

    private string CallbackSymbolName(PInvokeCallback export)
    {
        var method = export.Method;
        string module_symbol = _fixupSymbolName(method.DeclaringType!.Module!.Assembly!.GetName()!.Name!);
        return $"{module_symbol}_{method.DeclaringType.Name}_{method.Name}";
    }

    private static string MapType(Type t) => t.Name switch
    {
        "Void" => "void",
        nameof(Double) => "double",
        nameof(Single) => "float",
        nameof(Int64) => "int64_t",
        nameof(UInt64) => "uint64_t",
        _ => "int"
    };

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
        var sb = new StringBuilder();
        var method = pinvoke.Method;
        if (method.Name == "EnumCalendarInfo")
        {
            // FIXME: System.Reflection.MetadataLoadContext can't decode function pointer types
            // https://github.com/dotnet/runtime/issues/43791
            sb.Append($"int {_fixupSymbolName(pinvoke.EntryPoint)} (int, int, int, int, int);");
            return sb.ToString();
        }

        if (TryIsMethodGetParametersUnsupported(pinvoke.Method, out string? reason))
        {
            // Don't use method.ToString() or any of it's parameters, or return type
            // because at least one of those are unsupported, and will throw
            Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0,
                    $"Skipping pinvoke '{pinvoke.Method.DeclaringType!.FullName}::{pinvoke.Method.Name}' because '{reason}'.");

            pinvoke.Skip = true;
            return null;
        }

        if (pinvoke.WasmLinkage) {
            sb.Append($"__attribute__((weak,import_module(\"{EscapeLiteral(pinvoke.Module)}\"), import_name(\"{EscapeLiteral(pinvoke.EntryPoint)}\")))\nextern ");
        }

        sb.Append($"{MapType(method.ReturnType)} {TransformEntryPoint(pinvoke)} ({string.Join(", ", method.GetParameters().Select(p => MapType(p.ParameterType)))});");

        return sb.ToString();
    }

    private static string EntryPointSymbolName(PInvokeCallback export, out bool hasEntryPoint)
    {
        var method = export.Method;

        hasEntryPoint = false;
        foreach (var attr in method.CustomAttributes)
        {
            if (attr.AttributeType.Name == "UnmanagedCallersOnlyAttribute")
            {
                foreach(var arg in attr.NamedArguments)
                {
                    if (arg.MemberName == "EntryPoint")
                    {
                        hasEntryPoint = true;
                        return arg.TypedValue.Value!.ToString() ?? throw new Exception("EntryPoint is null");
                    }
                }
            }
        }

        return $"wasm_native_to_interp_{method.DeclaringType!.Module!.Assembly!.GetName()!.Name!}_{method.DeclaringType.Name}_{method.Name}";
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
        w.WriteLine($"InterpFtnDesc wasm_native_to_interp_ftndescs[{callbacks.Count}];");

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

            var entry_point = EntryPointSymbolName(cb, out var hasEntryPoint);
            var entry_name = _fixupSymbolName(entry_point);
            if (callbackNames.Contains(entry_name))
            {
                Error($"Two callbacks with the same name '{entry_point}' are not supported.");
            }
            callbackNames.Add(entry_name);
            cb.EntryName = entry_name;
            if (hasEntryPoint)
            {
                sb.Append($"__attribute__((export_name(\"{EscapeLiteral(entry_point)}\")))\n");
            }
            sb.Append($"{MapType(method.ReturnType)} {entry_name} (");
            int pindex = 0;
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(',');
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

        // Array of function pointers
        w.WriteLine($"static void *wasm_native_to_interp_funcs[] = {{ {string.Join(", ", callbacks.Select(cb => cb.EntryName))} }};");

        // Lookup table from method->interp entry
        // The key is a string of the form <assembly name>_<method token>
        // FIXME: Use a better encoding
        w.WriteLine($"static const char *wasm_native_to_interp_map[] = {{ {string.Join(", ", callbacks.Select(cb => $"\"{CallbackSymbolName(cb)}\""))} }};");
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

    private static bool IsBlittable(Type type)
    {
        if (type.IsPrimitive || type.IsByRef || type.IsPointer || type.IsEnum)
            return true;
        else
            return false;
    }

    private static void Error(string msg) => throw new LogAsErrorException(msg);
}
