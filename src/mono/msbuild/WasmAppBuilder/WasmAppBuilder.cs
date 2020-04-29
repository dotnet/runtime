// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

public class WasmAppBuilder : Task
{
    public ITaskItem[] PInvokeModules { get; set; }
    public ITaskItem[] PInvokeAssemblies { get; set; }

	public string PInvokeTablePath { get; set; }

	public override bool Execute () {
		if (PInvokeTablePath == null)
            throw new ArgumentException("Only PInvokeTablePath=true is supported right now.");

		GenPInvokeTable (PInvokeModules.Select (item => item.ItemSpec).ToArray (), PInvokeAssemblies.Select (item => item.ItemSpec).ToArray ());

		return true;
	}

	static string MapType (TypeReference t) {
		if (t.Name == "Void")
			return "void";
		else if (t.Name == "Double")
			return "double";
		else if (t.Name == "Single")
			return "float";
		else if (t.Name == "Int64")
			return "int64_t";
		else if (t.Name == "UInt64")
			return "uint64_t";
		else
			return "int";
	}

	static string GenPInvokeDecl (PInvoke pinvoke) {
		var sb = new StringBuilder ();
		var method = pinvoke.Method;
		sb.Append (MapType (method.ReturnType));
		sb.Append ($" {pinvoke.EntryPoint} (");
		int pindex = 0;
		foreach (var p in method.Parameters) {
			if (pindex > 0)
				sb.Append (",");
			sb.Append (MapType (method.Parameters [pindex].ParameterType));
			pindex ++;
		}
		sb.Append (");");
		return sb.ToString ();
	}

	void GenPInvokeTable (string[] pinvokeModules, string[] assemblies) {
		Log.LogMessage (MessageImportance.Normal, $"Generating pinvoke table to '{PInvokeTablePath}'.");

		var modules = new Dictionary<string, string> ();
		foreach (var module in pinvokeModules)
			modules [module] = module;

		var pinvokes = new List<PInvoke> ();
		foreach (var fname in assemblies) {
			var a = AssemblyDefinition.ReadAssembly (fname);

			foreach (var type in a.MainModule.Types) {
				ProcessTypeForPInvoke (pinvokes, type);
				foreach (var nested in type.NestedTypes)
					ProcessTypeForPInvoke (pinvokes, nested);
			}
		}

		using (var w = File.CreateText (PInvokeTablePath)) {
			EmitPInvokeTable (w, modules, pinvokes);
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

	void ProcessTypeForPInvoke (List<PInvoke> pinvokes, TypeDefinition type) {
		foreach (var method in type.Methods) {
			var info = method.PInvokeInfo;
			if (info == null)
				continue;
			pinvokes.Add (new PInvoke (info.EntryPoint, info.Module.Name, method));
		}
	}
}

class PInvoke
{
	public PInvoke (string entry_point, string module, MethodReference method) {
		EntryPoint = entry_point;
		Module = module;
		Method = method;
	}

	public string EntryPoint;
	public string Module;
	public MethodReference Method;
}
