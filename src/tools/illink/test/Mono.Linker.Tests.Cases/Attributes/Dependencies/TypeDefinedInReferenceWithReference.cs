namespace Mono.Linker.Tests.Cases.Attributes.Dependencies
{
	[AttributeDefinedInReference (typeof (TypeDefinedInReference))]
	public class TypeDefinedInReferenceWithReference
	{
		public static void Unused ()
		{
		}
	}
}