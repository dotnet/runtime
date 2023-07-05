using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class SerializationTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Serialization";

		[Fact]
		public Task CanDisableSerializationDiscovery ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DataContractJsonSerialization ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DataContractSerialization ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task DataContractSerializationUnused ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SerializationTypeRecursion ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task XmlSerialization ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task XmlSerializationUnused ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}