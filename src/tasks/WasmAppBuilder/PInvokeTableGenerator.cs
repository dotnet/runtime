// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal sealed class PInvokeTableGenerator
{
    private readonly Dictionary<Assembly, bool> _assemblyDisableRuntimeMarshallingAttributeCache = new();

    private TaskLoggingHelper Log { get; set; }
    private readonly Func<string, string> _fixupSymbolName;

    public PInvokeTableGenerator(Func<string, string> fixupSymbolName, TaskLoggingHelper log)
    {
        Log = log;
        _fixupSymbolName = fixupSymbolName;
    }

    public IEnumerable<string> Generate(string[] pinvokeModules, IEnumerable<string> assemblies, string outputPath)
    {
        var modules = new Dictionary<string, string>();
        foreach (var module in pinvokeModules)
            modules[module] = module;

        var signatures = new List<string>();

        var pinvokes = new List<PInvoke>();
        var callbacks = new List<PInvokeCallback>();

        PInvokeCollector pinvokeCollector = new(Log);

        var resolver = new PathAssemblyResolver(assemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");

        foreach (var asmPath in assemblies)
        {
            if (!File.Exists(asmPath))
                throw new LogAsErrorException($"Cannot find assembly {asmPath}");

            Log.LogMessage(MessageImportance.Low, $"Loading {asmPath} to scan for pinvokes");
            var a = mlc.LoadFromAssemblyPath(asmPath);
            foreach (var type in a.GetTypes())
                pinvokeCollector.CollectPInvokes(pinvokes, callbacks, signatures, type);
        }

        string tmpFileName = Path.GetTempFileName();
        try
        {
            using (var w = File.CreateText(tmpFileName))
            {
                EmitPInvokeTable(w, modules, pinvokes);
                EmitNativeToInterp(w, ref callbacks);
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

    private static bool HasAttribute(MemberInfo element, params string[] attributeNames)
    {
        foreach (CustomAttributeData cattr in CustomAttributeData.GetCustomAttributes(element))
        {
            try
            {
                for (int i = 0; i < attributeNames.Length; ++i)
                {
                    if (cattr.AttributeType.FullName == attributeNames [i] ||
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

    private void EmitPInvokeTable(StreamWriter w, Dictionary<string, string> modules, List<PInvoke> pinvokes)
    {
        w.WriteLine("// GENERATED FILE, DO NOT MODIFY");
        w.WriteLine();

        var pinvokesGroupedByEntryPoint = pinvokes
                                            .Where(l => modules.ContainsKey(l.Module))
                                            .OrderBy(l => l.EntryPoint)
                                            .GroupBy(l => l.EntryPoint);

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

        foreach (var module in modules.Keys)
        {
            string symbol = _fixupSymbolName(module) + "_imports";
            w.WriteLine("static PinvokeImport " + symbol + " [] = {");

            var assemblies_pinvokes = pinvokes.
                Where(l => l.Module == module && !l.Skip).
                OrderBy(l => l.EntryPoint).
                GroupBy(d => d.EntryPoint).
                Select(l => "{\"" + _fixupSymbolName(l.Key) + "\", " + _fixupSymbolName(l.Key) + "}, " +
                                "// " + string.Join(", ", l.Select(c => c.Method.DeclaringType!.Module!.Assembly!.GetName()!.Name!).Distinct().OrderBy(n => n)));

            foreach (var pinvoke in assemblies_pinvokes)
            {
                w.WriteLine(pinvoke);
            }

            w.WriteLine("{NULL, NULL}");
            w.WriteLine("};");
        }
        w.Write("static void *pinvoke_tables[] = { ");
        foreach (var module in modules.Keys)
        {
            string symbol = _fixupSymbolName(module) + "_imports";
            w.Write(symbol + ",");
        }
        w.WriteLine("};");
        w.Write("static char *pinvoke_names[] = { ");
        foreach (var module in modules.Keys)
        {
            w.Write("\"" + module + "\"" + ",");
        }
        w.WriteLine("};");

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

    private string SymbolNameForMethod(MethodInfo method)
    {
        StringBuilder sb = new();
        Type? type = method.DeclaringType;
        sb.Append($"{type!.Module!.Assembly!.GetName()!.Name!}_");
        sb.Append($"{(type!.IsNested ? type!.FullName : type!.Name)}_");
        sb.Append(method.Name);

        return _fixupSymbolName(sb.ToString());
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

        sb.Append(MapType(method.ReturnType));
        sb.Append($" {_fixupSymbolName(pinvoke.EntryPoint)} (");
        int pindex = 0;
        var pars = method.GetParameters();
        foreach (var p in pars)
        {
            if (pindex > 0)
                sb.Append(',');
            sb.Append(MapType(pars[pindex].ParameterType));
            pindex++;
        }
        sb.Append(");");
        return sb.ToString();
    }

    private void EmitNativeToInterp(StreamWriter w, ref List<PInvokeCallback> callbacks)
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
        w.WriteLine("InterpFtnDesc wasm_native_to_interp_ftndescs[" + callbacks.Count + "];");

        var callbackNames = new HashSet<string>();
        foreach (var cb in callbacks)
        {
            var sb = new StringBuilder();
            var method = cb.Method;

            // The signature of the interp entry function
            // This is a gsharedvt_in signature
            sb.Append("typedef void ");
            sb.Append($" (*WasmInterpEntrySig_{cb_index}) (");
            int pindex = 0;
            if (method.ReturnType.Name != "Void")
            {
                sb.Append("int*");
                pindex++;
            }
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(',');
                sb.Append("int*");
                pindex++;
            }
            if (pindex > 0)
                sb.Append(',');
            // Extra arg
            sb.Append("int*");
            sb.Append(");\n");

            bool is_void = method.ReturnType.Name == "Void";

            string module_symbol = _fixupSymbolName(method.DeclaringType!.Module!.Assembly!.GetName()!.Name!);
            uint token = (uint)method.MetadataToken;
            string class_name = method.DeclaringType.Name;
            string method_name = method.Name;
            string entry_name = $"wasm_native_to_interp_{module_symbol}_{class_name}_{method_name}";
            if (callbackNames.Contains(entry_name))
            {
                Error($"Two callbacks with the same name '{method_name}' are not supported.");
            }
            callbackNames.Add(entry_name);
            cb.EntryName = entry_name;
            sb.Append(MapType(method.ReturnType));
            sb.Append($" {entry_name} (");
            pindex = 0;
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(',');
                sb.Append(MapType(p.ParameterType));
                sb.Append($" arg{pindex}");
                pindex++;
            }
            sb.Append(") { \n");
            if (!is_void)
                sb.Append(MapType(method.ReturnType) + " res;\n");
            sb.Append($"((WasmInterpEntrySig_{cb_index})wasm_native_to_interp_ftndescs [{cb_index}].func) (");
            pindex = 0;
            if (!is_void)
            {
                sb.Append("(int*)&res");
                pindex++;
            }
            int aindex = 0;
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(", ");
                sb.Append($"(int*)&arg{aindex}");
                pindex++;
                aindex++;
            }
            if (pindex > 0)
                sb.Append(", ");
            sb.Append($"wasm_native_to_interp_ftndescs [{cb_index}].arg");
            sb.Append(");\n");
            if (!is_void)
                sb.Append("return res;\n");
            sb.Append('}');
            w.WriteLine(sb);
            cb_index++;
        }

        // Array of function pointers
        w.Write("static void *wasm_native_to_interp_funcs[] = { ");
        foreach (var cb in callbacks)
        {
            w.Write(cb.EntryName + ",");
        }
        w.WriteLine("};");

        // Lookup table from method->interp entry
        // The key is a string of the form <assembly name>_<method token>
        // FIXME: Use a better encoding
        w.Write("static const char *wasm_native_to_interp_map[] = { ");
        foreach (var cb in callbacks)
        {
            var method = cb.Method;
            string module_symbol = _fixupSymbolName(method.DeclaringType!.Module!.Assembly!.GetName()!.Name!);
            string class_name = method.DeclaringType.Name;
            string method_name = method.Name;
            w.WriteLine($"\"{module_symbol}_{class_name}_{method_name}\",");
        }
        w.WriteLine("};");
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
