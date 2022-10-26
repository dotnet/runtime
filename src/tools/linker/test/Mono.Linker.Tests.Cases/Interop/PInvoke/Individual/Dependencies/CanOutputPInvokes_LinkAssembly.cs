using System.Runtime.InteropServices;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Individual.Dependencies
{
	public class CanOutputPInvokes_LinkAssembly
	{
		[DllImport ("lib_linkassembly")]
		private static extern CanOutputPInvokes_LinkAssembly UnreachableWhenLinked ();
	}
}