using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SkipUnresolved (true)]
	[SetupCompileBefore ("Lib.dll", new[] { "Dependencies/TypeForwardedIsUpdatedForMissingTypeLib.cs" })]
	[SetupCompileBefore ("AnotherLibrary.dll", new[] { "Dependencies/TypeForwardedIsUpdatedForMissingTypeLib2.cs" })]

	[SetupCompileAfter ("AnotherLibrary.dll", new[] { "Dependencies/TypeForwardedIsUpdatedForMissingTypeLib2.cs" })]
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/TypeForwardedIsUpdatedForMissingTypeLib.cs" }, removeFromLinkerInput: true)]
	[SetupCompileAfter ("Lib.dll", new[] { "Dependencies/TypeForwardedIsUpdatedForMissingTypeFwd.cs" }, references: new[] { "Implementation.dll" })]

	public class TypeForwardedIsUpdatedForMissingType
	{
		public static void Main ()
		{
			Test (null);
		}

		// It's important that the assembly reference to AnotherLibrary is added before Lib
		public void DependencyWhichIsRemovedFromAssemblyList (C2 c)
		{
		}

		[Kept]
		static void Test (C1 c)
		{
		}
	}
}
