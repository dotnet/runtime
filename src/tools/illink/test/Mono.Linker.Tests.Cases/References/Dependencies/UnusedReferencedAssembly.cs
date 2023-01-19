using System;

namespace Mono.Linker.Tests.Cases.References.Dependencies
{
	public class UnusedReferencedAssembly
	{
		public UnusedReferencedAssembly ()
		{
			var _ = new UnusedReferencedFromCopyAssembly ();
			var _2 = Type.GetType ("Mono.Linker.Tests.Cases.References.Dependencies.UnusedDynamicallyReferencedFromCopyAssembly, unuseddynamiclibraryfromcopy");
		}
	}
}