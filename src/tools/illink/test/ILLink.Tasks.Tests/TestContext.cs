using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;

namespace ILLink.Tests
{
	public static class TestContext
	{
		/// <summary>
		///   The name of the tasks package to add to the integration
		///   projects.
		/// </summary>
		public static string TasksPackageName { get; private set; }

		/// <summary>
		///   The version of the tasks package to add to the
		///   integration projects.
		/// </summary>
		public static string TasksPackageVersion { get; private set; }

		/// <summary>
		///   The path of the directory from which to get the linker
		///   package.
		/// </summary>
		public static string PackageSource { get; private set; }

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
		///   unambiguously determine which pacakge to use in the tests.
		/// </summary>
		public static void SetupDefaultContext()
		{
			// test working directory is test project's <baseoutputpath>/<config>/<tfm>
			var testBin = Path.Combine(Environment.CurrentDirectory, "..", "..");
			var repoRoot = Path.GetFullPath(Path.Combine(testBin, "..", "..", ".."));

			// Locate task package
			var packageName = "ILLink.Tasks";
			var packageSource = Path.Combine(repoRoot, "src", "ILLink.Tasks", "bin", "nupkgs");
			var tasksPackages = Directory.GetFiles(packageSource)
				.Where(p => Path.GetExtension(p) == ".nupkg")
				.Select(p => Path.GetFileNameWithoutExtension(p))
				.Where(p => p.StartsWith(packageName));
			var nPackages = tasksPackages.Count();
			if (nPackages > 1) {
				throw new Exception($"duplicate {packageName} packages in {packageSource}");
			} else if (nPackages == 0) {
				throw new Exception($"{packageName} package not found in {packageSource}");
			}
			var tasksPackage = tasksPackages.Single();
			var version = tasksPackage.Remove(0, packageName.Length + 1);

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
			PackageSource = packageSource;
			TasksPackageName = packageName;
			TasksPackageVersion = version;
			DotnetToolPath = dotnetToolPath;
			// This sets the RID to the RID of the currently-executing system.
			RuntimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
			// workaround: the osx.10.13-x64 RID doesn't exist yet.
			// see https://github.com/NuGet/Home/issues/5862
			if (RuntimeIdentifier == "osx.10.14-x64")
			{
				RuntimeIdentifier = "osx.10.13-x64";
			}
			// We want to build and link integration projects in the
			// release configuration.
			Configuration = "Release";
			TestBin = testBin;
		}
	}
}
