using System;
using System.IO;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class WebApiFixture : ProjectFixture
	{
		public string csproj;

		public WebApiFixture(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) {
			csproj = SetupProject();
		}

		public string SetupProject()
		{
			string projectRoot = CreateTestFolder("webapi");
			string csproj = Path.Combine(projectRoot, $"webapi.csproj");

			if (File.Exists(csproj)) {
				LogMessage($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			int ret = CommandHelper.Dotnet("new webapi", projectRoot);
			if (ret != 0) {
				LogMessage("dotnet new failed");
				Assert.True(false);
			}

			PreventPublishFiltering(csproj);

			AddLinkerReference(csproj);

			AddNuGetConfig(projectRoot);

			return csproj;
		}

		// TODO: Remove this once we figure out what to do about apps
		// that have the publish output filtered by a manifest
		// file. It looks like aspnet has made this the default. See
		// the bug at https://github.com/dotnet/sdk/issues/1160.
		private void PreventPublishFiltering(string csproj) {
			var xdoc = XDocument.Load(csproj);
			var ns = xdoc.Root.GetDefaultNamespace();

			var propertygroup = xdoc.Root.Element(ns + "PropertyGroup");

			LogMessage("setting PublishWithAspNetCoreTargetManifest=false");
			propertygroup.Add(new XElement(ns + "PublishWithAspNetCoreTargetManifest",
										   "false"));

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}
	}

	public class WebApiTest : IntegrationTestBase, IClassFixture<WebApiFixture>
	{
		private readonly WebApiFixture fixture;

		public WebApiTest(WebApiFixture fixture, ITestOutputHelper output) : base(output)
		{
			this.fixture = fixture;
		}

		[Fact]
		public void RunWebApiStandalone()
		{
			string executablePath = BuildAndLink(fixture.csproj, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			string terminatingOutput = "Application started. Press Ctrl+C to shut down.";
			RunApp(target, out string commandOutput, 60000, terminatingOutput, selfContained: selfContained);
			Assert.Contains("Now listening on: http://localhost:5000", commandOutput);
			Assert.Contains(terminatingOutput, commandOutput);
		}
	}
}
