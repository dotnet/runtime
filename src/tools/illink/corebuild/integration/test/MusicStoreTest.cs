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
		public MusicStoreTest(ITestOutputHelper output) : base(output) {}

		private static List<string> rootFiles = new List<string> { "MusicStoreReflection.xml" };

		private string netcoreappVersion;

		[Fact]
		public void RunMusicStore()
		{
			string csproj = SetupProject();

			// Copy root files into the project directory
			string demoRoot= Path.GetDirectoryName(csproj);
			CopyRootFiles(demoRoot);

			// This is necessary because JitBench comes with a
			// NuGet.Config that has a <clear /> line, preventing
			// NuGet.Config sources defined in outer directories from
			// applying.
			string nugetConfig = Path.Combine("JitBench", "NuGet.config");
			AddLocalNugetFeedAfterClear(nugetConfig);

			AddLinkerReference(csproj);

			Dictionary<string, string> extraPublishArgs = new Dictionary<string, string>();
			extraPublishArgs.Add("JITBENCH_FRAMEWORK_VERSION", netcoreappVersion);
			BuildAndLink(csproj, rootFiles, extraPublishArgs);
		}

		string ObtainSDK(string repoDir)
		{
			int ret;
			string dotnetDirName = ".dotnet";
			string dotnetInstall = Path.Combine(repoDir, "dotnet-install");
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
				ret = RunCommand(dotnetInstall, $"-SharedRuntime -InstallDir {dotnetDirName} -Channel master -Architecture x64", repoDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = RunCommand(dotnetInstall, $"-InstallDir {dotnetDirName} -Channel master -Architecture x64", repoDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve sdk");
					Assert.True(false);
				}
			} else {
				ret = RunCommand(dotnetInstall, $"-sharedruntime -runtimeid {context.RuntimeIdentifier} -installdir {dotnetDirName} -channel master -architecture x64", repoDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve shared runtime");
					Assert.True(false);
				}
				ret = RunCommand(dotnetInstall, $"-installdir {dotnetDirName} -channel master -architecture x64", repoDir);
				if (ret != 0) {
					output.WriteLine("failed to retrieve sdk");
					Assert.True(false);
				}
			}

			string dotnetDir = Path.Combine(repoDir, dotnetDirName);
			string dotnetToolName = Directory.GetFiles(dotnetDir)
				.Select(p => Path.GetFileName(p))
				.Where(p => p.Contains("dotnet"))
				.Single();
			string dotnetToolPath = Path.Combine(dotnetDir, dotnetToolName);
			if (!File.Exists(dotnetToolPath)) {
				output.WriteLine("repo-local dotnet tool does not exist.");
				Assert.True(false);
			}

			string ncaDir = Path.Combine(dotnetDir, "shared", "Microsoft.NETCore.App");
			netcoreappVersion = Directory.GetDirectories(ncaDir)
				.Select(p => Path.GetFileName(p)).Max();
			if (String.IsNullOrEmpty(netcoreappVersion)) {
				output.WriteLine($"no netcoreapp version found in {ncaDir}");
				Assert.True(false);
			}

			return dotnetToolPath;
		}

		// returns path to .csproj project file
		string SetupProject()
		{
			string gitRepo = "http://github.com/aspnet/JitBench";
			string repoName = "JitBench";
			string gitBranch = "dev";

			int ret;
			if (Directory.Exists(repoName)) {
				Directory.Delete(repoName, true);
			}

			ret = RunCommand("git", $"clone {gitRepo}");
			if (ret != 0) {
				output.WriteLine("git failed");
				Assert.True(false);
			}

			string demoRoot = Path.Combine("JitBench", Path.Combine("src", "MusicStore"));
			if (!Directory.Exists(demoRoot)) {
				output.WriteLine($"{demoRoot} does not exist");
				Assert.True(false);
			}

			ret = RunCommand("git", $"checkout {gitBranch}", demoRoot);
			if (ret != 0) {
				output.WriteLine($"problem checking out branch {gitBranch}");
				Assert.True(false);
			}

			// MusicStore targets .NET Core 2.1, so it must be built
			// using an SDK that can target 2.1. We obtain that SDK
			// here.
			context.DotnetToolPath = ObtainSDK(repoName);

			string csproj = Path.Combine(demoRoot, "MusicStore.csproj");
			return csproj;
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
