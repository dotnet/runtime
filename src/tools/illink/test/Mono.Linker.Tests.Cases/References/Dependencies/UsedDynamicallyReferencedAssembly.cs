using System;

namespace Mono.Linker.Tests.Cases.References.Dependencies
{
	public class UsedDynamicallyReferencedAssembly
	{
		public UsedDynamicallyReferencedAssembly ()
		{
			var _ = new UnusedReferencedFromDynamicCopyAssembly ();
			var _2 = Type.GetType ("Mono.Linker.Tests.Cases.References.Dependencies.UnusedDynamicallyReferencedFromDynamicCopyAssembly, unuseddynamiclibraryfromdynamiccopy");
		}
	}
}