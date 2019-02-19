using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class IntegrationTestBase
	{
		protected readonly ITestOutputHelper output;

		protected readonly TestContext context;

		public IntegrationTestBase(ITestOutputHelper output)
		{
			this.output = output;

			// This sets up the context with some values specific to
			// the setup of the linker repository. A different context
			// should be used in order to run tests in a different
			// environment.
			this.context = TestContext.CreateDefaultContext();
		}

		protected int Dotnet(string args, string workingDir, string additionalPath = null)
		{
			return RunCommand(Path.GetFullPath(context.DotnetToolPath), args,
				workingDir, additionalPath, out string commandOutput);
		}

		protected int RunCommand(string command, string args, int timeout = Int32.MaxValue)
		{
			return RunCommand(command, args, null, null, out string commandOutput, timeout);
		}

		protected int RunCommand(string command, string args, string workingDir)
		{
			return RunCommand(command, args, workingDir, null, out string commandOutput);
		}

		protected int RunCommand(string command, string args, string workingDir, string additionalPath,
			out string commandOutput, int timeout = Int32.MaxValue, string terminatingOutput = null)
		{
			return (new CommandRunner(command, output))
				.WithArguments(args)
				.WithWorkingDir(workingDir)
				.WithAdditionalPath(additionalPath)
				.WithTimeout(timeout)
				.WithTerminatingOutput(terminatingOutput)
				.Run(out commandOutput);
		}

		/// <summary>
		///   Run the linker on the specified project. This assumes
		///   that the project already contains a reference to the
		///   linker task package.
		///   Optionally takes a list of root descriptor files.
		///   Returns the path to the built app, either the renamed
		///   host for self-contained publish, or the dll containing
		///   the entry point.
		/// </summary>
		public string BuildAndLink(string csproj, List<string> rootFiles = null, Dictionary<string, string> extraPublishArgs = null, bool selfContained = false)
		{
			string demoRoot = Path.GetDirectoryName(csproj);

			string publishArgs = $"publish -c {context.Configuration} /v:n /p:ShowLinkerSizeComparison=true";
			if (selfContained) {
				publishArgs += $" -r {context.RuntimeIdentifier}";
			}
			string rootFilesStr;
			if (rootFiles != null && rootFiles.Any()) {
				rootFilesStr = String.Join(";", rootFiles);
				publishArgs += $" /p:LinkerRootDescriptors={rootFilesStr}";
			}
			if (extraPublishArgs != null) {
				foreach (var item in extraPublishArgs) {
					publishArgs += $" /p:{item.Key}={item.Value}";
				}
			}
			int ret = Dotnet(publishArgs, demoRoot);

			if (ret != 0) {
				output.WriteLine("publish failed, returning " + ret);
				Assert.True(false);
			}

			// detect the target framework for which the app was published
			string tfmDir = Path.Combine(demoRoot, "bin", context.Configuration);
			string tfm = Directory.GetDirectories(tfmDir).Select(p => Path.GetFileName(p)).Single();
			string builtApp = Path.Combine(tfmDir, tfm);
			if (selfContained) {
				builtApp = Path.Combine(builtApp, context.RuntimeIdentifier);
			}
			builtApp = Path.Combine(builtApp, "publish",
				Path.GetFileNameWithoutExtension(csproj));
			if (selfContained) {
				if (context.RuntimeIdentifier.Contains("win")) {
					builtApp += ".exe";
				}
			} else {
				builtApp += ".dll";
			}
			Assert.True(File.Exists(builtApp));
			return builtApp;
		}

		public int RunApp(string target, out string processOutput, int timeout = Int32.MaxValue,
			string terminatingOutput = null, bool selfContained = false)
		{
			Assert.True(File.Exists(target));
			int ret;
			if (selfContained) {
				ret = RunCommand(
					target, null,
					Directory.GetParent(target).FullName,
					null, out processOutput, timeout, terminatingOutput);
			} else {
				ret = RunCommand(
					Path.GetFullPath(context.DotnetToolPath),
					Path.GetFullPath(target),
					Directory.GetParent(target).FullName,
					null, out processOutput, timeout, terminatingOutput);
			}
			return ret;
		}

		protected void AddLinkerReference(string csproj)
		{
			var xdoc = XDocument.Load(csproj);
			var ns = xdoc.Root.GetDefaultNamespace();
			bool added = false;
			foreach (var el in xdoc.Root.Elements(ns + "ItemGroup")) {
				if (el.Elements(ns + "PackageReference").Any()) {
					el.Add(new XElement(ns+"PackageReference",
						new XAttribute("Include", context.TasksPackageName),
						new XAttribute("Version", context.TasksPackageVersion)));
					added = true;
					break;
				}
			}
			if (!added) {
				xdoc.Root.Add(new XElement(ns + "ItemGroup",
					new XElement(ns + "PackageReference",
						new XAttribute("Include", context.TasksPackageName),
						new XAttribute("Version", context.TasksPackageVersion))));
				added= true;
			}

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}

		static void AddLinkerRoots(string csproj, List<string> rootFiles)
		{
			var xdoc = XDocument.Load(csproj);
			var ns = xdoc.Root.GetDefaultNamespace();

			var rootsItemGroup = new XElement(ns+"ItemGroup");
			foreach (var rootFile in rootFiles) {
				rootsItemGroup.Add(new XElement(ns+"LinkerRootFiles",
					new XAttribute("Include", rootFile)));
			}

			var propertyGroup = xdoc.Root.Elements(ns + "PropertyGroup").First();
			propertyGroup.AddAfterSelf(rootsItemGroup);

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}
	}
}
