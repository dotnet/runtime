using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;

namespace ILLink.Tests
{
	public static class TestContext
	{
		/// <summary>
		///   The path to the ILLink.Tasks.dll assembly
		/// </summary>
		public static string TasksAssemblyPath { get; private set; }

		/// <summary>
		///   The path to the dotnet tool to use to run the
		///   integration tests.
		/// </summary>
		public static string DotnetToolPath { get; set; }

		/// <summary>
		///   The RID to use when restoring, building, and linking the
		///   integration test projects.
		/// </summary>
		public static string RuntimeIdentifier { get; private set; }

		/// <summary>
		///   The configuration to use to build the integration test
		///   projects.
		/// </summary>
		public static string Configuration { get; private set; }

		/// <summary>
		///   The root testbin directory. Used to install test
		///   assets that don't depend on the configuration or
		///   target framework.
		/// </summary>
		public static string TestBin { get; private set; }

		static TestContext()
		{
			SetupDefaultContext();
		}

		/// <summary>
		///   This is the context from which tests will be run in the
		///   linker repo. The local directory that contains the
		///   linker integration packages (hard-coded here) is
		///   searched for the tasks package. This assumes that only
		///   one version of the package is present, and uses it to
		///   unambiguously determine which package to use in the tests.
		/// </summary>
		public static void SetupDefaultContext()
		{
			// test working directory is test project's <baseoutputpath>/<config>/<tfm>
			var testBin = Path.Combine(Environment.CurrentDirectory, "..", "..");
			var repoRoot = Path.GetFullPath(Path.Combine(testBin, "..", "..", ".."));

			// We want to build and link integration projects in the
			// release configuration.
			Configuration = "Release";
			TestBin = testBin;

#if DEBUG
			var illinkConfiguration = "Debug";
#else
			var illinkConfiguration = "Release";
#endif

			// Locate tasks assembly
			var tasksAssembly = Path.Combine(repoRoot, "artifacts", "bin", "ILLink.Tasks", illinkConfiguration, "netcoreapp3.0", "ILLink.Tasks.dll");
			if (!File.Exists(tasksAssembly)) {
				throw new Exception("ILLink.Tasks not found at " + tasksAssembly);
			}


			// Locate dotnet host
			var dotnetDir = Path.Combine(repoRoot, ".dotnet");
			var dotnetToolName = Directory.GetFiles(dotnetDir)
				.Select(p => Path.GetFileName(p))
				.Where(p => p.StartsWith("dotnet"))
				.Where(p => {
					var ext = Path.GetExtension(p);
					return ext == "" || ext == ".exe";
				})
				.Single();
			var dotnetToolPath = Path.Combine(dotnetDir, dotnetToolName);

			// Initialize static members
			TasksAssemblyPath = tasksAssembly;
			DotnetToolPath = dotnetToolPath;
			// This sets the RID to the RID of the currently-executing system.
			RuntimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
			// workaround: the osx.10.13-x64 RID doesn't exist yet.
			// see https://github.com/NuGet/Home/issues/5862
			if (RuntimeIdentifier == "osx.10.14-x64")
			{
				RuntimeIdentifier = "osx.10.13-x64";
			}
		}
	}
}
