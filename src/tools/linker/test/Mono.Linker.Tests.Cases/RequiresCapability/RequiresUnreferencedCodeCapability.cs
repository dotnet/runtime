// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copyused", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresUnreferencedCodeInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkAttributesFile ("RequiresUnreferencedCodeCapability.attributes.xml")]
	[SkipKeptItemsValidation]
	// Annotated members on a copied assembly should not produce any warnings
	// unless directly called or referenced through reflection.
	[LogDoesNotContain ("--UncalledMethod--")]
	[LogDoesNotContain ("--getter UnusedProperty--")]
	[LogDoesNotContain ("--setter UnusedProperty--")]
	[LogDoesNotContain ("--UnusedBaseTypeCctor--")]
	[LogDoesNotContain ("--UnusedVirtualMethod1--")]
	[LogDoesNotContain ("--UnusedVirtualMethod2--")]
	[LogDoesNotContain ("--IUnusedInterface.UnusedMethod--")]
	[LogDoesNotContain ("--UnusedImplementationClass.UnusedMethod--")]
	public class RequiresUnreferencedCodeCapability
	{
		[ExpectedWarning ("IL2026", "--IDerivedInterface.MethodInDerivedInterface--", GlobalAnalysisOnly = true)]
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			TestRequiresSuppressesWarningsFromReflectionAnalysis ();
			TestDuplicateRequiresAttribute ();
			TestRequiresUnreferencedCodeOnlyThroughReflection ();
			TestBaseTypeVirtualMethodRequiresUnreferencedCode ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCodeOnBase ();
			TestTypeWhichOverridesVirtualPropertyRequiresUnreferencedCode ();
			TestStaticCctorRequiresUnreferencedCode ();
			TestStaticCtorMarkingIsTriggeredByFieldAccess ();
			TestStaticCtorTriggeredByMethodCall ();
			TestTypeIsBeforeFieldInit ();
			TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (typeof (DynamicallyAccessedTypeWithRequiresUnreferencedCode));
			TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (typeof (TypeWhichOverridesMethod));
			TestInterfaceMethodWithRequiresUnreferencedCode ();
			TestCovariantReturnCallOnDerived ();
			TestRequiresInMethodFromCopiedAssembly ();
			TestRequiresThroughReflectionInMethodFromCopiedAssembly ();
			TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (typeof (RequiresUnreferencedCodeInCopyAssembly.IDerivedInterface));
			TestRequiresInDynamicDependency ();
			TestThatTrailingPeriodIsAddedToMessage ();
			TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ();
			TestRequiresOnAttributeOnGenericParameter ();
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageOnly--.")]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl")]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --ConstructorRequires--.")]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026", "Message for --setter PropertyRequires--.")]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			set { }
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresAndCallsOtherRequiresMethods--.")]
		static void TestRequiresSuppressesWarningsFromReflectionAnalysis ()
		{
			RequiresAndCallsOtherRequiresMethods<TestType> ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresAndCallsOtherRequiresMethods--")]
		[LogDoesNotContain ("Message for --RequiresUnreferencedCodeMethod--")]
		[RecognizedReflectionAccessPattern]
		static void RequiresAndCallsOtherRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
		{
			// Normally this would warn, but with the attribute on this method it should be auto-suppressed
			RequiresUnreferencedCodeMethod ();

			// Normally this would warn due to incompatible annotations, but with the attribute on this method it should be auto-suppressed
			GetTypeWithPublicMethods ().RequiresPublicFields ();

			TypeRequiresPublicFields<TPublicMethods>.Method ();

			MethodRequiresPublicFields<TPublicMethods> ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeMethod--")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetTypeWithPublicMethods ()
		{
			return null;
		}

		class TypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public static void Method () { }
		}

		static void MethodRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> () { }

		class TestType { }

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
		}

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute))]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void RequiresUnreferencedCodeOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void TestRequiresUnreferencedCodeOnlyThroughReflection ()
		{
			typeof (RequiresUnreferencedCodeCapability)
				.GetMethod (nameof (RequiresUnreferencedCodeOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class BaseType
		{
			[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequiresUnreferencedCode--")]
			public virtual void VirtualMethodRequiresUnreferencedCode ()
			{
			}
		}

		class TypeWhichOverridesMethod : BaseType
		{
			[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequiresUnreferencedCode--")]
			public override void VirtualMethodRequiresUnreferencedCode ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--")]
		static void TestBaseTypeVirtualMethodRequiresUnreferencedCode ()
		{
			var tmp = new BaseType ();
			tmp.VirtualMethodRequiresUnreferencedCode ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequiresUnreferencedCode")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--")]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode ()
		{
			var tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequiresUnreferencedCode ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequiresUnreferencedCode")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--")]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCodeOnBase ()
		{
			BaseType tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequiresUnreferencedCode ();
		}

		class PropertyBaseType
		{
			public virtual int VirtualPropertyRequiresUnreferencedCode { [RequiresUnreferencedCode ("Message for --PropertyBaseType.VirtualPropertyRequiresUnreferencedCode--")] get; }
		}

		class TypeWhichOverridesProperty : PropertyBaseType
		{
			public override int VirtualPropertyRequiresUnreferencedCode {
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequiresUnreferencedCode--")]
				get { return 1; }
			}
		}

		[LogDoesNotContain ("TypeWhichOverridesProperty.VirtualPropertyRequiresUnreferencedCode")]
		[ExpectedWarning ("IL2026", "--PropertyBaseType.VirtualPropertyRequiresUnreferencedCode--")]
		static void TestTypeWhichOverridesVirtualPropertyRequiresUnreferencedCode ()
		{
			var tmp = new TypeWhichOverridesProperty ();
			_ = tmp.VirtualPropertyRequiresUnreferencedCode;
		}

		class StaticCtor
		{
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--TestStaticCtor--")]
		static void TestStaticCctorRequiresUnreferencedCode ()
		{
			_ = new StaticCtor ();
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess.Cctor--")]
			static StaticCtorTriggeredByFieldAccess ()
			{
				field = 0;
			}

			public static int field;
		}

		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByFieldAccess.Cctor--")]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccess ()
		{
			var x = StaticCtorTriggeredByFieldAccess.field + 1;
		}

		class TypeIsBeforeFieldInit
		{
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		[LogContains ("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.TypeIsBeforeFieldInit..cctor():" +
			" Using method 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.TypeIsBeforeFieldInit.AnnotatedMethod()'" +
			" which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code." +
			" Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.")]
		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}

		class StaticCtorTriggeredByMethodCall
		{
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			static StaticCtorTriggeredByMethodCall ()
			{
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			public void TriggerStaticCtorMarking ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.Cctor--")]
		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		public class DynamicallyAccessedTypeWithRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode--")]
			public void RequiresUnreferencedCode ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequiresUnreferencedCode (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[LogDoesNotContain ("ImplementationClass.RequiresUnreferencedCodeMethod")]
		[ExpectedWarning ("IL2026", "--IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
		static void TestInterfaceMethodWithRequiresUnreferencedCode ()
		{
			IRequiresUnreferencedCode inst = new ImplementationClass ();
			inst.RequiresUnreferencedCodeMethod ();
		}

		class BaseReturnType { }
		class DerivedReturnType : BaseReturnType { }

		interface IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --IRequiresUnreferencedCode.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ();
		}

		class ImplementationClass : IRequiresUnreferencedCode
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresUnreferencedCodeMethod--")]
			public void RequiresUnreferencedCodeMethod ()
			{
			}
		}

		abstract class CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnBase.GetRequiresUnreferencedCode--")]
			public abstract BaseReturnType GetRequiresUnreferencedCode ();
		}

		class CovariantReturnDerived : CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnDerived.GetRequiresUnreferencedCode--")]
			public override DerivedReturnType GetRequiresUnreferencedCode ()
			{
				return null;
			}
		}

		[LogDoesNotContain ("--CovariantReturnBase.GetRequiresUnreferencedCode--")]
		[ExpectedWarning ("IL2026", "--CovariantReturnDerived.GetRequiresUnreferencedCode--")]
		static void TestCovariantReturnCallOnDerived ()
		{
			var tmp = new CovariantReturnDerived ();
			tmp.GetRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2026", "--Method--")]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresUnreferencedCodeInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--")]
		static void TestRequiresThroughReflectionInMethodFromCopiedAssembly ()
		{
			typeof (RequiresUnreferencedCodeInCopyAssembly)
				.GetMethod ("MethodCalledThroughReflection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		static void TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeInDynamicDependency--")]
		static void RequiresUnreferencedCodeInDynamicDependency ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeInDynamicDependency--")]
		[DynamicDependency ("RequiresUnreferencedCodeInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresUnreferencedCodeInDynamicDependency ();
		}

		[RequiresUnreferencedCode ("Linker adds a trailing period to this message")]
		static void WarningMessageWithoutEndingPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker adds a trailing period to this message.")]
		static void TestThatTrailingPeriodIsAddedToMessage ()
		{
			WarningMessageWithoutEndingPeriod ();
		}

		[RequiresUnreferencedCode ("Linker does not add a period to this message.")]
		static void WarningMessageEndsWithPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker does not add a period to this message.")]
		static void TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ()
		{
			WarningMessageEndsWithPeriod ();
		}

		class AttributeWhichRequiresUnreferencedCodeAttribute : Attribute
		{
			[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			public AttributeWhichRequiresUnreferencedCodeAttribute ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
		class GenericTypeWithAttributedParameter<[AttributeWhichRequiresUnreferencedCode] T>
		{
			public static void TestMethod () { }
		}

		static void TestRequiresOnAttributeOnGenericParameter ()
		{
			GenericTypeWithAttributedParameter<int>.TestMethod ();
		}
	}
}