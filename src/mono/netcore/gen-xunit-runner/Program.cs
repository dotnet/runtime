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
using System.Collections.Generic;
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

	static int Main (string[] args)
	{
		if (args.Length < 3) {
			Console.WriteLine ("Usage: <outfile> <corefx dir> <test assembly filename> <xunit console options>");
			return 1;
		}

		var outfile_name = args [0];
		var sdkdir = args [1] + "/artifacts/bin/runtime/netcoreapp-OSX-Debug-x64";
		args = args.Skip (2).ToArray ();

		// Despite a lot of effort, couldn't get dotnet to load these assemblies from the sdk dir, so copy them to our binary dir
		File.Copy ($"{sdkdir}/Microsoft.DotNet.PlatformAbstractions.dll", AppContext.BaseDirectory, true);
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

		var w = File.CreateText (outfile_name);
		w.WriteLine ("using System;");
		w.WriteLine ("using System.Reflection;");
		w.WriteLine ("public class RunTests {");
		w.WriteLine ("\tpublic static int Main () {");
		w.WriteLine ("\t\tint nrun = 0;");
		w.WriteLine ("\t\tint nfailed = 0;");

		var filters = cmdline.Project.Filters;
		foreach (XunitTestCase tc in sink.TestCases) {
			var m = ((ReflectionMethodInfo)tc.Method).MethodInfo;
			//Console.WriteLine ("" + m.ReflectedType + " " + m + " " + (tc.TestMethodArguments == null));
			if (tc.TestMethodArguments != null || m.GetParameters ().Length > 0)
				continue;
			var t = m.ReflectedType;
			if (t.IsGenericType)
				continue;

			/*
			foreach (var trait in tc.Traits) {
				foreach (var s in trait.Value)
					Console.WriteLine (m.Name + " " + trait.Key + " " + s);
			}
			*/

			if (!filters.Filter (tc)) {
				//Console.WriteLine ("FILTERED: " + m);
				continue;
			}

			string typename = GetTypeName (t);
			w.WriteLine ($"Console.WriteLine (\"{typename}:{m.Name}...\");");
			w.WriteLine ("\t\tnrun ++;");
			w.WriteLine ("\t\ttry {");
			if (m.IsStatic) {
				if (!m.IsPublic)
					w.WriteLine ($"\t\ttypeof({typename}).GetMethod (\"{m.Name}\", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null, null);");
				else
					w.WriteLine ($"\t\t{typename}.{m.Name} ();");
			} else {
				if (typeof (IDisposable).IsAssignableFrom (t)) {
					w.WriteLine ($"\t\tusing (var o = new {typename} ()) {{");
				} else {
					w.WriteLine ("\t\t{");
					w.WriteLine ($"\t\t\tvar o = new {typename} ();");
				}
				w.WriteLine ($"\t\t\to.{m.Name} ();");
				w.WriteLine ("\t\t}");
			}
			w.WriteLine ("\t\t} catch (Exception ex) { nfailed ++; Console.WriteLine (\"FAILED: \" + ex); }");
			if (cmdline.StopOnFail)
				w.WriteLine ("\t\tif (nfailed > 0) return 1;");
		}
		w.WriteLine ("\t\tConsole.WriteLine (\"RUN: \" + nrun + \", FAILED: \" + nfailed);");
		w.WriteLine ("\t\treturn 0;");
		w.WriteLine ("\t}");
		w.WriteLine ("}");
		w.Close ();

		return 0;
	}
}
