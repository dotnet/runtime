// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class PInvokeTableGenerator : Task
{
    [Required]
    public ITaskItem[]? Modules { get; set; }
    [Required]
    public ITaskItem[]? Assemblies { get; set; }
    [Required]
    public string? OutputPath { get; set; }

    public override bool Execute()
    {
        GenPInvokeTable(Modules!.Select(item => item.ItemSpec).ToArray(), Assemblies!.Select(item => item.ItemSpec).ToArray());
        return true;
    }

    private void GenPInvokeTable(string[] pinvokeModules, string[] assemblies)
    {
        var modules = new Dictionary<string, string>();
        foreach (var module in pinvokeModules)
            modules [module] = module;

        var pinvokes = new List<PInvoke>();
        var callbacks = new List<PInvokeCallback>();

        var resolver = new PathAssemblyResolver(assemblies);
        var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (var aname in assemblies)
        {
            var a = mlc.LoadFromAssemblyPath(aname);
            foreach (var type in a.GetTypes())
                CollectPInvokes(pinvokes, callbacks, type);
        }

        Log.LogMessage(MessageImportance.Normal, $"Generating pinvoke table to '{OutputPath}'.");

        using (var w = File.CreateText(OutputPath!))
        {
            EmitPInvokeTable(w, modules, pinvokes);
            EmitNativeToInterp(w, callbacks);
        }
    }

    private void CollectPInvokes(List<PInvoke> pinvokes, List<PInvokeCallback> callbacks, Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)) {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                var dllimport = method.CustomAttributes.First(attr => attr.AttributeType.Name == "DllImportAttribute");
                var module = (string)dllimport.ConstructorArguments[0].Value!;
                var entrypoint = (string)dllimport.NamedArguments.First(arg => arg.MemberName == "EntryPoint").TypedValue.Value!;
                pinvokes.Add(new PInvoke(entrypoint, module, method));
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

        var decls = new HashSet<string>();
        foreach (var pinvoke in pinvokes.OrderBy(l => l.EntryPoint))
        {
            if (modules.ContainsKey(pinvoke.Module)) {
                var decl = GenPInvokeDecl(pinvoke);
                if (decls.Contains(decl))
                    continue;

                w.WriteLine(decl);
                decls.Add(decl);
            }
        }

        foreach (var module in modules.Keys)
        {
            string symbol = module.Replace(".", "_") + "_imports";
            w.WriteLine("static PinvokeImport " + symbol + " [] = {");

            var assemblies_pinvokes = pinvokes.
                Where(l => l.Module == module).
                OrderBy(l => l.EntryPoint).
                GroupBy(d => d.EntryPoint).
                Select (l => "{\"" + l.Key + "\", " + l.Key + "}, // " + string.Join (", ", l.Select(c => c.Method.DeclaringType!.Module!.Assembly!.GetName ()!.Name!).Distinct()));

            foreach (var pinvoke in assemblies_pinvokes) {
                w.WriteLine (pinvoke);
            }

            w.WriteLine("{NULL, NULL}");
            w.WriteLine("};");
        }
        w.Write("static void *pinvoke_tables[] = { ");
        foreach (var module in modules.Keys)
        {
            string symbol = module.Replace(".", "_") + "_imports";
            w.Write(symbol + ",");
        }
        w.WriteLine("};");
        w.Write("static char *pinvoke_names[] = { ");
        foreach (var module in modules.Keys)
        {
            w.Write("\"" + module + "\"" + ",");
        }
        w.WriteLine("};");
    }

    private string MapType (Type t)
    {
        string name = t.Name;
        if (name == "Void")
            return "void";
        else if (name == "Double")
            return "double";
        else if (name == "Single")
            return "float";
        else if (name == "Int64")
            return "int64_t";
        else if (name == "UInt64")
            return "uint64_t";
        else
            return "int";
    }

    private string GenPInvokeDecl(PInvoke pinvoke)
    {
        var sb = new StringBuilder();
        var method = pinvoke.Method;
        sb.Append(MapType(method.ReturnType));
        sb.Append($" {pinvoke.EntryPoint} (");
        int pindex = 0;
        var pars = method.GetParameters();
        foreach (var p in pars) {
            if (pindex > 0)
                sb.Append(",");
            sb.Append(MapType(pars[pindex].ParameterType));
            pindex ++;
        }
        sb.Append(");");
        return sb.ToString();
    }

    void EmitNativeToInterp(StreamWriter w, List<PInvokeCallback> callbacks)
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

        foreach (var cb in callbacks) {
            MethodInfo method = cb.Method;
            bool isVoid = method.ReturnType.FullName == "System.Void";

            if (!isVoid && !IsBlittable(method.ReturnType))
                Error($"The return type '{method.ReturnType.FullName}' of pinvoke callback method '{method}' needs to be blittable.");
            foreach (var p in method.GetParameters()) {
                if (!IsBlittable(p.ParameterType))
                    Error("Parameter types of pinvoke callback method '" + method + "' needs to be blittable.");
            }
        }

        var callbackNames = new HashSet<string>();

        foreach (var cb in callbacks) {
            var sb = new StringBuilder();
            var method = cb.Method;

            // The signature of the interp entry function
            // This is a gsharedvt_in signature
            sb.Append("typedef void ");
            sb.Append(" (*WasmInterpEntrySig_" + cb_index + ") (");
            int pindex = 0;
            if (method.ReturnType.Name != "Void") {
                sb.Append("int");
                pindex ++;
            }
            foreach (var p in method.GetParameters()) {
                if (pindex > 0)
                    sb.Append(",");
                sb.Append("int");
                pindex ++;
            }
            if (pindex > 0)
                sb.Append(",");
            // Extra arg
            sb.Append("int");
            sb.Append(");\n");

            bool is_void = method.ReturnType.Name == "Void";

            string module_symbol = method.DeclaringType!.Module!.Assembly!.GetName()!.Name!.Replace(".", "_");
            uint token = (uint)method.MetadataToken;
            string class_name = method.DeclaringType.Name;
            string method_name = method.Name;
            string entry_name = $"wasm_native_to_interp_{module_symbol}_{class_name}_{method_name}";
            if (callbackNames.Contains (entry_name))
            {
                Error($"Two callbacks with the same name '{method_name}' are not supported.");
            }
            callbackNames.Add (entry_name);
            cb.EntryName = entry_name;
            sb.Append(MapType(method.ReturnType));
            sb.Append($" {entry_name} (");
            pindex = 0;
            foreach (var p in method.GetParameters()) {
                if (pindex > 0)
                    sb.Append(",");
                sb.Append(MapType(method.GetParameters()[pindex].ParameterType));
                sb.Append(" arg" + pindex);
                pindex ++;
            }
            sb.Append(") { \n");
            if (!is_void)
                sb.Append(MapType(method.ReturnType) + " res;\n");
            sb.Append("((WasmInterpEntrySig_" + cb_index + ")wasm_native_to_interp_ftndescs [" + cb_index + "].func) (");
            pindex = 0;
            if (!is_void) {
                sb.Append("&res");
                pindex ++;
            }
            int aindex = 0;
            foreach (var p in method.GetParameters()) {
                if (pindex > 0)
                    sb.Append(", ");
                sb.Append("&arg" + aindex);
                pindex ++;
                aindex ++;
            }
            if (pindex > 0)
                sb.Append(", ");
            sb.Append($"wasm_native_to_interp_ftndescs [{cb_index}].arg");
            sb.Append(");\n");
            if (!is_void)
                sb.Append("return res;\n");
            sb.Append("}");
            w.WriteLine(sb);
            cb_index ++;
        }

        // Array of function pointers
        w.Write ("static void *wasm_native_to_interp_funcs[] = { ");
        foreach (var cb in callbacks) {
            w.Write (cb.EntryName + ",");
        }
        w.WriteLine ("};");

        // Lookup table from method->interp entry
        // The key is a string of the form <assembly name>_<method token>
        // FIXME: Use a better encoding
        w.Write ("static const char *wasm_native_to_interp_map[] = { ");
        foreach (var cb in callbacks) {
            var method = cb.Method;
            string module_symbol = method.DeclaringType!.Module!.Assembly!.GetName()!.Name!.Replace(".", "_");
            string class_name = method.DeclaringType.Name;
            string method_name = method.Name;
            w.WriteLine ($"\"{module_symbol}_{class_name}_{method_name}\",");
        }
        w.WriteLine ("};");
    }

    static bool IsBlittable (Type type)
    {
        if (type.IsPrimitive || type.IsByRef || type.IsPointer)
            return true;
        else
            return false;
    }

    static void Error (string msg)
    {
        // FIXME:
        throw new Exception(msg);
    }
}

class PInvoke
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
}

class PInvokeCallback
{
    public PInvokeCallback(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method;
    public string? EntryName;
}
