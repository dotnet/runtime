namespace Mono.Linker.Tests.Cases.LinkXml.Dependencies.EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod
{
	public class Library2 : Base
	{
		public override void VirtualMethodFromBase ()
		{
		}
	}
}