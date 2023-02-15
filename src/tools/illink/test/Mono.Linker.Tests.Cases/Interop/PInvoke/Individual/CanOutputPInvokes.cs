using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Interop.PInvoke.Individual.Dependencies;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Individual
{
	[SetupLinkerAction ("copy", "copyassembly")]
	[SetupLinkerAction ("link", "linkassembly")]
	// Prevent dumping of pinvokes from core assemblies
	[SetupLinkerTrimMode ("skip")]
	[SetupCompileBefore ("copyassembly.dll", new[] { typeof (CanOutputPInvokes_CopyAssembly) })]
	[SetupCompileBefore ("linkassembly.dll", new[] { typeof (CanOutputPInvokes_LinkAssembly) })]
	[SetupLinkerArgument ("--output-pinvokes", new[] { "pinvokes.json" })]

	public class CanOutputPInvokes
	{
		public static void Main ()
		{
			var foo = FooEntryPoint ();
			var bar = CustomEntryPoint ();
			var baz = CustomEntryPoint0 ();

			var copyAssembly = new CanOutputPInvokes_CopyAssembly ();
		}

		class Foo
		{
			public Foo ()
			{
			}
		}

		[DllImport ("lib")]
		private static extern Foo FooEntryPoint ();

		[DllImport ("lib", EntryPoint = "CustomEntryPoint")]
		private static extern Foo CustomEntryPoint ();

		[DllImport ("lib", EntryPoint = "CustomEntryPoint")]
		private static extern Foo CustomEntryPoint0 ();

		[DllImport ("lib")]
		private static extern Foo UnreachableDllImport ();
	}
}