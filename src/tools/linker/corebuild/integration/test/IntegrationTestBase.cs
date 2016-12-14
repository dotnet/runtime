using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
			return RunCommand(context.DotnetToolPath, args, workingDir, additionalPath);
		}

		protected int RunCommand(string command, string args, string workingDir, string additionalPath = null)
		{
			output.WriteLine($"{command} {args}");
			if (workingDir != null)
				output.WriteLine($"working directory: {workingDir}");
			var psi = new ProcessStartInfo
			{
				FileName = command,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = workingDir,
			};

			if (additionalPath != null) {
				string path = psi.Environment["PATH"];
				psi.Environment["PATH"] = path + ";" + additionalPath;
			}

			var process = new Process
			{
				StartInfo = psi,
			};

			process.Start();
			string capturedOutput = process.StandardOutput.ReadToEnd();
			output.WriteLine(capturedOutput);
			string capturedError = process.StandardError.ReadToEnd();
			output.WriteLine(capturedError);
			process.WaitForExit();
			return process.ExitCode;
		}

		/// <summary>
		///   Run the linker on the specified project. This assumes
		///   that the project already contains a reference to the
		///   linker task package.
		///   Optionally takes a list of root descriptor files.
		/// </summary>
		public void BuildAndLink(string csproj, List<string> rootFiles = null)
		{
			string rid = context.RuntimeIdentifier;
			string config = context.Configuration;
			string demoRoot = Path.GetDirectoryName(csproj);

			int ret = Dotnet($"restore -r {rid}", demoRoot);
			if (ret != 0) {
				output.WriteLine("restore failed");
				Assert.True(false);
				return;
			}

			string publishArgs = $"publish -r {rid} -c {config} /v:n /p:ShowLinkerSizeComparison=true";
			string rootFilesStr;
			if (rootFiles != null && rootFiles.Any()) {
				rootFilesStr = String.Join(";", rootFiles);
				publishArgs += $" /p:LinkerRootDescriptors={rootFilesStr}";
			}
			ret = Dotnet(publishArgs, demoRoot);
			if (ret != 0) {
				output.WriteLine("publish failed");
				Assert.True(false);
				return;
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
