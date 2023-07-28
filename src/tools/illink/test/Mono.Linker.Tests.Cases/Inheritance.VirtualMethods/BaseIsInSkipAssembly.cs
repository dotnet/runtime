using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.VirtualMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/SkipLibrary.cs" })]
	[SetupLinkerAction ("skip", "base")]
	public class BaseIsInSkipAssembly
	{
		[Kept]
		public static void Main ()
		{
			new Instantiated ();
			Type _ = typeof (Uninstantiated);
		}

		[Kept]
		[KeptBaseType (typeof (BaseInSkipAssembly))]
		class Uninstantiated : BaseInSkipAssembly
		{
			public override int Method () => 1;
		}

		[Kept]
		[KeptBaseType (typeof (BaseInSkipAssembly))]
		[KeptMember (".ctor()")]
		class Instantiated : BaseInSkipAssembly
		{
			[Kept]
			public override int Method () => 1;
		}
	}
}