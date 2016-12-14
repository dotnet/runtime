using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class HelloWorldTest : IntegrationTestBase
	{
		public HelloWorldTest(ITestOutputHelper output) : base(output) {}

		public string SetupProject()
		{
			string projectRoot = "helloworld";

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			int ret = Dotnet("new console", projectRoot);
			if (ret != 0) {
				output.WriteLine("dotnet new failed");
				Assert.True(false);
			}

			string csproj = Path.Combine(projectRoot, $"{projectRoot}.csproj");
			return csproj;
		}

		[Fact]
		public void RunHelloWorld()
		{
			string csproj = SetupProject();

			AddLinkerReference(csproj);

			BuildAndLink(csproj, null);
		}
	}
}
