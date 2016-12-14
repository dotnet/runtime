using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;

namespace ILLink.Tests
{
	public class TestContext
	{
		/// <summary>
		///   The name of the tasks package to add to the integration
		///   projects.
		/// </summary>
		public string TasksPackageName { get; private set; }

		/// <summary>
		///   The version of the tasks package to add to the
		///   integration projects.
		/// </summary>
		public string TasksPackageVersion { get; private set; }

		/// <summary>
		///   The path of the directory from which to get the linker
		///   package.
		/// </summary>
		public string PackageSource { get; private set; }

		/// <summary>
		///   The path to the dotnet tool to use to run the
		///   integration tests.
		/// </summary>
		public string DotnetToolPath { get; private set; }

		/// <summary>
		///   The RID to use when restoring, building, and linking the
		///   integration test projects.
		/// </summary>
		public string RuntimeIdentifier { get; private set; }

		/// <summary>
		///   The configuration to use to build the integration test
		///   projects.
		/// </summary>
		public string Configuration { get; private set; }

		/// <summary>
		///   This is the context from which tests will be run in the
		///   linker repo. The local directory that contains the
		///   linker integration packages (hard-coded here) is
		///   searched for the tasks package. This assumes that only
		///   one version of the package is present, and uses it to
		///   unambiguously determine which pacakge to use in the tests.
		/// </summary>
		public static TestContext CreateDefaultContext()
		{
			var packageName = "ILLink.Tasks";
			var packageSource = "../../../../nupkgs";
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
			var dotnetDir = "../../../../../../corebuild/Tools/dotnetcli";
			var dotnetToolNames = Directory.GetFiles(dotnetDir)
				.Select(p => Path.GetFileName(p))
				.Where(p => p.Contains("dotnet"));
			var nTools = dotnetToolNames.Count();
			if (nTools > 1) {
				throw new Exception($"multiple dotnet tools in {dotnetDir}");
			} else if (nTools == 0) {
				throw new Exception($"no dotnet tool found in {dotnetDir}");
			}
			var dotnetToolName = dotnetToolNames.Single();
			var dotnetToolPath = Path.Combine(dotnetDir, dotnetToolName);

			var context = new TestContext();
			context.PackageSource = packageSource;
			context.TasksPackageName = packageName;
			context.TasksPackageVersion = version;
			context.DotnetToolPath = dotnetToolPath;
			// This sets the RID to the RID of the currently-executing system.
			context.RuntimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
			// We want to build and link integration projects in the
			// release configuration.
			context.Configuration = "Release";
			return context;
		}
	}
}
