// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkerAttributeRemoval.xml")]
	[IgnoreLinkAttributes (false)]

	[SetupCompileBefore ("attribute.dll", new[] { "Dependencies/LinkerAttributeRemovalAttributeToRemove.cs" })]
	[SetupCompileBefore ("copyattribute.dll", new[] { "Dependencies/LinkerAttributeRemovalAttributeFromCopyAssembly.cs" })]
	[SetupLinkerAction ("copy", "copyattribute")]
#if !NETCOREAPP
	[Reference ("System.dll")]
	[SetupCompileBefore ("copyassembly.dll", new[] { "Dependencies/LinkerAttributeRemovalCopyAssembly.cs" }, references: new[] { "System.dll", "attribute.dll" })]
#else
	[SetupCompileBefore ("copyassembly.dll", new[] { "Dependencies/LinkerAttributeRemovalCopyAssembly.cs" }, references: new[] { "attribute.dll" })]
#endif
	[SetupLinkerAction ("copy", "copyassembly")]

	[SetupCompileBefore (
		"LinkerAttributeRemovalEmbeddedAndLazyLoad.dll",
		new[] { "Dependencies/LinkerAttributeRemovalEmbeddedAndLazyLoad.cs" })]

	// The test here is that the TypeOnCopyAssemblyWithAttributeUsage has an attribute TestAttributeUsedFromCopyAssemblyAttribute
	// which is marked for removal by the LinkerAttributeRemoval.xml. Normally that would mean that the attribute usage
	// as well as the type (and assembly since it's the only type in it) would be removed as there are no other usages of the attribute type.
	// But because the copyassembly is linked with "copy" action, the attribute usage should not be removed and thus the attribute
	// should be kept.
	[KeptAssembly ("copyassembly.dll")]
	[KeptAssembly ("attribute.dll")]
	[KeptTypeInAssembly ("attribute.dll", typeof (TestAttributeUsedFromCopyAssemblyAttribute))]
	[KeptTypeInAssembly ("attribute.dll", typeof (TestAnotherAttributeUsedFromCopyAssemblyAttribute))]
	[KeptTypeInAssembly ("attribute.dll", typeof (TestAttributeReferencedAsTypeFromCopyAssemblyAttribute))]
	[KeptTypeInAssembly ("copyassembly.dll", typeof (TypeOnCopyAssemblyWithAttributeUsage))]
	[KeptAttributeInAssembly ("copyassembly.dll", typeof (TestAttributeUsedFromCopyAssemblyAttribute), typeof (TypeOnCopyAssemblyWithAttributeUsage))]
	[KeptAttributeInAssembly ("copyassembly.dll", typeof (EditorBrowsableAttribute), typeof (TypeOnCopyAssemblyWithAttributeUsage))]
	[KeptAttributeInAssembly ("copyassembly.dll", typeof (TestAnotherAttributeUsedFromCopyAssemblyAttribute))]

	[KeptAssembly ("copyattribute.dll")]
	[KeptTypeInAssembly ("copyattribute.dll", typeof (AttributeFromCopyAssemblyAttribute))]

	[KeptAssembly ("LinkerAttributeRemovalEmbeddedAndLazyLoad.dll")]
	[KeptTypeInAssembly ("LinkerAttributeRemovalEmbeddedAndLazyLoad.dll", typeof (TypeWithEmbeddedAttributeToBeRemoved))]
	// This needs to fixed with lazy loading assembly refactoring - currently the assembly="*" only applies to assemblies in initial closure
	// The attribute should be removed and not kept as it is now
	// [RemovedAttributeInAssembly ("LinkerAttributeRemovalEmbeddedAndLazyLoad.dll", typeof (EmbeddedAttributeToBeRemoved), typeof (TypeWithEmbeddedAttributeToBeRemoved))]
	[KeptAttributeInAssembly ("LinkerAttributeRemovalEmbeddedAndLazyLoad", typeof (EmbeddedAttributeToBeRemoved), typeof (TypeWithEmbeddedAttributeToBeRemoved))]

	[LogContains ("IL2045: Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TypeOnCopyAssemblyWithAttributeUsage.TypeOnCopyAssemblyWithAttributeUsage(): Attribute 'Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TestAttributeReferencedAsTypeFromCopyAssemblyAttribute'")]
	[LogDoesNotContain ("IL2045")] // No other 2045 messages should be logged

	[ExpectedWarning ("IL2049", "'InvalidInternal'", FileName = "LinkerAttributeRemoval.xml")]
	[ExpectedWarning ("IL2048", "RemoveAttributeInstances", FileName = "LinkerAttributeRemoval.xml")]

	[ExpectedNoWarnings]

	[KeptMember (".ctor()")]
	class LinkerAttributeRemoval
	{
		public static void Main ()
		{
			var instance = new LinkerAttributeRemoval ();
			instance._fieldWithCustomAttribute = null;
			string value = instance.methodWithCustomAttribute (null);
			TestType ();

			_ = new TypeOnCopyAssemblyWithAttributeUsage ();
			TestAttributeUsageRemovedEvenIfAttributeIsKeptForOtherReasons ();

			TestAttributeFromCopyAssembly ();

			TestEmbeddedAttributeLazyLoadRemoved ();

			TestMarkAllRemove ();
		}

#pragma warning disable CS0414
		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type _fieldWithCustomAttribute;
#pragma warning restore CS0414

		[Kept]
		[KeptAttributeAttribute (typeof (TestDontRemoveAttribute))]
		[TestDontRemoveAttribute]
		[TestRemoveAttribute]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private string methodWithCustomAttribute ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] string parameterWithCustomAttribute)
		{
			return null;
		}

		[ExpectedWarning ("IL2045", "Attribute 'System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute'")]
		[Kept]
		public static void TestType ()
		{
			const string reflectionTypeKeptString = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}

		[Kept]
		[TestAttributeUsedFromCopyAssembly (TestAttributeUsedFromCopyAssemblyEnum.None)]
		static void TestAttributeUsageRemovedEvenIfAttributeIsKeptForOtherReasons ()
		{
		}

		[Kept]
		[AttributeFromCopyAssembly]
		static void TestAttributeFromCopyAssembly ()
		{
		}

		[Kept]
		// This attribute should be removed once the assembly lazy loading is implemented
		[DynamicDependency (
			DynamicallyAccessedMemberTypes.PublicConstructors,
			"Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TypeWithEmbeddedAttributeToBeRemoved",
			"LinkerAttributeRemovalEmbeddedAndLazyLoad")]
		static void TestEmbeddedAttributeLazyLoadRemoved ()
		{
			// This needs the DynamicDependency above otherwise linker will not load the assembly at all
			Activator.CreateInstance (Type.GetType ("Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TypeWithEmbeddedAttributeToBeRemoved, LinkerAttributeRemovalEmbeddedAndLazyLoad"));
		}

		[ExpectedWarning ("IL2045")]
		[Kept]
		static void TestMarkAllRemove ()
		{
			Type.GetType ("Mono.Linker.Tests.Cases.LinkAttributes.TestMarkAllRemoveAttribute").RequiresAll ();
		}
	}

	[KeptBaseType (typeof (System.Attribute))]
	class TestDontRemoveAttribute : Attribute
	{
		[Kept]
		public TestDontRemoveAttribute ()
		{
		}
	}

	class TestRemoveAttribute : Attribute
	{
		public TestRemoveAttribute ()
		{
		}
	}

	[KeptBaseType (typeof (System.Attribute))]
	[KeptMember (".ctor()")]
	class TestMarkAllRemoveAttribute : Attribute
	{
	}
}
