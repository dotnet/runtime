//
// NOTES:
// - If xunit can't laod a trait discoverer assembly, it silently ignores the error.
// - the RemoteTestExecutor code used by corefx only seems to work if
//   the app is executed from the binary dir using dotnet ./<dllname>.
//   If ran using dotnet run, it seems to invoke itself instead of
//   RemoteExecutorConsoleApp.exe.
//   If ran using dotnet bin/.../<dllname>, it fails with:
//   No executable found matching command "dotnet-<dir>/RemoteExecutorConsoleApp.exe"
//

using System;
using System.Runtime.Loader;
using System.Linq;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;
using Xunit.ConsoleClient;

class MsgSink : IMessageSink {

	public bool OnMessage(IMessageSinkMessage message) {
		Console.WriteLine (((Xunit.Sdk.DiagnosticMessage)message).Message);
		return true;
	}

	public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes) {
		Console.WriteLine ("m2");
		return true;
	}
}

class Program
{
	static string GetTypeName (Type t) {
		if (t.IsNested)
			return t.DeclaringType.FullName + "." + t.Name;
		else
			return t.FullName;
	}

	static void ComputeTraits (Assembly assembly, XunitTestCase tc) {
		// Traits are not set because of some assembly loading problems i.e. Assembly.Load (new AssemblyName ("Microsoft.DotNet.XUnitExtensions")) fails
		// So load them manually
		foreach (ReflectionAttributeInfo attr in ((ReflectionMethodInfo)tc.Method).GetCustomAttributes (typeof (ITraitAttribute))) {
			var discovererAttr = attr.GetCustomAttributes (typeof (TraitDiscovererAttribute)).FirstOrDefault();
			if (discovererAttr != null) {
				var discoverer_args = discovererAttr.GetConstructorArguments().Cast<string>().ToList();
				var disc_assembly = Assembly.LoadFrom (Path.Combine (Path.GetDirectoryName (assembly.Location), discoverer_args [1]) + ".dll");
				var disc_type = disc_assembly.GetType (discoverer_args [0]);
				var disc_obj = (ITraitDiscoverer)Activator.CreateInstance (disc_type);
				foreach (var trait in disc_obj.GetTraits (attr)) {
					if (!tc.Traits.ContainsKey (trait.Key))
						tc.Traits [trait.Key] = new List<string> ();
					tc.Traits [trait.Key].Add (trait.Value);
				}
			}
		}
	}

	class CaseData {
		public object[] Values;
		public MemberDataAttribute MemberData;
		public MemberInfo Member;
	}

	static CodeExpression EncodeValue (object val)
	{
		if (val is int || val is long || val is uint || val is ulong || val is byte || val is sbyte || val is short || val is ushort || val is bool || val is string || val is char || val is float || val is double)
			//return new CodePrimitiveExpression (val);
			return new CodeCastExpression (new CodeTypeReference (val.GetType ()), new CodePrimitiveExpression (val));
		else if (val is Type)
			return new CodeTypeOfExpression ((Type)val);
		else if (val is Enum) {
            TypeCode typeCode = Convert.GetTypeCode (val);
			object o;
			if (typeCode == TypeCode.UInt64)
				o = UInt64.Parse (((Enum)val).ToString ("D"));
			else
				o = Int64.Parse (((Enum)val).ToString ("D"));
			return new CodeCastExpression (new CodeTypeReference (val.GetType ()), new CodePrimitiveExpression (o));
		} else if (val is null) {
			return new CodePrimitiveExpression (null);
		} else if (val is Array) {
			var arr = (IEnumerable)val;
			var arr_expr = new CodeArrayCreateExpression (new CodeTypeReference (val.GetType ()), new CodeExpression [] {});
			foreach (var v in arr)
				arr_expr.Initializers.Add (EncodeValue (v));
			return arr_expr;
		} else {
			throw new Exception ("Unable to emit inline data: " + val.GetType () + " " + val);
		}
	}

	static int Main (string[] args)
	{
		if (args.Length < 3) {
			Console.WriteLine ("Usage: <out-dir> <corefx dir> <test assembly filename> <xunit console options>");
			return 1;
		}

		var testAssemblyName = Path.GetFileNameWithoutExtension (args [2]);
		var testAssemblyFull = Path.GetFullPath (args[2]);
		var outdir_name = Path.Combine (args [0], testAssemblyName);
		var sdkdir = args [1] + "/artifacts/bin/runtime/netcoreapp-OSX-Debug-x64";
		args = args.Skip (2).ToArray ();
		// Response file support
		var extra_args = new List<string> ();
		for (int i = 0; i < args.Length; ++i) {
			var arg = args [i];
			if (arg [0] == '@') {
				foreach (var line in File.ReadAllLines (arg.Substring (1))) {
					if (line.Length == 0 || line [0] == '#')
						continue;
					extra_args.AddRange (line.Split (' '));
				}
				args [i] = "";
			}
		}
		args = args.Where (s => s != String.Empty).Concat (extra_args).ToArray ();

		// Despite a lot of effort, couldn't get dotnet to load these assemblies from the sdk dir, so copy them to our binary dir
//		File.Copy ($"{sdkdir}/Microsoft.DotNet.PlatformAbstractions.dll", AppContext.BaseDirectory, true);
		File.Copy ($"{sdkdir}/CoreFx.Private.TestUtilities.dll", AppContext.BaseDirectory, true);
		File.Copy ($"{sdkdir}/Microsoft.DotNet.XUnitExtensions.dll", AppContext.BaseDirectory, true);

		var cmdline = CommandLine.Parse (args);

		// Ditto
		File.Copy (cmdline.Project.Assemblies.First ().AssemblyFilename, AppContext.BaseDirectory, true);

		var assembly = Assembly.LoadFrom (Path.Combine (AppContext.BaseDirectory, Path.GetFileName (cmdline.Project.Assemblies.First ().AssemblyFilename)));

		var msg_sink = new MsgSink ();
		var xunit2 = new Xunit2Discoverer (AppDomainSupport.Denied, new NullSourceInformationProvider (), new ReflectionAssemblyInfo (assembly), null, null, msg_sink);
		var sink = new TestDiscoverySink ();
		var config = new TestAssemblyConfiguration () { DiagnosticMessages = true, InternalDiagnosticMessages = true, PreEnumerateTheories = false };
		xunit2.Find (false, sink, TestFrameworkOptions.ForDiscovery (config));
		sink.Finished.WaitOne ();

		foreach (XunitTestCase tc in sink.TestCases)
			ComputeTraits (assembly, tc);

		// Compute testcase data
		var tc_data = new Dictionary<XunitTestCase, List<CaseData>> ();
		foreach (XunitTestCase tc in sink.TestCases) {
			var m = ((ReflectionMethodInfo)tc.Method).MethodInfo;
			var t = m.ReflectedType;

			var cases = new List<CaseData> ();

			if (m.GetParameters ().Length > 0) {
				foreach (var cattr in m.GetCustomAttributes (true))
					if (cattr is InlineDataAttribute) {
						var data = ((InlineDataAttribute)cattr).GetData (null).First ();
						if (data == null)
							data = new object [m.GetParameters ().Length];
						if (data.Length != m.GetParameters ().Length)
							throw new Exception ();

						bool unhandled = false;
						foreach (var val in data) {
							if (val is float || val is double)
								unhandled = true;
							if (val is Type) {
								var type = val as Type;
								if (!type.IsVisible)
									unhandled = true;
							}
							if (val is Enum) {
								if (!val.GetType ().IsPublic)
									unhandled = true;
							}
						}
						if (!unhandled)
							cases.Add (new CaseData () { Values = data });
					}
			} else {
				cases.Add (new CaseData ());
			}
			tc_data [tc] = cases;
		}

#if FALSE
		//w.WriteLine ($"\t\ttypeof({typename}).GetMethod (\"{m.Name}\", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null, null);");
		//w.WriteLine ($"\t\tusing (var o = new {typename} ()) {{");
		//if (cmdline.StopOnFail)
		//w.WriteLine ("\t\tif (nfailed > 0) return 1;");
		//w.WriteLine ("\t\tConsole.WriteLine (\"RUN: \" + nrun + \", FAILED: \" + nfailed);");
#endif

		var cu = new CodeCompileUnit ();
		var ns = new CodeNamespace ("");
		cu.Namespaces.Add (ns);
		ns.Imports.Add (new CodeNamespaceImport ("System"));
		ns.Imports.Add (new CodeNamespaceImport ("System.Reflection"));
		var code_class = new CodeTypeDeclaration ("RunTests");
		ns.Types.Add (code_class);

		var code_main = new CodeEntryPointMethod ();
		code_main.ReturnType = new CodeTypeReference ("System.Int32");
		code_class.Members.Add (code_main);

		var statements = code_main.Statements;
		statements.Add (new CodeVariableDeclarationStatement (typeof (int), "nrun", new CodePrimitiveExpression (0)));
		statements.Add (new CodeVariableDeclarationStatement (typeof (int), "nfailed", new CodePrimitiveExpression (0)));

		int nskipped = 0;

		var filters = cmdline.Project.Filters;
		foreach (XunitTestCase tc in sink.TestCases) {
			var m = ((ReflectionMethodInfo)tc.Method).MethodInfo;
			//Console.WriteLine ("" + m.ReflectedType + " " + m + " " + (tc.TestMethodArguments == null));
			var t = m.ReflectedType;
			if (t.IsGenericType) {
				nskipped ++;
				continue;
			}
			if (!filters.Filter (tc)) {
				nskipped ++;
				continue;
			}

			var cases = tc_data [tc];

			int caseindex = 0;
			foreach (var test in cases) {
				string typename = GetTypeName (t);
				string msg;
				if (cases.Count > 1)
					msg = $"{typename}.{m.Name}[{caseindex}] ...";
				else
					msg = $"{typename}.{m.Name} ...";
				caseindex ++;
				statements.Add (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("Console"), "WriteLine", new CodeExpression [] { new CodePrimitiveExpression (msg) }));
				statements.Add (new CodeAssignStatement (new CodeVariableReferenceExpression ("nrun"), new CodeBinaryOperatorExpression (new CodeVariableReferenceExpression ("nrun"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression (1))));
				var try1 = new CodeTryCatchFinallyStatement();
				statements.Add (try1);
				if (!m.IsStatic) {
					// FIXME: Disposable
					try1.TryStatements.Add (new CodeVariableDeclarationStatement ("var", "o", new CodeObjectCreateExpression (t, new CodeExpression[] {})));
				}
				if (!m.IsPublic) {
					// FIXME:
					nskipped ++;
				} else {
					CodeMethodInvokeExpression call;

					if (m.IsStatic)
						call = new CodeMethodInvokeExpression (new CodeTypeReferenceExpression (t), m.Name, new CodeExpression [] {});
					else
						call = new CodeMethodInvokeExpression (new CodeVariableReferenceExpression ("o"), m.Name, new CodeExpression [] {});

					if (test.Values != null) {
						foreach (var val in test.Values)
							call.Parameters.Add (EncodeValue (val));
					}
					try1.TryStatements.Add (call);
				}
				var catch1 = new CodeCatchClause ("ex", new CodeTypeReference ("System.Exception"));
				catch1.Statements.Add (new CodeAssignStatement (new CodeVariableReferenceExpression ("nfailed"), new CodeBinaryOperatorExpression (new CodeVariableReferenceExpression ("nfailed"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression (1))));
				catch1.Statements.Add (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("Console"), "WriteLine", new CodeExpression [] {
							new CodeBinaryOperatorExpression (
															  new CodePrimitiveExpression ("FAILED: "),
															  CodeBinaryOperatorType.Add,
															  new CodeVariableReferenceExpression ("ex"))
						}));
				try1.CatchClauses.Add (catch1);
			}
		}

		statements.Add (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("Console"), "WriteLine", new CodeExpression [] {
					new CodeBinaryOperatorExpression (
													  new CodePrimitiveExpression ("RUN: "),
													  CodeBinaryOperatorType.Add,
													  new CodeVariableReferenceExpression ("nrun"))
				}));
		statements.Add (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("Console"), "WriteLine", new CodeExpression [] {
					new CodeBinaryOperatorExpression (
													  new CodePrimitiveExpression ("FAILURES: "),
													  CodeBinaryOperatorType.Add,
													  new CodeVariableReferenceExpression ("nfailed"))
				}));
		statements.Add (new CodeMethodInvokeExpression (new CodeTypeReferenceExpression ("Console"), "WriteLine", new CodeExpression [] {
					new CodeBinaryOperatorExpression (
													  new CodePrimitiveExpression ("SKIPPED: "),
													  CodeBinaryOperatorType.Add,
													  new CodePrimitiveExpression (nskipped))
				}));
		statements.Add (new CodeMethodReturnStatement (new CodePrimitiveExpression (0)));


		Directory.CreateDirectory (outdir_name);
		var outfile_name = Path.Combine (outdir_name, "runner.cs");
		var provider = new CSharpCodeProvider ();
		using (var w2 = File.CreateText (outfile_name)) {
			provider.GenerateCodeFromCompileUnit (cu, w2, new CodeGeneratorOptions ());
		}

		var csproj_template = File.ReadAllText ("gen-test.csproj.template");
		csproj_template = csproj_template.Replace ("#XUNIT_LOCATION#", sdkdir);
		csproj_template = csproj_template.Replace ("#TEST_ASSEMBLY#", testAssemblyName);
		csproj_template = csproj_template.Replace ("#TEST_ASSEMBLY_LOCATION#", testAssemblyFull);
		
		File.WriteAllText (Path.Combine (outdir_name, testAssemblyName + "-runner.csproj"), csproj_template);

		return 0;
	}
}
