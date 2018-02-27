using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions; // ITestOutputHelper

namespace ILLink.Tests
{
	public class MusicStoreTest : IntegrationTestBase
	{
		private static List<string> rootFiles = new List<string> { "MusicStoreReflection.xml" };

		private static string gitRepo = "http://github.com/aspnet/JitBench";
		private static string repoName = "JitBench";

		// Revision can also be a branch name. We generally
		// want to ensure that we are able to link the latest
		// MusicStore from the dev branch.
		private static string gitRevision = "536f89ac6178246e58125401480b0a3b6406efe8";

		// The version of Microsoft.NETCore.App that
		// musicstore will run on (and deploy with, for
		// self-contained deployments).
		private static string runtimeVersion = "2.1.0-preview1-25915-01";

		// The version of the SDK used to build and link
		// musicstore.
		private static string sdkVersion = "2.2.0-preview1-007525";

		// The version of Microsoft.AspNetCore.All to publish with.
		private static string aspNetVersion = "2.1.0-preview1-27654";

		private static Dictionary<string, string> versionPublishArgs;
		private static Dictionary<string, string> VersionPublishArgs
		{
			get {
				if (versionPublishArgs != null) {
					return versionPublishArgs;
				}
				versionPublishArgs = new Dictionary<string, string>();
				versionPublishArgs.Add("JITBENCH_FRAMEWORK_VERSION", runtimeVersion);
				versionPublishArgs.Add("JITBENCH_ASPNET_VERSION", aspNetVersion);
				return versionPublishArgs;
			}
		}

		private static string csproj;

		public MusicStoreTest(ITestOutputHelper output) : base(output) {
			csproj = SetupProject();

			// MusicStore targets .NET Core 2.1, so it must be built
			// using an SDK that can target 2.1. We obtain that SDK
			// here.
			context.DotnetToolPath = ObtainSDK(context.TestBin, repoName);
		}

		[Fact]
		public void RunMusicStoreStandalone()
		{
			string executablePath = BuildAndLink(csproj, rootFiles, VersionPublishArgs, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		[Fact]
		public void RunMusicStorePortable()
		{
			Dictionary<string, string> extraPublishArgs = new Dictionary<string, string>(VersionPublishArgs);
			extraPublishArgs.Add("PublishWithAspNetCoreTargetManifest", "false");
			string target = BuildAndLink(csproj, null, extraPublishArgs, selfContained: false);
			CheckOutput(target, selfContained: false);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			int ret = RunApp(target, out string commandOutput, selfContained: selfContained);

			Assert.True(commandOutput.Contains("Starting request to http://localhost:5000"));
			Assert.True(commandOutput.Contains("Response: OK"));
			Assert.True(commandOutput.Contains("Running 100 requests"));
			Assert.True(ret == 0);
		}

		// returns path to .csproj project file
		string SetupProject()
		{
			int ret;
			string demoRoot = Path.Combine(repoName, Path.Combine("src", "MusicStore"));
			string csproj = Path.Combine(demoRoot, "MusicStore.csproj");

			if (File.Exists(csproj)) {
				output.WriteLine($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(repoName)) {
				Directory.Delete(repoName, true);
			}

			ret = RunCommand("git", $"clone {gitRepo} {repoName}");
			if (ret != 0) {
				output.WriteLine("git failed");
				Assert.True(false);
			}

			if (!Directory.Exists(demoRoot)) {
				output.WriteLine($"{demoRoot} does not exist");
				Assert.True(false);
			}

			ret = RunCommand("git", $"checkout {gitRevision}", demoRoot);
			if (ret != 0) {
				output.WriteLine($"problem checking out revision {gitRevision}");
				Assert.True(false);
			}

			// Copy root files into the project directory
			CopyRootFiles(demoRoot);

			// This is necessary because JitBench comes with a
			// NuGet.Config that has a <clear /> line, preventing
			// NuGet.Config sources defined in outer directories from
			// applying.
			string nugetConfig = Path.Combine(repoName, "NuGet.config");
			AddLocalNugetFeedAfterClear(nugetConfig);

			AddLinkerReference(csproj);

			AddGlobalJson(repoName);

			return csproj;
		}

		void AddGlobalJson(string repoDir)
		{
			string globalJson = Path.Combine(repoDir, "global.json");
			string globalJsonContents = "{ \"sdk\": { \"version\": \"" + sdkVersion + "\" } }\n";
			File.WriteAllText(globalJson, globalJsonContents);
		}


		string GetDotnetToolPath(string dotnetDir)
		{
			string dotnetToolName = Directory.GetFiles(dotnetDir)
				.Select(p => Path.GetFileName(p))
				.Where(p => p.Contains("dotnet"))
				.Single();
			string dotnetToolPath = Path.Combine(dotnetDir, dotnetToolName);

			if (!File.Exists(dotnetToolPath)) {
				output.WriteLine("repo-local dotnet tool does not exist.");
				Assert.True(false);
			}

			return dotnetToolPath;
		}

		string ObtainSDK(string rootDir, string repoDir)
		{
			int ret;
			string dotnetDirName = ".dotnet";
			string dotnetDir = Path.Combine(rootDir, dotnetDirName);
			if (Directory.Exists(dotnetDir)) {
				return GetDotnetToolPath(dotnetDir);
			}

			string dotnetInstall = Path.Combine(Path.GetFullPath(repoDir), "dotnet-install");
			if (context.RuntimeIdentifier.Contains("win")) {
				dotnetInstall += ".ps1";
			} else {
				dotnetInstall += ".sh";
			}
			if (!File.Exists(dotnetInstall)) {
				output.WriteLine($"missing dotnet-install script at {dotnetInstall}");
				Assert.True(false);
			}

			if (context.RuntimeIdentifier.Contains("win")) {
				ret = RunCommand("powershell", $"{dotnetInstall} -SharedRuntime -InstallDir {dotnetDirName} -Channel master -Architecture x64 -Version {runtimeVersion}", rootDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = RunCommand("powershell", $"{dotnetInstall} -InstallDir {dotnetDirName} -Channel master -Architecture x64 -Version {sdkVersion}", rootDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve sdk");
					Assert.True(false);
				}
			} else {
				ret = RunCommand(dotnetInstall, $"-sharedruntime -runtimeid {context.RuntimeIdentifier} -installdir {dotnetDirName} -channel master -architecture x64 -version {runtimeVersion}", rootDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = RunCommand(dotnetInstall, $"-installdir {dotnetDirName} -channel master -architecture x64 -version {sdkVersion}", rootDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve sdk");
					Assert.True(false);
				}
			}

			return GetDotnetToolPath(dotnetDir);
		}

		static void CopyRootFiles(string demoRoot)
		{
			foreach (var rf in rootFiles) {
				File.Copy(rf, Path.Combine(demoRoot, rf));
			}
		}

		private void AddLocalNugetFeedAfterClear(string nugetConfig)
		{
			string localPackagePath = Path.GetFullPath(context.PackageSource);
			var xdoc = XDocument.Load(nugetConfig);
			var ns = xdoc.Root.GetDefaultNamespace();
			var clear = xdoc.Root.Element(ns+"packageSources").Element(ns+"clear");
			clear.Parent.Add(new XElement(ns+"add",
						new XAttribute("key", "local linker feed"),
						new XAttribute("value", localPackagePath)));

			using (var fs = new FileStream(nugetConfig, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}
	}
}
