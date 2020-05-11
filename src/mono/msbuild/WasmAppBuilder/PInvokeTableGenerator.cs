// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public override bool Execute () {
        GenPInvokeTable (Modules!.Select (item => item.ItemSpec).ToArray (), Assemblies!.Select (item => item.ItemSpec).ToArray ());
        return true;
    }

    void GenPInvokeTable (string[] pinvokeModules, string[] assemblies) {
        var modules = new Dictionary<string, string> ();
        foreach (var module in pinvokeModules)
            modules [module] = module;

        var pinvokes = new List<PInvoke> ();

        var resolver = new PathAssemblyResolver (assemblies);
        var mlc = new MetadataLoadContext (resolver, "System.Private.CoreLib");
        foreach (var aname in assemblies) {
            var a = mlc.LoadFromAssemblyPath (aname);
            foreach (var type in a.GetTypes ())
                CollectPInvokes (pinvokes, type);
        }

        Log.LogMessage (MessageImportance.Normal, $"Generating pinvoke table to '{OutputPath}'.");

        using (var w = File.CreateText (OutputPath!)) {
            EmitPInvokeTable (w, modules, pinvokes);
        }
    }

    void CollectPInvokes (List<PInvoke> pinvokes, Type type) {
        foreach (var method in type.GetMethods (BindingFlags.DeclaredOnly|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)) {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                continue;
            var dllimport = method.CustomAttributes.First (attr => attr.AttributeType.Name == "DllImportAttribute");
            var module = (string)dllimport.ConstructorArguments [0].Value!;
            var entrypoint = (string)dllimport.NamedArguments.First (arg => arg.MemberName == "EntryPoint").TypedValue.Value!;
            pinvokes.Add (new PInvoke (entrypoint, module, method));
        }
    }

    void EmitPInvokeTable (StreamWriter w, Dictionary<string, string> modules, List<PInvoke> pinvokes) {
        w.WriteLine ("// GENERATED FILE, DO NOT MODIFY");
        w.WriteLine ("typedef struct {");
        w.WriteLine ("const char *name;");
        w.WriteLine ("void *func;");
        w.WriteLine ("} PinvokeImport;");
        w.WriteLine ();

        foreach (var pinvoke in pinvokes) {
            if (modules.ContainsKey (pinvoke.Module))
                w.WriteLine (GenPInvokeDecl (pinvoke));
        }

        foreach (var module in modules.Keys) {
            string symbol = module.Replace (".", "_") + "_imports";
            w.WriteLine ("static PinvokeImport " + symbol + " [] = {");
            foreach (var pinvoke in pinvokes) {
                if (pinvoke.Module == module)
                    w.WriteLine ("{\"" + pinvoke.EntryPoint + "\", " + pinvoke.EntryPoint + "},");
            }
            w.WriteLine ("{NULL, NULL}");
            w.WriteLine ("};");
        }
        w.Write ("static void *pinvoke_tables[] = { ");
        foreach (var module in modules.Keys) {
            string symbol = module.Replace (".", "_") + "_imports";
            w.Write (symbol + ",");
        }
        w.WriteLine ("};");
        w.Write ("static char *pinvoke_names[] = { ");
        foreach (var module in modules.Keys) {
            w.Write ("\"" + module + "\"" + ",");
        }
        w.WriteLine ("};");
    }

    string MapType (Type t) {
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

    string GenPInvokeDecl (PInvoke pinvoke) {
        var sb = new StringBuilder ();
        var method = pinvoke.Method;
        sb.Append (MapType (method.ReturnType));
        sb.Append ($" {pinvoke.EntryPoint} (");
        int pindex = 0;
        var pars = method.GetParameters ();
        foreach (var p in pars) {
            if (pindex > 0)
                sb.Append (",");
            sb.Append (MapType (pars [pindex].ParameterType));
            pindex ++;
        }
        sb.Append (");");
        return sb.ToString ();
    }
}

class PInvoke
{
    public PInvoke (string entry_point, string module, MethodInfo method) {
        EntryPoint = entry_point;
        Module = module;
        Method = method;
    }

    public string EntryPoint;
    public string Module;
    public MethodInfo Method;
}
