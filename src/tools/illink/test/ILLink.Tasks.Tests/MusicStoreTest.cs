using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions; // ITestOutputHelper

namespace ILLink.Tests
{
	public class MusicStoreFixture : ProjectFixture
	{
		public static List<string> rootFiles = new List<string> { "MusicStoreReflection.xml" };

		private static string gitRepo = "http://github.com/aspnet/JitBench";
		private static string repoName = "JitBench";

		// Revision can also be a branch name. We generally
		// want to ensure that we are able to link the latest
		// MusicStore from the dev branch.
		private static string gitRevision = "ac314bd68294ae0f91bd16df20cf5ebd4b8ef5b5";

		// The version of Microsoft.NETCore.App that
		// musicstore will run on (and deploy with, for
		// self-contained deployments).
		private static string runtimeVersion = "3.0.0-preview-27324-5";

		// The version of the SDK used to build and link
		// musicstore, if a specific version is desired.
		private static string sdkVersion = "2.2.0-preview1-007525";

		// The version of Microsoft.AspNetCore.All to publish with.
		private static string aspNetVersion = "2.1.0-preview1-27654";

		public static Dictionary<string, string> versionPublishArgs;
		public static Dictionary<string, string> VersionPublishArgs
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

		public static string csproj;

		public MusicStoreFixture(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
		{
			csproj = SetupProject();
		}

		// returns path to .csproj project file
		string SetupProject()
		{
			int ret;
			string demoRoot = Path.Combine(repoName, Path.Combine("src", "MusicStore"));
			string csproj = Path.Combine(demoRoot, "MusicStore.csproj");

			if (File.Exists(csproj)) {
				LogMessage($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(repoName)) {
				Directory.Delete(repoName, true);
			}

			ret = CommandHelper.RunCommand("git", $"clone {gitRepo} {repoName}");
			if (ret != 0) {
				LogMessage("git failed");
				Assert.True(false);
			}

			if (!Directory.Exists(demoRoot)) {
				LogMessage($"{demoRoot} does not exist");
				Assert.True(false);
			}

			ret = CommandHelper.RunCommand("git", $"checkout {gitRevision}", demoRoot);
			if (ret != 0) {
				LogMessage($"problem checking out revision {gitRevision}");
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

			// We no longer need a custom global.json, because we are
			// using the same SDK used in the repo.
			// AddGlobalJson(repoName);

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
				LogMessage("repo-local dotnet tool does not exist.");
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
			if (TestContext.RuntimeIdentifier.Contains("win")) {
				dotnetInstall += ".ps1";
			} else {
				dotnetInstall += ".sh";
			}
			if (!File.Exists(dotnetInstall)) {
				LogMessage($"missing dotnet-install script at {dotnetInstall}");
				Assert.True(false);
			}

			if (TestContext.RuntimeIdentifier.Contains("win")) {
				ret = CommandHelper.RunCommand("powershell", $"{dotnetInstall} -SharedRuntime -InstallDir {dotnetDirName} -Channel master -Architecture x64 -Version {runtimeVersion}", rootDir);
				if (ret != 0) {
					LogMessage("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = CommandHelper.RunCommand("powershell", $"{dotnetInstall} -InstallDir {dotnetDirName} -Channel master -Architecture x64 -Version {sdkVersion}", rootDir);
				if (ret != 0) {
					LogMessage("failed to retrieve sdk");
					Assert.True(false);
				}
			} else {
				ret = CommandHelper.RunCommand(dotnetInstall, $"-sharedruntime -runtimeid {TestContext.RuntimeIdentifier} -installdir {dotnetDirName} -channel master -architecture x64 -version {runtimeVersion}", rootDir);
				if (ret != 0) {
					LogMessage("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = CommandHelper.RunCommand(dotnetInstall, $"-installdir {dotnetDirName} -channel master -architecture x64 -version {sdkVersion}", rootDir);
				if (ret != 0) {
					LogMessage("failed to retrieve sdk");
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
			string localPackagePath = Path.GetFullPath(TestContext.PackageSource);
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

	public class MusicStoreTest : IntegrationTestBase, IClassFixture<MusicStoreFixture>
	{

		MusicStoreFixture fixture;

		public MusicStoreTest(MusicStoreFixture fixture, ITestOutputHelper output) : base(output) {
			this.fixture = fixture;
			// MusicStore has been updated to target netcoreapp3.0, so
			// we should be able to run on the SDK used to build this
			// repo.
			// context.DotnetToolPath = ObtainSDK(context.TestBin, repoName);
		}

		[Fact]
		public void RunMusicStoreStandalone()
		{
			string executablePath = BuildAndLink(MusicStoreFixture.csproj, MusicStoreFixture.rootFiles, MusicStoreFixture.VersionPublishArgs, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		[Fact]
		public void RunMusicStorePortable()
		{
			Dictionary<string, string> extraPublishArgs = new Dictionary<string, string>(MusicStoreFixture.VersionPublishArgs);
			extraPublishArgs.Add("PublishWithAspNetCoreTargetManifest", "false");
			string target = BuildAndLink(MusicStoreFixture.csproj, null, extraPublishArgs, selfContained: false);
			CheckOutput(target, selfContained: false);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			int ret = RunApp(target, out string commandOutput, selfContained: selfContained);

			Assert.Contains("starting request to http://localhost:5000", commandOutput);
			Assert.Contains("Response: OK", commandOutput);
			Assert.Contains("Running 100 requests", commandOutput);
			Assert.True(ret == 0);
		}

	}
}
