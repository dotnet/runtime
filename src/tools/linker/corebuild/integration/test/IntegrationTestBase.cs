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
			return RunCommand(context.DotnetToolPath, args, workingDir, additionalPath, out string commandOutput);
		}

		protected int RunCommand(string command, string args, int timeout = 60000)
		{
			return RunCommand(command, args, null, null, out string commandOutput, timeout);
		}

		protected int RunCommand(string command, string args, string workingDir)
		{
			return RunCommand(command, args, workingDir, null, out string commandOutput);
		}

		protected int RunCommand(string command, string args, string workingDir, string additionalPath, out string commandOutput, int timeout = 60000)
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
			var process = new Process();
			process.StartInfo = psi;

			StringBuilder processOutput = new StringBuilder();
			DataReceivedEventHandler handler = (sender, e) => {
				processOutput.Append(e.Data);
				processOutput.AppendLine();
			};
			StringBuilder processError = new StringBuilder();
			DataReceivedEventHandler ehandler = (sender, e) => {
				processError.Append(e.Data);
				processError.AppendLine();
			};
			process.OutputDataReceived += handler;
			process.ErrorDataReceived += ehandler;
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			if (!process.WaitForExit(timeout)) {
				process.Kill();
			}
			// WaitForExit with timeout doesn't guarantee
			// that the async output handlers have been
			// called, so WaitForExit needs to be called
			// afterwards.
			process.WaitForExit();
			string processOutputStr = processOutput.ToString();
			string processErrorStr = processError.ToString();
			output.WriteLine(processOutputStr);
			output.WriteLine(processErrorStr);
			commandOutput = processOutputStr;
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
				output.WriteLine("publish failed, returning " + ret);
				Assert.True(false);
				return;
			}
		}

 		public int RunApp(string csproj, out string processOutput, int timeout = 60000)
		{
			string demoRoot = Path.GetDirectoryName(csproj);
			string executablePath = Path.Combine(
				demoRoot, "bin", context.Configuration, "netcoreapp2.0",
				context.RuntimeIdentifier, "publish",
				Path.GetFileNameWithoutExtension(csproj)
			);
			if (context.RuntimeIdentifier.Contains("win")) {
				executablePath += ".exe";
			}
			Assert.True(File.Exists(executablePath));

			// work around bug in prerelease .NET Core,
			// where the published host isn't executable
			int ret;
			if (!context.RuntimeIdentifier.Contains("win")) {
				ret = RunCommand("chmod", "+x " + executablePath, 1000);
				Assert.True(ret == 0);
			}

			ret = RunCommand(executablePath, null,
					 Directory.GetParent(executablePath).FullName,
					 null, out processOutput, timeout);
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
