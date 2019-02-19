using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{
	public class HelloWorldTest : IntegrationTestBase
	{
		private string csproj;

		public HelloWorldTest(ITestOutputHelper output) : base(output) {
			csproj = SetupProject();
		}

		public string SetupProject()
		{
			string projectRoot = "helloworld";
			string csproj = Path.Combine(projectRoot, $"{projectRoot}.csproj");

			if (File.Exists(csproj)) {
				output.WriteLine($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			int ret = Dotnet("new console", projectRoot);
			if (ret != 0) {
				output.WriteLine("dotnet new failed");
				Assert.True(false);
			}

			AddLinkerReference(csproj);

			return csproj;
		}

		[Fact]
		public void RunHelloWorldStandalone()
		{
			string executablePath = BuildAndLink(csproj, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		[Fact]
		public void RunHelloWorldPortable()
		{
			string target = BuildAndLink(csproj, selfContained: false);
			CheckOutput(target, selfContained: false);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			int ret = RunApp(target, out string commandOutput, selfContained: selfContained);
			Assert.True(ret == 0);
			Assert.True(commandOutput.Contains("Hello World!"));
		}
	}
}
