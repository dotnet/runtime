// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresUnreferencedCodeInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkAttributesFile ("RequiresUnreferencedCodeCapability.attributes.xml")]
	[SetupLinkerDescriptorFile ("RequiresUnreferencedCodeCapability.descriptor.xml")]
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
	// [LogDoesNotContain ("UnusedVirtualMethod2")] // https://github.com/dotnet/linker/issues/2106
	// [LogContains ("--RequiresUnreferencedCodeOnlyViaDescriptor--")]  // https://github.com/dotnet/linker/issues/2103
	[ExpectedNoWarnings]
	public class RequiresUnreferencedCodeCapability
	{
		[ExpectedWarning ("IL2026", "--IDerivedInterface.MethodInDerivedInterface--", GlobalAnalysisOnly = true)]
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequiresUnreferencedCode.RequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
		[ExpectedWarning ("IL2026", "--IBaseInterface.MethodInBaseInterface--", GlobalAnalysisOnly = true)]
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			SuppressMethodBodyReferences.Test ();
			SuppressGenericParameters<TestType, TestType>.Test ();
			TestDuplicateRequiresAttribute ();
			TestRequiresUnreferencedCodeOnlyThroughReflection ();
			AccessedThroughReflectionOnGenericType<TestType>.Test ();
			TestBaseTypeVirtualMethodRequiresUnreferencedCode ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCodeOnBase ();
			TestTypeWhichOverridesVirtualPropertyRequiresUnreferencedCode ();
			TestStaticCctorRequiresUnreferencedCode ();
			TestStaticCtorMarkingIsTriggeredByFieldAccess ();
			TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ();
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
			WarnIfRequiresUnreferencedCodeOnStaticConstructor.Test ();
			RequiresOnAttribute.Test ();
			RequiresOnGenerics.Test ();
			CovariantReturnViaLdftn.Test ();
			AccessThroughSpecialAttribute.Test ();
			AccessThroughPInvoke.Test ();
			OnEventMethod.Test ();
			AccessThroughNewConstraint.Test ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameter ();
			AccessThroughNewConstraint.TestNewConstraintOnTypeParameterOfStaticType ();
			AccessThroughLdToken.Test ();
			RequiresOnClass.Test ();
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

		[ExpectedNoWarnings]
		class SuppressMethodBodyReferences
		{
			static Type _unknownType;
			static Type GetUnknownType () => null;

			[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeMethod--")]
			static void RequiresUnreferencedCodeMethod ()
			{
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type _requiresPublicConstructors;

			[RequiresUnreferencedCode ("")]
			static void TestRUCMethod ()
			{
				// Normally this would warn, but with the attribute on this method it should be auto-suppressed
				RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestParameter ()
			{
				_unknownType.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestReturnValue ()
			{
				GetUnknownType ().RequiresPublicEvents ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestField ()
			{
				_requiresPublicConstructors = _unknownType;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestRUCMethod ();
				TestParameter ();
				TestReturnValue ();
				TestField ();
			}
		}

		[ExpectedNoWarnings]
		class SuppressGenericParameters<TUnknown, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TPublicProperties>
		{
			static Type _unknownType;

			static void GenericMethodRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () { }

			class GenericTypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> { }

			[RequiresUnreferencedCode ("")]
			static void TestGenericMethod ()
			{
				GenericMethodRequiresPublicMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestGenericMethodMismatch ()
			{
				GenericMethodRequiresPublicMethods<TPublicProperties> ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestGenericType ()
			{
				new GenericTypeRequiresPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			static void TestMakeGenericTypeWithStaticTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (typeof (TUnknown));
			}

			[RequiresUnreferencedCode ("")]
			static void TestMakeGenericTypeWithDynamicTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (_unknownType);
			}

			[RequiresUnreferencedCode ("")]
			static void TestMakeGenericMethod ()
			{
				typeof (SuppressGenericParameters<TUnknown, TPublicProperties>)
					.GetMethod ("GenericMethodRequiresPublicMethods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.MakeGenericMethod (typeof (TPublicProperties));
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestGenericMethod ();
				TestGenericMethodMismatch ();
				TestGenericType ();
				TestMakeGenericTypeWithStaticTypes ();
				TestMakeGenericTypeWithDynamicTypes ();
				TestMakeGenericMethod ();
			}
		}

		class TestType { }

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
		}

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), GlobalAnalysisOnly = true)]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyThroughReflection--")]
		static void RequiresUnreferencedCodeOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeOnlyThroughReflection--", GlobalAnalysisOnly = true)]
		static void TestRequiresUnreferencedCodeOnlyThroughReflection ()
		{
			typeof (RequiresUnreferencedCodeCapability)
				.GetMethod (nameof (RequiresUnreferencedCodeOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class AccessedThroughReflectionOnGenericType<T>
		{
			[RequiresUnreferencedCode ("Message for --GenericType.RequiresUnreferencedCodeOnlyThroughReflection--")]
			public static void RequiresUnreferencedCodeOnlyThroughReflection ()
			{
			}

			[ExpectedWarning ("IL2026", "--GenericType.RequiresUnreferencedCodeOnlyThroughReflection--", GlobalAnalysisOnly = true)]
			public static void Test ()
			{
				typeof (AccessedThroughReflectionOnGenericType<T>)
					.GetMethod (nameof (RequiresUnreferencedCodeOnlyThroughReflection))
					.Invoke (null, new object[0]);
			}
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
			[ExpectedWarning ("IL2116", "StaticCtor..cctor()")]
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		static void TestStaticCctorRequiresUnreferencedCode ()
		{
			_ = new StaticCtor ();
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByFieldAccess..cctor()")]
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess.Cctor--")]
			static StaticCtorTriggeredByFieldAccess ()
			{
				field = 0;
			}

			public static int field;
		}

		static void TestStaticCtorMarkingIsTriggeredByFieldAccess ()
		{
			var x = StaticCtorTriggeredByFieldAccess.field + 1;
		}

		struct StaticCCtorForFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCCtorForFieldAccess..cctor()")]
			[RequiresUnreferencedCode ("Message for --StaticCCtorForFieldAccess.cctor--")]
			static StaticCCtorForFieldAccess () { }

			public static int field;
		}

		static void TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ()
		{
			StaticCCtorForFieldAccess.field = 0;
		}

		class TypeIsBeforeFieldInit
		{
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		[LogContains ("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.TypeIsBeforeFieldInit..cctor():" +
			" Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.TypeIsBeforeFieldInit.AnnotatedMethod()'" +
			" which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code." +
			" Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.")]
		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}

		class StaticCtorTriggeredByMethodCall
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByMethodCall..cctor()")]
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			static StaticCtorTriggeredByMethodCall ()
			{
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			public void TriggerStaticCtorMarking ()
			{
			}
		}

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

		// https://github.com/dotnet/linker/issues/2107
		// Doesn't work in the analyzer because the test infra for analyzer will not build the second assembly
		// and provide it as a ref assembly to the compilation - so the analyzer actually sees the below
		// as errors (missing assembly).
		[ExpectedWarning ("IL2026", "--Method--", GlobalAnalysisOnly = true)]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresUnreferencedCodeInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--", GlobalAnalysisOnly = true)]
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

		class WarnIfRequiresUnreferencedCodeOnStaticConstructor
		{
			class ClassWithRequiresUnreferencedCodeOnStaticConstructor
			{
				[ExpectedWarning ("IL2116")]
				[RequiresUnreferencedCode ("This attribute shouldn't be allowed")]
				static ClassWithRequiresUnreferencedCodeOnStaticConstructor () { }
			}

			public static void Test ()
			{
				typeof (ClassWithRequiresUnreferencedCodeOnStaticConstructor).RequiresNonPublicConstructors ();
			}
		}

		[ExpectedNoWarnings]
		class RequiresOnAttribute
		{
			class AttributeWhichRequiresUnreferencedCodeAttribute : Attribute
			{
				[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
				public AttributeWhichRequiresUnreferencedCodeAttribute ()
				{
				}
			}

			class AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute : Attribute
			{
				public AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute ()
				{
				}

				public bool PropertyWhichRequires {
					get => false;

					[RequiresUnreferencedCode ("--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
					set { }
				}
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			class GenericTypeWithAttributedParameter<[AttributeWhichRequiresUnreferencedCode] T>
			{
				public static void TestMethod () { }
			}

			// https://github.com/dotnet/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", GlobalAnalysisOnly = true)]
			static void GenericMethodWithAttributedParameter<[AttributeWhichRequiresUnreferencedCode] T> () { }

			static void TestRequiresOnAttributeOnGenericParameter ()
			{
				GenericTypeWithAttributedParameter<int>.TestMethod ();
				GenericMethodWithAttributedParameter<int> ();
			}

			// https://github.com/dotnet/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			class TypeWithAttributeWhichRequires
			{
			}

			// https://github.com/dotnet/linker/issues/2094 - should be supported by the analyzer
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static void MethodWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static int _fieldWithAttributeWhichRequires;

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeAttribute.ctor--")]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresUnreferencedCodeOnPropertyAttribute.PropertyWhichRequires--")]
			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			static bool PropertyWithAttributeWhichRequires { get; set; }

			[AttributeWhichRequiresUnreferencedCode]
			[AttributeWhichRequiresUnreferencedCodeOnProperty (PropertyWhichRequires = true)]
			[RequiresUnreferencedCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void MethodWhichRequiresWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void TestMethodWhichRequiresWithAttributeWhichRequires ()
			{
				MethodWhichRequiresWithAttributeWhichRequires ();
			}

			public static void Test ()
			{
				TestRequiresOnAttributeOnGenericParameter ();
				new TypeWithAttributeWhichRequires ();
				MethodWithAttributeWhichRequires ();
				_fieldWithAttributeWhichRequires = 0;
				PropertyWithAttributeWhichRequires = false;
				TestMethodWhichRequiresWithAttributeWhichRequires ();
			}
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeOnlyViaDescriptor--")]
		static void RequiresUnreferencedCodeOnlyViaDescriptor ()
		{
		}

		class RequiresOnGenerics
		{
			class GenericWithStaticMethod<T>
			{
				[RequiresUnreferencedCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				public static void GenericTypeWithStaticMethodWhichRequires () { }
			}

			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--")]
			public static void GenericTypeWithStaticMethodViaLdftn ()
			{
				var _ = new Action (GenericWithStaticMethod<TestType>.GenericTypeWithStaticMethodWhichRequires);
			}

			public static void Test ()
			{
				GenericTypeWithStaticMethodViaLdftn ();
			}
		}

		class CovariantReturnViaLdftn
		{
			abstract class Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Base.GetRequiresUnreferencedCode--")]
				public abstract BaseReturnType GetRequiresUnreferencedCode ();
			}

			class Derived : Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequiresUnreferencedCode--")]
				public override DerivedReturnType GetRequiresUnreferencedCode ()
				{
					return null;
				}
			}

			[ExpectedWarning ("IL2026", "--CovariantReturnViaLdftn.Derived.GetRequiresUnreferencedCode--")]
			public static void Test ()
			{
				var tmp = new Derived ();
				var _ = new Func<DerivedReturnType> (tmp.GetRequiresUnreferencedCode);
			}
		}

		class AccessThroughSpecialAttribute
		{
			// https://github.com/dotnet/linker/issues/1873
			// [ExpectedWarning ("IL2026", "--DebuggerProxyType.Method--")]
			[DebuggerDisplay ("Some{*}value")]
			class TypeWithDebuggerDisplay
			{
				[RequiresUnreferencedCode ("Message for --DebuggerProxyType.Method--")]
				public void Method ()
				{
				}
			}

			public static void Test ()
			{
				var _ = new TypeWithDebuggerDisplay ();
			}
		}

		class AccessThroughPInvoke
		{
			class PInvokeReturnType
			{
				[RequiresUnreferencedCode ("Message for --PInvokeReturnType.ctor--")]
				public PInvokeReturnType () { }
			}

			// https://github.com/dotnet/linker/issues/2116
			[ExpectedWarning ("IL2026", "--PInvokeReturnType.ctor--", GlobalAnalysisOnly = true)]
			[DllImport ("nonexistent")]
			static extern PInvokeReturnType PInvokeReturnsType ();

			// Analyzer doesn't support IL2050 yet
			[ExpectedWarning ("IL2050", GlobalAnalysisOnly = true)]
			public static void Test ()
			{
				PInvokeReturnsType ();
			}
		}

		class OnEventMethod
		{
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--")]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				add { }
				remove { }
			}

			public static void Test ()
			{
				EventToTestRemove += (sender, e) => { };
				EventToTestAdd -= (sender, e) => { };
			}
		}

		class AccessThroughNewConstraint
		{
			class NewConstraintTestType
			{
				[RequiresUnreferencedCode ("Message for --NewConstraintTestType.ctor--")]
				public NewConstraintTestType () { }
			}

			static void GenericMethod<T> () where T : new() { }

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			public static void Test ()
			{
				GenericMethod<NewConstraintTestType> ();
			}

			static class NewConstraintOnTypeParameterOfStaticType<T> where T : new()
			{
				public static void DoNothing () { }
			}

			class NewConstaintOnTypeParameter<T> where T : new()
			{
			}

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			public static void TestNewConstraintOnTypeParameter ()
			{
				_ = new NewConstaintOnTypeParameter<NewConstraintTestType> ();
			}

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			public static void TestNewConstraintOnTypeParameterOfStaticType ()
			{
				NewConstraintOnTypeParameterOfStaticType<NewConstraintTestType>.DoNothing ();
			}
		}

		class AccessThroughLdToken
		{
			static bool PropertyWithLdToken {
				[RequiresUnreferencedCode ("Message for --PropertyWithLdToken.get--")]
				get {
					return false;
				}
			}

			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			public static void Test ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}
		}

		class RequiresOnClass
		{
			[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
			class ClassWithRequiresUnreferencedCode
			{
				public static object Instance;

				public ClassWithRequiresUnreferencedCode () { }

				public static void StaticMethod () { }

				public void NonStaticMethod () { }

				// RequiresOnMethod.MethodWithRUC generates a warning that gets suppressed because the declaring type has RUC
				public static void CallRUCMethod () => RequiresOnMethod.MethodWithRUC ();

				public class NestedClass
				{
					public static void NestedStaticMethod () { }

					// This warning doesn't get suppressed since the declaring type NestedClass is not annotated with RequiresUnreferencedCode
					[ExpectedWarning ("IL2026", "RequiresOnClass.RequiresOnMethod.MethodWithRUC()", "MethodWithRUC")]
					public static void CallRUCMethod () => RequiresOnMethod.MethodWithRUC ();
				}

				// RequiresUnfereferencedCode on the type will suppress IL2072
				static ClassWithRequiresUnreferencedCode ()
				{
					Instance = Activator.CreateInstance (Type.GetType ("SomeText"));
				}

				public static void TestSuppressions (Type[] types)
				{
					// StaticMethod is a static method on a RUC annotated type, so it should warn. But RequiresUnreferencedCode in the
					// class suppresses other RequiresUnreferencedCode messages
					StaticMethod ();

					var nested = new NestedClass ();

					// RequiresUnreferencedCode in the class suppresses DynamicallyAccessedMembers messages
					types[1].GetMethods ();

					void LocalFunction (int a) { }
					LocalFunction (2);
				}
			}

			class RequiresOnMethod
			{
				[RequiresUnreferencedCode ("MethodWithRUC")]
				public static void MethodWithRUC () { }
			}

			[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithoutRequires", "RequiresOnClass.ClassWithRequiresUnreferencedCode", "--ClassWithRequiresUnreferencedCode--")]
			private class DerivedWithoutRequires : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}

				public static void ShouldntWarn (object objectToCast)
				{
					_ = typeof (ClassWithRequiresUnreferencedCode);
					var type = (ClassWithRequiresUnreferencedCode) objectToCast;
				}
			}

			// In order to generate IL2109 the nested class would also need to be annotated with RequiresUnreferencedCode
			// otherwise we threat the nested class as safe
			private class DerivedWithoutRequires2 : ClassWithRequiresUnreferencedCode.NestedClass
			{
				public static void StaticMethod () { }
			}

			[UnconditionalSuppressMessage ("trim", "IL2109")]
			class TestUnconditionalSuppressMessage : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInTestSuppressionClass () { }
			}

			class ClassWithoutRequiresUnreferencedCode
			{
				public ClassWithoutRequiresUnreferencedCode () { }

				public static void StaticMethod () { }

				public void NonStaticMethod () { }

				public class NestedClass
				{
					public static void NestedStaticMethod () { }
				}
			}

			[RequiresUnreferencedCode ("Message for --StaticCtor--")]
			class StaticCtor
			{
				static StaticCtor ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.StaticCtor.StaticCtor()", "Message for --StaticCtor--", GlobalAnalysisOnly = true)]
			static void TestStaticCctorRequiresUnreferencedCode ()
			{
				_ = new StaticCtor ();
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess--")]
			class StaticCtorTriggeredByFieldAccess
			{
				static StaticCtorTriggeredByFieldAccess ()
				{
					field = 0;
				}

				public static int field;
			}

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--", GlobalAnalysisOnly = true)]
			static void TestStaticCtorMarkingIsTriggeredByFieldAccessWrite ()
			{
				StaticCtorTriggeredByFieldAccess.field = 1;
			}

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--", GlobalAnalysisOnly = true)]
			static void TestStaticCtorMarkingTriggeredOnSecondAccessWrite ()
			{
				StaticCtorTriggeredByFieldAccess.field = 2;
			}

			[RequiresUnreferencedCode ("--TestStaticRUCFieldAccessSuppressedByRUCOnMethod_Inner--")]
			static void TestStaticRUCFieldAccessSuppressedByRUCOnMethod_Inner ()
			{
				StaticCtorTriggeredByFieldAccess.field = 3;
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			static void TestStaticRUCFieldAccessSuppressedByRUCOnMethod ()
			{
				TestStaticRUCFieldAccessSuppressedByRUCOnMethod_Inner ();
			}

			[RequiresUnreferencedCode ("Message for --StaticCCtorTriggeredByFieldAccessRead--")]
			class StaticCCtorTriggeredByFieldAccessRead
			{
				public static int field = 42;
			}

			[ExpectedWarning ("IL2026", "StaticCCtorTriggeredByFieldAccessRead.field", "Message for --StaticCCtorTriggeredByFieldAccessRead--", GlobalAnalysisOnly = true)]
			static void TestStaticCtorMarkingIsTriggeredByFieldAccessRead ()
			{
				var _ = StaticCCtorTriggeredByFieldAccessRead.field;
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByCtorCalls--")]
			class StaticCtorTriggeredByCtorCalls
			{
				static StaticCtorTriggeredByCtorCalls ()
				{
				}

				public void TriggerStaticCtorMarking ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByCtorCalls.StaticCtorTriggeredByCtorCalls()", GlobalAnalysisOnly = true)]
			static void TestStaticCtorTriggeredByCtorCall ()
			{
				new StaticCtorTriggeredByCtorCalls ();
			}

			[RequiresUnreferencedCode ("Message for --ClassWithInstanceField--")]
			class ClassWithInstanceField
			{
				public int field = 42;
			}

			[ExpectedWarning ("IL2026", "ClassWithInstanceField.ClassWithInstanceField()", GlobalAnalysisOnly = true)]
			static void TestInstanceFieldCallDontWarn ()
			{
				ClassWithInstanceField instance = new ClassWithInstanceField ();
				var _ = instance.field;
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall2--")]
			class StaticCtorTriggeredByMethodCall2
			{
				static StaticCtorTriggeredByMethodCall2 ()
				{
				}

				public void TriggerStaticCtorMarking ()
				{
				}
			}

			static void TestNullInstanceTryingToCallMethod ()
			{
				StaticCtorTriggeredByMethodCall2 instance = null;
				instance.TriggerStaticCtorMarking ();
			}

			[RequiresUnreferencedCode ("Message for --DerivedWithRequires--")]
			private class DerivedWithRequires : ClassWithoutRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}
			}

			[RequiresUnreferencedCode ("Message for --DerivedWithRequires2--")]
			private class DerivedWithRequires2 : ClassWithRequiresUnreferencedCode
			{
				public static void StaticMethodInInheritedClass () { }

				// A nested class is not considered a static method nor constructor therefore RequiresUnreferencedCode doesnt apply
				// and this warning is not suppressed
				[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithRequires2/DerivedNestedClass", "--ClassWithRequiresUnreferencedCode--")]
				public class DerivedNestedClass : ClassWithRequiresUnreferencedCode
				{
					public static void NestedStaticMethod () { }
				}
			}

			class BaseWithoutRequiresOnType
			{
				[RequiresUnreferencedCode ("RUC")]
				public virtual void Method () { }
			}

			[RequiresUnreferencedCode ("RUC")]
			class DerivedWithRequiresOnType : BaseWithoutRequiresOnType
			{
				public override void Method () { }
			}

			[RequiresUnreferencedCode ("RUC")]
			class BaseWithRequiresOnType
			{
				public virtual void Method () { }
			}

			[RequiresUnreferencedCode ("RUC")]
			class DerivedWithoutRequiresOnType : BaseWithRequiresOnType
			{
				public override void Method () { }
			}

			public interface InterfaceWithoutRequires
			{
				[RequiresUnreferencedCode ("RUC")]
				static int Method ()
				{
					return 0;
				}

				[RequiresUnreferencedCode ("RUC")]
				int Method (int a);
			}

			[RequiresUnreferencedCode ("RUC")]
			class ImplementationWithRequiresOnType : InterfaceWithoutRequires
			{
				public static int Method ()
				{
					return 1;
				}

				public int Method (int a)
				{
					return a;
				}
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			static void TestRequiresInClassAccessedByStaticMethod ()
			{
				ClassWithRequiresUnreferencedCode.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			static void TestRequiresInClassAccessedByCctor ()
			{
				var classObject = new ClassWithRequiresUnreferencedCode ();
			}

			static void TestRequiresInParentClassAccesedByStaticMethod ()
			{
				ClassWithRequiresUnreferencedCode.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			// Although we suppress the warning from RequiresOnMethod.MethodWithRUC () we still get a warning because we call CallRUCMethod() which is an static method on a type with RUC
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.CallRUCMethod()", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "ClassWithRequiresUnreferencedCode.Instance", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			static void TestRequiresOnBaseButNotOnDerived ()
			{
				DerivedWithoutRequires.StaticMethodInInheritedClass ();
				DerivedWithoutRequires.StaticMethod ();
				DerivedWithoutRequires.CallRUCMethod ();
				DerivedWithoutRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.CallRUCMethod ();
				DerivedWithoutRequires.ShouldntWarn (null);
				DerivedWithoutRequires.Instance.ToString ();
				DerivedWithoutRequires2.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires.StaticMethodInInheritedClass()", "--DerivedWithRequires--", GlobalAnalysisOnly = true)]
			static void TestRequiresOnDerivedButNotOnBase ()
			{
				DerivedWithRequires.StaticMethodInInheritedClass ();
				DerivedWithRequires.StaticMethod ();
				DerivedWithRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires2.StaticMethodInInheritedClass()", "--DerivedWithRequires2--", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.StaticMethod()", "--ClassWithRequiresUnreferencedCode--", GlobalAnalysisOnly = true)]
			static void TestRequiresOnBaseAndDerived ()
			{
				DerivedWithRequires2.StaticMethodInInheritedClass ();
				DerivedWithRequires2.StaticMethod ();
				DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires2.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequiresUnreferencedCode.TestSuppressions(Type[])", GlobalAnalysisOnly = true)]
			static void TestSuppressionsOnClass ()
			{
				ClassWithRequiresUnreferencedCode.TestSuppressions (new[] { typeof (ClassWithRequiresUnreferencedCode) });
				TestUnconditionalSuppressMessage.StaticMethodInTestSuppressionClass ();
			}

			[RequiresUnreferencedCode ("--StaticMethodOnRUCTypeSuppressedByRUCOnMethod--")]
			static void StaticMethodOnRUCTypeSuppressedByRUCOnMethod ()
			{
				DerivedWithRequires.StaticMethodInInheritedClass ();
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			static void TestStaticMethodOnRUCTypeSuppressedByRUCOnMethod ()
			{
				StaticMethodOnRUCTypeSuppressedByRUCOnMethod ();
			}

			static void TestStaticConstructorCalls ()
			{
				TestStaticCctorRequiresUnreferencedCode ();
				TestStaticCtorMarkingIsTriggeredByFieldAccessWrite ();
				TestStaticCtorMarkingTriggeredOnSecondAccessWrite ();
				TestStaticRUCFieldAccessSuppressedByRUCOnMethod ();
				TestStaticCtorMarkingIsTriggeredByFieldAccessRead ();
				TestStaticCtorTriggeredByMethodCall ();
				TestStaticCtorTriggeredByCtorCall ();
				TestInstanceFieldCallDontWarn ();
			}

			[RequiresUnreferencedCode ("--MemberTypesWithRUC--")]
			class MemberTypesWithRUC
			{
				public static int field;
				public static int Property { get; set; }

				// These should not be reported https://github.com/dotnet/linker/issues/2218
				[ExpectedWarning ("IL2026", "add_Event", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "remove_Event", GlobalAnalysisOnly = true)]
				public static event EventHandler Event;
			}

			[ExpectedWarning ("IL2026", "MemberTypesWithRUC.field", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRUC.Property.set", GlobalAnalysisOnly = true)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRUC.remove_Event", GlobalAnalysisOnly = true)]
			static void TestOtherMemberTypesWithRUC ()
			{
				MemberTypesWithRUC.field = 1;
				MemberTypesWithRUC.Property = 1;
				MemberTypesWithRUC.Event -= null;
			}

			class ReflectionAccessOnMethod
			{
				// Analyzer still dont understand RUC on type
				[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()", GlobalAnalysisOnly = true)]
				static void TestDAMAccess ()
				{
					// Warns because BaseWithoutRequiresOnType.Method as RUC on the method
					typeof (BaseWithoutRequiresOnType).RequiresPublicMethods ();

					// Doesn't warn because DerivedWithRequiresOnType doesn't have any static methods
					typeof (DerivedWithRequiresOnType).RequiresPublicMethods ();

					// Warns twice since both methods on InterfaceWithoutRequires have RUC on the method
					typeof (InterfaceWithoutRequires).RequiresPublicMethods ();

					// Warns because ImplementationWithRequiresOnType.Method is a static public method on a RUC type
					typeof (ImplementationWithRequiresOnType).RequiresPublicMethods ();

					// Doesn't warn since BaseWithRequiresOnType has no static methods
					typeof (BaseWithRequiresOnType).RequiresPublicMethods ();

					// Doesn't warn since DerivedWithoutRequiresOnType has no static methods
					typeof (DerivedWithoutRequiresOnType).RequiresPublicMethods ();
				}

				[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()", GlobalAnalysisOnly = true)]
				static void TestDirectReflectionAccess ()
				{
					// RUC on the method itself
					typeof (BaseWithoutRequiresOnType).GetMethod (nameof (BaseWithoutRequiresOnType.Method));

					// RUC on the method itself
					typeof (InterfaceWithoutRequires).GetMethod (nameof (InterfaceWithoutRequires.Method));

					// Warns because ImplementationWithRequiresOnType.Method is a static public method on a RUC type
					typeof (ImplementationWithRequiresOnType).GetMethod (nameof (ImplementationWithRequiresOnType.Method));

					// Doesn't warn since Method is not static (so it doesn't matter that the type has RUC)
					typeof (BaseWithRequiresOnType).GetMethod (nameof (BaseWithRequiresOnType.Method));
				}

				public static void Test ()
				{
					TestDAMAccess ();
					TestDirectReflectionAccess ();
				}
			}

			class ReflectionAccessOnCtor
			{
				[RequiresUnreferencedCode ("--BaseWithRUC--")]
				class BaseWithRUC
				{
					public BaseWithRUC () { }
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnCtor/DerivedWithoutRUC", "ReflectionAccessOnCtor.BaseWithRUC", GlobalAnalysisOnly = true)]
				class DerivedWithoutRUC : BaseWithRUC
				{
					[ExpectedWarning ("IL2026", "--BaseWithRUC--")] // The body has direct call to the base.ctor()
					public DerivedWithoutRUC () { }
				}

				[RequiresUnreferencedCode ("--DerivedWithRUCOnBaseWithRUC--")]
				class DerivedWithRUCOnBaseWithRUC : BaseWithRUC
				{
					// No warning - suppressed by the RUC on this type
					private DerivedWithRUCOnBaseWithRUC () { }
				}

				class BaseWithoutRUC { }

				[RequiresUnreferencedCode ("--DerivedWithRUCOnBaseWithout--")]
				class DerivedWithRUCOnBaseWithoutRuc : BaseWithoutRUC
				{
					public DerivedWithRUCOnBaseWithoutRuc () { }
				}

				[ExpectedWarning ("IL2026", "BaseWithRUC.BaseWithRUC()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUCOnBaseWithRUC.DerivedWithRUCOnBaseWithRUC()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUCOnBaseWithoutRuc.DerivedWithRUCOnBaseWithoutRuc()", GlobalAnalysisOnly = true)]
				static void TestDAMAccess ()
				{
					// Warns because the type has RUC
					typeof (BaseWithRUC).RequiresPublicConstructors ();

					// Doesn't warn since there's no RUC on this type
					typeof (DerivedWithoutRUC).RequiresPublicParameterlessConstructor ();

					// Warns - RUC on the type
					typeof (DerivedWithRUCOnBaseWithRUC).RequiresNonPublicConstructors ();

					// Warns - RUC On the type
					typeof (DerivedWithRUCOnBaseWithoutRuc).RequiresPublicConstructors ();
				}

				[ExpectedWarning ("IL2026", "BaseWithRUC.BaseWithRUC()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUCOnBaseWithRUC.DerivedWithRUCOnBaseWithRUC()", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUCOnBaseWithoutRuc.DerivedWithRUCOnBaseWithoutRuc()", GlobalAnalysisOnly = true)]
				static void TestDirectReflectionAccess ()
				{
					typeof (BaseWithRUC).GetConstructor (Type.EmptyTypes);
					typeof (DerivedWithoutRUC).GetConstructor (Type.EmptyTypes);
					typeof (DerivedWithRUCOnBaseWithRUC).GetConstructor (BindingFlags.NonPublic, Type.EmptyTypes);
					typeof (DerivedWithRUCOnBaseWithoutRuc).GetConstructor (Type.EmptyTypes);
				}

				public static void Test ()
				{
					TestDAMAccess ();
					TestDirectReflectionAccess ();
				}
			}

			class ReflectionAccessOnField
			{
				[RequiresUnreferencedCode ("--WithRUC--")]
				class WithRUC
				{
					public int InstanceField;
					public static int StaticField;
					private static int PrivateStaticField;
				}

				[RequiresUnreferencedCode ("--WithRUCOnlyInstanceFields--")]
				class WithRUCOnlyInstanceFields
				{
					public int InstnaceField;
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnField/DerivedWithoutRUC", "ReflectionAccessOnField.WithRUC", GlobalAnalysisOnly = true)]
				class DerivedWithoutRUC : WithRUC
				{
					public static int DerivedStaticField;
				}

				[RequiresUnreferencedCode ("--DerivedWithRUC--")]
				class DerivedWithRUC : WithRUC
				{
					public static int DerivedStaticField;
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticField", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticField", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticField", GlobalAnalysisOnly = true)]
				static void TestDAMAccess ()
				{
					typeof (WithRUC).RequiresPublicFields ();
					typeof (WithRUC).RequiresNonPublicFields ();
					typeof (WithRUCOnlyInstanceFields).RequiresPublicFields ();
					typeof (DerivedWithoutRUC).RequiresPublicFields ();
					typeof (DerivedWithRUC).RequiresPublicFields ();
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticField", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticField", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticField", GlobalAnalysisOnly = true)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRUC).GetField (nameof (WithRUC.StaticField));
					typeof (WithRUC).GetField (nameof (WithRUC.InstanceField)); // Doesn't warn
					typeof (WithRUC).GetField ("PrivateStaticField", BindingFlags.NonPublic);
					typeof (WithRUCOnlyInstanceFields).GetField (nameof (WithRUCOnlyInstanceFields.InstnaceField)); // Doesn't warn
					typeof (DerivedWithoutRUC).GetField (nameof (DerivedWithRUC.DerivedStaticField)); // Doesn't warn
					typeof (DerivedWithRUC).GetField (nameof (DerivedWithRUC.DerivedStaticField));
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticField", GlobalAnalysisOnly = true)]
				[DynamicDependency (nameof (WithRUC.StaticField), typeof (WithRUC))]
				[DynamicDependency (nameof (WithRUC.InstanceField), typeof (WithRUC))] // Doesn't warn
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithoutRUC))] // Doesn't warn
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticField", GlobalAnalysisOnly = true)]
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithRUC))]
				static void TestDynamicDependencyAccess ()
				{
				}

				[RequiresUnreferencedCode ("This class is dangerous")]
				class BaseForDAMAnnotatedClass
				{
					public static int baseField;
				}

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
				[RequiresUnreferencedCode ("This class is dangerous")]
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseField", GlobalAnalysisOnly = true)]
				class DAMAnnotatedClass : BaseForDAMAnnotatedClass
				{
					[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicField", GlobalAnalysisOnly = true)]
					public static int publicField;

					[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privatefield", GlobalAnalysisOnly = true)]
					static int privatefield;
				}

				static void TestDAMOnTypeAccess (DAMAnnotatedClass instance)
				{
					instance.GetType ().GetField ("publicField");
				}

				public static void Test ()
				{
					TestDAMAccess ();
					TestDirectReflectionAccess ();
					TestDynamicDependencyAccess ();
					TestDAMOnTypeAccess (null);
				}
			}

			class ReflectionAccessOnEvents
			{
				// Most of the tests in this run into https://github.com/dotnet/linker/issues/2218
				// So for now keeping just a very simple test

				[RequiresUnreferencedCode ("--WithRUC--")]
				class WithRUC
				{
					// These should be reported only in TestDirectReflectionAccess
					// https://github.com/dotnet/linker/issues/2218
					[ExpectedWarning ("IL2026", "add_StaticEvent", GlobalAnalysisOnly = true)]
					[ExpectedWarning ("IL2026", "remove_StaticEvent", GlobalAnalysisOnly = true)]
					public static event EventHandler StaticEvent;
				}

				[ExpectedWarning ("IL2026", "add_StaticEvent", GlobalAnalysisOnly = true)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRUC).GetEvent (nameof (WithRUC.StaticEvent));
				}

				public static void Test ()
				{
					TestDirectReflectionAccess ();
				}
			}

			class ReflectionAccessOnProperties
			{
				[RequiresUnreferencedCode ("--WithRUC--")]
				class WithRUC
				{
					public int InstanceProperty { get; set; }
					public static int StaticProperty { get; set; }
					private static int PrivateStaticProperty { get; set; }
				}

				[RequiresUnreferencedCode ("--WithRUCOnlyInstanceProperties--")]
				class WithRUCOnlyInstanceProperties
				{
					public int InstnaceProperty { get; set; }
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnProperties/DerivedWithoutRUC", "ReflectionAccessOnProperties.WithRUC", GlobalAnalysisOnly = true)]
				class DerivedWithoutRUC : WithRUC
				{
					public static int DerivedStaticProperty { get; set; }
				}

				[RequiresUnreferencedCode ("--DerivedWithRUC--")]
				class DerivedWithRUC : WithRUC
				{
					public static int DerivedStaticProperty { get; set; }
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.set", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticProperty.set", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.set", GlobalAnalysisOnly = true)]
				static void TestDAMAccess ()
				{
					typeof (WithRUC).RequiresPublicProperties ();
					typeof (WithRUC).RequiresNonPublicProperties ();
					typeof (WithRUCOnlyInstanceProperties).RequiresPublicProperties ();
					typeof (DerivedWithoutRUC).RequiresPublicProperties ();
					typeof (DerivedWithRUC).RequiresPublicProperties ();
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.set", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.PrivateStaticProperty.set", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.set", GlobalAnalysisOnly = true)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRUC).GetProperty (nameof (WithRUC.StaticProperty));
					typeof (WithRUC).GetProperty (nameof (WithRUC.InstanceProperty)); // Doesn't warn
					typeof (WithRUC).GetProperty ("PrivateStaticProperty", BindingFlags.NonPublic);
					typeof (WithRUCOnlyInstanceProperties).GetProperty (nameof (WithRUCOnlyInstanceProperties.InstnaceProperty)); // Doesn't warn
					typeof (DerivedWithoutRUC).GetProperty (nameof (DerivedWithRUC.DerivedStaticProperty)); // Doesn't warn
					typeof (DerivedWithRUC).GetProperty (nameof (DerivedWithRUC.DerivedStaticProperty));
				}

				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "WithRUC.StaticProperty.set", GlobalAnalysisOnly = true)]
				[DynamicDependency (nameof (WithRUC.StaticProperty), typeof (WithRUC))]
				[DynamicDependency (nameof (WithRUC.InstanceProperty), typeof (WithRUC))] // Doesn't warn
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (DerivedWithoutRUC))] // Doesn't warn
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2026", "DerivedWithRUC.DerivedStaticProperty.set", GlobalAnalysisOnly = true)]
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (DerivedWithRUC))]
				static void TestDynamicDependencyAccess ()
				{
				}

				[RequiresUnreferencedCode ("This class is dangerous")]
				class BaseForDAMAnnotatedClass
				{
					public static int baseProperty { get; set; }
				}

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
				[RequiresUnreferencedCode ("This class is dangerous")]
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseProperty.get", GlobalAnalysisOnly = true)]
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseProperty.set", GlobalAnalysisOnly = true)]
				class DAMAnnotatedClass : BaseForDAMAnnotatedClass
				{
					public static int publicProperty {
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicProperty.get", GlobalAnalysisOnly = true)]
						get;
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicProperty.set", GlobalAnalysisOnly = true)]
						set;
					}

					static int privateProperty {
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privateProperty.get", GlobalAnalysisOnly = true)]
						get;
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privateProperty.set", GlobalAnalysisOnly = true)]
						set;
					}
				}

				static void TestDAMOnTypeAccess (DAMAnnotatedClass instance)
				{
					instance.GetType ().GetProperty ("publicProperty");
				}

				public static void Test ()
				{
					TestDAMAccess ();
					TestDirectReflectionAccess ();
					TestDynamicDependencyAccess ();
					TestDAMOnTypeAccess (null);
				}
			}

			[RequiresUnreferencedCode ("The attribute is dangerous")]
			public class AttributeWithRUC : Attribute
			{
				public static int field;

				// `field` cannot be used as named attribute argument because is static, and if accessed via
				// a property the property will be the one generating the warning, but then the warning will 
				// be suppresed by the RequiresUnreferencedCode on the declaring type
				public int PropertyOnAttribute {
					get { return field; }
					set { field = value; }
				}
			}

			[AttributeWithRUC (PropertyOnAttribute = 42)]
			[ExpectedWarning ("IL2026", "AttributeWithRUC.AttributeWithRUC()", GlobalAnalysisOnly = true)]
			static void KeepFieldOnAttribute () { }

			public static void Test ()
			{
				TestRequiresInClassAccessedByStaticMethod ();
				TestRequiresInParentClassAccesedByStaticMethod ();
				TestRequiresInClassAccessedByCctor ();
				TestRequiresOnBaseButNotOnDerived ();
				TestRequiresOnDerivedButNotOnBase ();
				TestRequiresOnBaseAndDerived ();
				TestSuppressionsOnClass ();
				TestStaticMethodOnRUCTypeSuppressedByRUCOnMethod ();
				TestStaticConstructorCalls ();
				TestOtherMemberTypesWithRUC ();
				ReflectionAccessOnMethod.Test ();
				ReflectionAccessOnCtor.Test ();
				ReflectionAccessOnField.Test ();
				ReflectionAccessOnEvents.Test ();
				ReflectionAccessOnProperties.Test ();
				KeepFieldOnAttribute ();
			}
		}
	}
}