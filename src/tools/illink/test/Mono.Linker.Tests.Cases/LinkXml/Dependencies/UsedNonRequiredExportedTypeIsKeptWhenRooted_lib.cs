
namespace Mono.Linker.Tests.Cases.LinkXml
{
	public class UsedNonRequiredExportedTypeIsKeptWhenRooted_Used
	{
		public int field;

		public void Method ()
		{
		}
	}

	public class OtherType
	{
		public int field;

		public void Method ()
		{
		}

		public int Property { get; set; }
	}
}