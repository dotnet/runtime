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

		protected int RunCommand(string command, string args, string workingDir, string additionalPath, out string commandOutput, int timeout = Int32.MaxValue)
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

			// dotnet sets some environment variables that
			// may cause problems in the child process.
			psi.Environment.Remove("MSBuildExtensionsPath");
			psi.Environment.Remove("MSBuildLoadMicrosoftTargetsReadOnly");
			psi.Environment.Remove("MSBuildSDKsPath");
			psi.Environment.Remove("VbcToolExe");
			psi.Environment.Remove("CscToolExe");
			psi.Environment.Remove("MSBUILD_EXE_PATH");

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
			output.WriteLine("environment:");
			foreach (var item in psi.Environment) {
				output.WriteLine($"\t{item.Key}={item.Value}");
			}
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			if (!process.WaitForExit(timeout)) {
				output.WriteLine($"killing process after {timeout} ms");
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
		public void BuildAndLink(string csproj, List<string> rootFiles = null, Dictionary<string, string> extraPublishArgs = null)
		{
			string rid = context.RuntimeIdentifier;
			string config = context.Configuration;
			string demoRoot = Path.GetDirectoryName(csproj);

			string publishArgs = $"publish -r {rid} -c {config} /v:n /p:ShowLinkerSizeComparison=true";
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
				return;
			}
		}

		public int RunApp(string csproj, out string processOutput, int timeout = Int32.MaxValue)
		{
			string demoRoot = Path.GetDirectoryName(csproj);
			// detect the target framework for which the app was published
			string tfmDir = Path.Combine(demoRoot, "bin", context.Configuration);
			string tfm = Directory.GetDirectories(tfmDir).Select(p => Path.GetFileName(p)).Single();
			string executablePath = Path.Combine(tfmDir, tfm,
				context.RuntimeIdentifier, "publish",
				Path.GetFileNameWithoutExtension(csproj)
			);
			if (context.RuntimeIdentifier.Contains("win")) {
				executablePath += ".exe";
			}
			Assert.True(File.Exists(executablePath));

			int ret = RunCommand(executablePath, null,
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
