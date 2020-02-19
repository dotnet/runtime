using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{

	/// <summary>
	///   Represents a project. Each fixture contains setup code run
	///   once before all tests in the same test class. ProjectFixture
	///   is the base type for different specific project fixtures.
	/// </summary>
	public class ProjectFixture
	{
		private readonly FixtureLogger logger;
		protected CommandHelper CommandHelper;

		protected void LogMessage (string message)
		{
			logger.LogMessage (message);
		}

		public ProjectFixture (IMessageSink diagnosticMessageSink)
		{
			logger = new FixtureLogger (diagnosticMessageSink);
			CommandHelper = new CommandHelper (logger);
		}

		protected void AddNuGetConfig(string projectRoot)
		{
			var nugetConfig = Path.Combine(projectRoot, "NuGet.config");
			var xdoc = new XDocument();
			var configuration = new XElement("configuration");
			var packageSources = new XElement("packageSources");
			packageSources.Add(new XElement("add",
						new XAttribute("key", "dotnet-core"),
						new XAttribute("value", "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")));
			packageSources.Add(new XElement("add",
						new XAttribute("key", "local linker feed"),
						new XAttribute("value", TestContext.PackageSource)));

			configuration.Add(packageSources);
			xdoc.Add(configuration);

			using (var fs = new FileStream(nugetConfig, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}

		protected void AddLinkerReference(string csproj)
		{
			var xdoc = XDocument.Load(csproj);
			var ns = xdoc.Root.GetDefaultNamespace();
			bool added = false;
			foreach (var el in xdoc.Root.Elements(ns + "ItemGroup")) {
				if (el.Elements(ns + "PackageReference").Any()) {
					el.Add(new XElement(ns+"PackageReference",
						new XAttribute("Include", TestContext.TasksPackageName),
						new XAttribute("Version", TestContext.TasksPackageVersion)));
					added = true;
					break;
				}
			}
			if (!added) {
				xdoc.Root.Add(new XElement(ns + "ItemGroup",
					new XElement(ns + "PackageReference",
						new XAttribute("Include", TestContext.TasksPackageName),
						new XAttribute("Version", TestContext.TasksPackageVersion))));
			}

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}

		protected string CreateTestFolder(string projectName)
		{
			string tempFolder = Path.GetFullPath(Path.Combine("tests-temp", projectName));
			Directory.CreateDirectory(tempFolder);

			// write empty Directory.Build.props and Directory.Build.targets to disable accidental import of arcade from repo root
			File.WriteAllText(Path.Combine(tempFolder, "Directory.Build.props"), "<Project></Project>");
			File.WriteAllText(Path.Combine(tempFolder, "Directory.Build.targets"), "<Project></Project>");

			return Path.Combine(tempFolder, projectName);
		}

		protected void WriteEmbeddedResource(string resourceName, string destination)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var resourceStream = assembly.GetManifestResourceStream(resourceName);
			resourceStream.CopyTo(File.Create(destination));
		}
	}

	/// <summary>
	///   Contains logic shared by multiple test classes.
	/// </summary>
	public class IntegrationTestBase
	{
		private readonly TestLogger logger;
		protected readonly CommandHelper CommandHelper;

		public IntegrationTestBase(ITestOutputHelper output)
		{
			logger = new TestLogger(output);
			CommandHelper = new CommandHelper(logger);
		}

		private void LogMessage (string message)
		{
			logger.LogMessage (message);
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

			string publishArgs = $"publish -c {TestContext.Configuration} /v:n /p:ShowLinkerSizeComparison=true";
			if (selfContained) {
				publishArgs += $" -r {TestContext.RuntimeIdentifier}";
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
			int ret = CommandHelper.Dotnet(publishArgs, demoRoot);

			if (ret != 0) {
				LogMessage("publish failed, returning " + ret);
				Assert.True(false);
			}

			// detect the target framework for which the app was published
			string tfmDir = Path.Combine(demoRoot, "bin", TestContext.Configuration);
			string tfm = Directory.GetDirectories(tfmDir).Select(p => Path.GetFileName(p)).Single();
			string builtApp = Path.Combine(tfmDir, tfm);
			if (selfContained) {
				builtApp = Path.Combine(builtApp, TestContext.RuntimeIdentifier);
			}
			builtApp = Path.Combine(builtApp, "publish",
				Path.GetFileNameWithoutExtension(csproj));
			if (selfContained) {
				if (TestContext.RuntimeIdentifier.Contains("win")) {
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
				ret = CommandHelper.RunCommand(
					target, null,
					Directory.GetParent(target).FullName,
					null, out processOutput, timeout, terminatingOutput);
			} else {
				ret = CommandHelper.RunCommand(
					Path.GetFullPath(TestContext.DotnetToolPath),
					Path.GetFullPath(target),
					Directory.GetParent(target).FullName,
					null, out processOutput, timeout, terminatingOutput);
			}
			return ret;
		}
	}
}
