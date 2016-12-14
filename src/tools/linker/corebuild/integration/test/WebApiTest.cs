using System;
using System.IO;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class WebApiTest : IntegrationTestBase
	{
		public WebApiTest(ITestOutputHelper output) : base(output) {}

		public string SetupProject()
		{
			string projectRoot = "webapi";

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			int ret = Dotnet("new webapi", projectRoot);
			if (ret != 0) {
				output.WriteLine("dotnet new failed");
				Assert.True(false);
			}

			string csproj = Path.Combine(projectRoot, $"{projectRoot}.csproj");
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

			output.WriteLine("setting PublishWithAspNetCoreTargetManifest=false");
			propertygroup.Add(new XElement(ns + "PublishWithAspNetCoreTargetManifest",
										   "false"));

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}

		[Fact]
		public void RunWebApi()
		{
			string csproj = SetupProject();

			PreventPublishFiltering(csproj);

			AddLinkerReference(csproj);

			BuildAndLink(csproj);
		}
	}
}
