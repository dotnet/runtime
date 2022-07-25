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
    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+' };

    private TaskLoggingHelper Log { get; set; }

    public PInvokeTableGenerator(TaskLoggingHelper log) => Log = log;

    public IEnumerable<string> GenPInvokeTable(string[] pinvokeModules, string[] assemblies, string outputPath)
    {
        var modules = new Dictionary<string, string>();
        foreach (var module in pinvokeModules)
            modules[module] = module;

        var signatures = new List<string>();

        var pinvokes = new List<PInvoke>();
        var callbacks = new List<PInvokeCallback>();

        var resolver = new PathAssemblyResolver(assemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (var aname in assemblies)
        {
            var a = mlc.LoadFromAssemblyPath(aname);
            foreach (var type in a.GetTypes())
                CollectPInvokes(pinvokes, callbacks, signatures, type);
        }

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

    private void CollectPInvokes(List<PInvoke> pinvokes, List<PInvokeCallback> callbacks, List<string> signatures, Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            try
            {
                CollectPInvokesForMethod(method);
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not get pinvoke, or callbacks for method {method.Name}: {ex}");
                continue;
            }
        }

        void CollectPInvokesForMethod(MethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                var dllimport = method.CustomAttributes.First(attr => attr.AttributeType.Name == "DllImportAttribute");
                var module = (string)dllimport.ConstructorArguments[0].Value!;
                var entrypoint = (string)dllimport.NamedArguments.First(arg => arg.MemberName == "EntryPoint").TypedValue.Value!;
                pinvokes.Add(new PInvoke(entrypoint, module, method));

                string? signature = SignatureMapper.MethodToSignature(method);
                if (signature == null)
                {
                    throw new LogAsErrorException($"Unsupported parameter type in method '{type.FullName}.{method.Name}'");
                }

                Log.LogMessage(MessageImportance.Normal, $"[pinvoke] Adding signature {signature} for method '{type.FullName}.{method.Name}'");
                signatures.Add(signature);
            }

            foreach (CustomAttributeData cattr in CustomAttributeData.GetCustomAttributes(method))
            {
                try
                {
                    if (cattr.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute" ||
                        cattr.AttributeType.Name == "MonoPInvokeCallbackAttribute")
                        callbacks.Add(new PInvokeCallback(method));
                }
                catch
                {
                    // Assembly not found, ignore
                }
            }
        }
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
                Log.LogWarning($"Found a native function ({first.EntryPoint}) with varargs in {first.Module}." +
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
            string symbol = ModuleNameToId(module) + "_imports";
            w.WriteLine("static PinvokeImport " + symbol + " [] = {");

            var assemblies_pinvokes = pinvokes.
                Where(l => l.Module == module && !l.Skip).
                OrderBy(l => l.EntryPoint).
                GroupBy(d => d.EntryPoint).
                Select(l => "{\"" + FixupSymbolName(l.Key) + "\", " + FixupSymbolName(l.Key) + "}, " +
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
            string symbol = ModuleNameToId(module) + "_imports";
            w.Write(symbol + ",");
        }
        w.WriteLine("};");
        w.Write("static char *pinvoke_names[] = { ");
        foreach (var module in modules.Keys)
        {
            w.Write("\"" + module + "\"" + ",");
        }
        w.WriteLine("};");

        static string ModuleNameToId(string name)
        {
            if (name.IndexOfAny(s_charsToReplace) < 0)
                return name;

            string fixedName = name;
            foreach (char c in s_charsToReplace)
                fixedName = fixedName.Replace(c, '_');

            return fixedName;
        }

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

    private static string FixupSymbolName(string name)
    {
        UTF8Encoding utf8 = new();
        byte[] bytes = utf8.GetBytes(name);
        StringBuilder sb = new();

        foreach (byte b in bytes)
        {
            if ((b >= (byte)'0' && b <= (byte)'9') ||
                (b >= (byte)'a' && b <= (byte)'z') ||
                (b >= (byte)'A' && b <= (byte)'Z') ||
                (b == (byte)'_'))
            {
                sb.Append((char)b);
            }
            else if (s_charsToReplace.Contains((char)b))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append($"_{b:X}_");
            }
        }

        return sb.ToString();
    }

    private static string SymbolNameForMethod(MethodInfo method)
    {
        StringBuilder sb = new();
        Type? type = method.DeclaringType;
        sb.Append($"{type!.Module!.Assembly!.GetName()!.Name!}_");
        sb.Append($"{(type!.IsNested ? type!.FullName : type!.Name)}_");
        sb.Append(method.Name);

        return FixupSymbolName(sb.ToString());
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
            sb.Append($"int {FixupSymbolName(pinvoke.EntryPoint)} (int, int, int, int, int);");
            return sb.ToString();
        }

        if (TryIsMethodGetParametersUnsupported(pinvoke.Method, out string? reason))
        {
            Log.LogWarning($"Skipping the following DllImport because '{reason}'. {Environment.NewLine}  {pinvoke.Method}");
            pinvoke.Skip = true;
            return null;
        }

        sb.Append(MapType(method.ReturnType));
        sb.Append($" {FixupSymbolName(pinvoke.EntryPoint)} (");
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

    private static void EmitNativeToInterp(StreamWriter w, List<PInvokeCallback> callbacks)
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

        foreach (var cb in callbacks)
        {
            MethodInfo method = cb.Method;
            bool isVoid = method.ReturnType.FullName == "System.Void";

            if (!isVoid && !IsBlittable(method.ReturnType))
                Error($"The return type '{method.ReturnType.FullName}' of pinvoke callback method '{method}' needs to be blittable.");
            foreach (var p in method.GetParameters())
            {
                if (!IsBlittable(p.ParameterType))
                    Error("Parameter types of pinvoke callback method '" + method + "' needs to be blittable.");
            }
        }

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

            string module_symbol = method.DeclaringType!.Module!.Assembly!.GetName()!.Name!.Replace(".", "_");
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
                sb.Append(MapType(method.GetParameters()[pindex].ParameterType));
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
                sb.Append("&res");
                pindex++;
            }
            int aindex = 0;
            foreach (var p in method.GetParameters())
            {
                if (pindex > 0)
                    sb.Append(", ");
                sb.Append($"&arg{aindex}");
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
            string module_symbol = method.DeclaringType!.Module!.Assembly!.GetName()!.Name!.Replace(".", "_");
            string class_name = method.DeclaringType.Name;
            string method_name = method.Name;
            w.WriteLine($"\"{module_symbol}_{class_name}_{method_name}\",");
        }
        w.WriteLine("};");
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

#pragma warning disable CA1067
internal sealed class PInvoke : IEquatable<PInvoke>
#pragma warning restore CA1067
{
    public PInvoke(string entryPoint, string module, MethodInfo method)
    {
        EntryPoint = entryPoint;
        Module = module;
        Method = method;
    }

    public string EntryPoint;
    public string Module;
    public MethodInfo Method;
    public bool Skip;

    public bool Equals(PInvoke? other)
        => other != null &&
            string.Equals(EntryPoint, other.EntryPoint, StringComparison.Ordinal) &&
            string.Equals(Module, other.Module, StringComparison.Ordinal) &&
            string.Equals(Method.ToString(), other.Method.ToString(), StringComparison.Ordinal);

    public override string ToString() => $"{{ EntryPoint: {EntryPoint}, Module: {Module}, Method: {Method}, Skip: {Skip} }}";
}

internal sealed class PInvokeComparer : IEqualityComparer<PInvoke>
{
    public bool Equals(PInvoke? x, PInvoke? y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;

        return x.Equals(y);
    }

    public int GetHashCode(PInvoke pinvoke)
        => $"{pinvoke.EntryPoint}{pinvoke.Module}{pinvoke.Method}".GetHashCode();
}

internal sealed class PInvokeCallback
{
    public PInvokeCallback(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method;
    public string? EntryName;
}
