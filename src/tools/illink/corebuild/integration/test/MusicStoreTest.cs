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

			BuildAndLink(csproj, rootFiles);
		}

		// returns path to .csproj project file
		string SetupProject()
		{
			string gitRepo = "http://github.com/aspnet/JitBench";
			string repoName = "JitBench";
			string gitBranch = "dev";
			string demoRoot = Path.Combine("JitBench", Path.Combine("src", "MusicStore"));

			int ret;
			if (Directory.Exists(repoName)) {
				Directory.Delete(repoName, true);
			}

			ret = RunCommand("git", $"clone {gitRepo}", null, null);
			if (ret != 0) {
				output.WriteLine("git failed");
				Assert.True(false);
			}

			if (!Directory.Exists(demoRoot)) {
				output.WriteLine($"{demoRoot} does not exist");
				Assert.True(false);
			}

			ret = RunCommand("git", $"checkout {gitBranch}", demoRoot, null);
			if (ret != 0) {
				output.WriteLine($"problem checking out branch {gitBranch}");
				Assert.True(false);
			}

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
