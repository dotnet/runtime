using Mono.Linker.Tests.Cases.Extensibility.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[ExpectedNoWarnings]
	[SetupCompileBefore ("FixAbstractMethods.dll", new[] { "Dependencies/FixAbstractMethods.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupCompileBefore ("InterfaceType.dll", new[] { "Dependencies/InterfaceType.cs" })]
	[SetupCompileAfter ("InterfaceType.dll", new[] { "Dependencies/InterfaceType.cs" }, defines: new[] { "INCLUDE_ABSTRACT_METHOD"})]
	[SetupCompileBefore ("InterfaceImplementation.dll", new[] { "Dependencies/InterfaceImplementation.cs" }, references: new[] { "InterfaceType.dll" })]
	[CreatedMemberInAssembly ("InterfaceImplementation.dll", typeof (InterfaceImplementationInOtherAssembly), "AbstractMethod()")]
	[SetupLinkerArgument ("--custom-step", "FixAbstractMethods,FixAbstractMethods.dll")]

	public class CustomStepCanFixAbstractMethods
	{
		public static void Main ()
		{
			TestReflectionAccessToOtherAssembly ();
			TestReflectionAccess ();
			TestDirectAccess ();
		}

		[Kept]
		static void TestReflectionAccessToOtherAssembly ()
		{
			// Regression test for https://github.com/dotnet/runtime/issues/103987
			// To simulate the issue, the type needs to live in a different assembly than the testcase, and it needs
			// to be created through reflection instead of a direct call to the constructor, otherwise we build the
			// TypeMapInfo cache too early for the custom step.

			// var type = typeof (InterfaceImplementation);
			var type = typeof (InterfaceImplementationInOtherAssembly);
			InterfaceType instance = (InterfaceType) System.Activator.CreateInstance (type);
			InterfaceType.UseInstance (instance);
		}

		[Kept]
		static void TestReflectionAccess ()
		{
			var type = typeof (InterfaceImplementationAccessedViaReflection);
			InterfaceType instance = (InterfaceType) System.Activator.CreateInstance (type);
			InterfaceType.UseInstance (instance);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (InterfaceType))]
		// [CreatedMember ("AbstractMethod()")] // https://github.com/dotnet/runtime/issues/104266
		class InterfaceImplementationAccessedViaReflection : InterfaceType
		{
		}

		[Kept]
		static void TestDirectAccess ()
		{
			InterfaceType instance = new InterfaceImplementation ();
			InterfaceType.UseInstance (instance);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (InterfaceType))]
		// [CreatedMember ("AbstractMethod()")] // https://github.com/dotnet/runtime/issues/104266
		class InterfaceImplementation : InterfaceType
		{
		}
	}
}
