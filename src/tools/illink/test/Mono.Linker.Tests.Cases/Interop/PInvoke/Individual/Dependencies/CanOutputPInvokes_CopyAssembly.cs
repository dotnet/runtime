using System.Runtime.InteropServices;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Individual.Dependencies
{
	public class CanOutputPInvokes_CopyAssembly
	{
		public CanOutputPInvokes_CopyAssembly ()
		{
			var foo = FooEntryPoint ();
			var bar = CustomEntryPoint ();
		}

		[DllImport ("lib_copyassembly")]
		private static extern CanOutputPInvokes_CopyAssembly FooEntryPoint ();

		[DllImport ("lib_copyassembly", EntryPoint = "CustomEntryPoint")]
		private static extern CanOutputPInvokes_CopyAssembly CustomEntryPoint ();
	}
}