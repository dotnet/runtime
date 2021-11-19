// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkerAction ("copy", "lib2")]
	[SetupCompileBefore ("lib2.dll", new[] { "Dependencies/ReferenceInterfaces.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib2.dll")]
	[SetupLinkAttributesFile ("RequiresCapability.attributes.xml")]
	[SetupLinkerDescriptorFile ("RequiresCapability.descriptor.xml")]
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
	// [LogContains ("--RequiresOnlyViaDescriptor--")]  // https://github.com/dotnet/linker/issues/2103
	[ExpectedNoWarnings]
	public class RequiresCapability
	{
		[ExpectedWarning ("IL2026", "--IDerivedInterface.MethodInDerivedInterface--", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequires.MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2026", "--IBaseInterface.MethodInBaseInterface--", ProducedBy = ProducedBy.Trimmer)]
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			SuppressMethodBodyReferences.Test ();
			SuppressGenericParameters<TestType, TestType>.Test ();
			TestDuplicateRequiresAttribute ();
			TestRequiresOnlyThroughReflection ();
			AccessedThroughReflectionOnGenericType<TestType>.Test ();
			TestBaseTypeVirtualMethodRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ();
			TestTypeWhichOverridesVirtualPropertyRequires ();
			TestStaticCctorRequires ();
			TestStaticCtorMarkingIsTriggeredByFieldAccess ();
			TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ();
			TestStaticCtorTriggeredByMethodCall ();
			TestTypeIsBeforeFieldInit ();
			TestDynamicallyAccessedMembersWithRequires (typeof (DynamicallyAccessedTypeWithRequires));
			TestDynamicallyAccessedMembersWithRequires (typeof (TypeWhichOverridesMethod));
			TestInterfaceMethodWithRequires ();
			TestCovariantReturnCallOnDerived ();
			TestRequiresInMethodFromCopiedAssembly ();
			TestRequiresThroughReflectionInMethodFromCopiedAssembly ();
			TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (typeof (RequiresInCopyAssembly.IDerivedInterface));
			TestRequiresInDynamicDependency ();
			TestThatTrailingPeriodIsAddedToMessage ();
			TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ();
			WarnIfRequiresOnStaticConstructor.Test ();
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
			AttributeMismatch.Test ();
			RequiresOnClass.Test ();
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageOnly--.")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageOnly--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		[RequiresAssemblyFiles ("Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		[RequiresAssemblyFiles ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --ConstructorRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --ConstructorRequires--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			[RequiresAssemblyFiles ("Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026", "Message for --setter PropertyRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --getter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3002", "Message for --setter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			[RequiresAssemblyFiles ("Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			[RequiresAssemblyFiles ("Message for --setter PropertyRequires--")]
			set { }
		}

		[ExpectedNoWarnings]
		class SuppressMethodBodyReferences
		{
			static Type _unknownType;
			static Type GetUnknownType () => null;

			[RequiresUnreferencedCode ("Message for --MethodWithRequires--")]
			[RequiresAssemblyFiles ("Message for --MethodWithRequires--")]
			static void MethodWithRequires ()
			{
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			static Type _requiresPublicConstructors;

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestMethodWithRequires ()
			{
				// Normally this would warn, but with the attribute on this method it should be auto-suppressed
				MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestParameter ()
			{
				_unknownType.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestReturnValue ()
			{
				GetUnknownType ().RequiresPublicEvents ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestField ()
			{
				_requiresPublicConstructors = _unknownType;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			public static void Test ()
			{
				TestMethodWithRequires ();
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
			[RequiresAssemblyFiles ("")]
			static void TestGenericMethod ()
			{
				GenericMethodRequiresPublicMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestGenericMethodMismatch ()
			{
				GenericMethodRequiresPublicMethods<TPublicProperties> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestGenericType ()
			{
				new GenericTypeRequiresPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestMakeGenericTypeWithStaticTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (typeof (TUnknown));
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestMakeGenericTypeWithDynamicTypes ()
			{
				typeof (GenericTypeRequiresPublicFields<>).MakeGenericType (_unknownType);
			}

			[RequiresUnreferencedCode ("")]
			[RequiresAssemblyFiles ("")]
			static void TestMakeGenericMethod ()
			{
				typeof (SuppressGenericParameters<TUnknown, TPublicProperties>)
					.GetMethod ("GenericMethodRequiresPublicMethods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.MakeGenericMethod (typeof (TPublicProperties));
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
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
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute), ProducedBy = ProducedBy.Trimmer)]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresOnlyThroughReflection--")]
		static void RequiresOnlyThroughReflection ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresOnlyThroughReflection--", ProducedBy = ProducedBy.Trimmer)]
		static void TestRequiresOnlyThroughReflection ()
		{
			typeof (RequiresCapability)
				.GetMethod (nameof (RequiresOnlyThroughReflection), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		class AccessedThroughReflectionOnGenericType<T>
		{
			[RequiresUnreferencedCode ("Message for --GenericType.RequiresOnlyThroughReflection--")]
			public static void RequiresOnlyThroughReflection ()
			{
			}

			[ExpectedWarning ("IL2026", "--GenericType.RequiresOnlyThroughReflection--", ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (AccessedThroughReflectionOnGenericType<T>)
					.GetMethod (nameof (RequiresOnlyThroughReflection))
					.Invoke (null, new object[0]);
			}
		}

		class BaseType
		{
			[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequires--")]
			[RequiresAssemblyFiles ("Message for --BaseType.VirtualMethodRequires--")]
			public virtual void VirtualMethodRequires ()
			{
			}
		}

		class TypeWhichOverridesMethod : BaseType
		{
			[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
			[RequiresAssemblyFiles ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
			public override void VirtualMethodRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestBaseTypeVirtualMethodRequires ()
		{
			var tmp = new BaseType ();
			tmp.VirtualMethodRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequires")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequires ()
		{
			var tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequires")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ()
		{
			BaseType tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequires ();
		}

		class PropertyBaseType
		{
			public virtual int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				get;
			}
		}

		class TypeWhichOverridesProperty : PropertyBaseType
		{
			public override int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				get { return 1; }
			}
		}

		[LogDoesNotContain ("TypeWhichOverridesProperty.VirtualPropertyRequires")]
		[ExpectedWarning ("IL2026", "--PropertyBaseType.VirtualPropertyRequires--")]
		[ExpectedWarning ("IL3002", "--PropertyBaseType.VirtualPropertyRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesVirtualPropertyRequires ()
		{
			var tmp = new TypeWhichOverridesProperty ();
			_ = tmp.VirtualPropertyRequires;
		}

		class StaticCtor
		{
			[ExpectedWarning ("IL2116", "StaticCtor..cctor()", ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		static void TestStaticCctorRequires ()
		{
			_ = new StaticCtor ();
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByFieldAccess..cctor()", ProducedBy = ProducedBy.Trimmer)]
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
			// TODO: Analyzer still allows RUC/RAF on static constructor with no warning
			// https://github.com/dotnet/linker/issues/2347
			[ExpectedWarning ("IL2116", "StaticCCtorForFieldAccess..cctor()", ProducedBy = ProducedBy.Trimmer)]
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
			[ExpectedWarning ("IL2026", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3002", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer)]
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresAssemblyFiles ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		// Linker sees the call to AnnotatedMethod in the static .ctor, but analyzer doesn't see the static .ctor at all
		// since it's fully compiler generated, instead it sees the call on the field initialization itself.
		[LogContains ("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.TypeIsBeforeFieldInit..cctor():" +
			" Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.TypeIsBeforeFieldInit.AnnotatedMethod()'" +
			" which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code." +
			" Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.", ProducedBy = ProducedBy.Trimmer)]
		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}

		class StaticCtorTriggeredByMethodCall
		{
			// TODO: Analyzer still allows RUC/RAF on static constructor with no warning
			// https://github.com/dotnet/linker/issues/2347
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByMethodCall..cctor()", ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			[RequiresAssemblyFiles ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			static StaticCtorTriggeredByMethodCall ()
			{
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			[RequiresAssemblyFiles ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			public void TriggerStaticCtorMarking ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		public class DynamicallyAccessedTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
			public void MethodWithRequires ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequires (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[LogDoesNotContain ("ImplementationClass.MethodWithRequires")]
		[ExpectedWarning ("IL2026", "--IRequires.MethodWithRequires--")]
		[ExpectedWarning ("IL3002", "--IRequires.MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestInterfaceMethodWithRequires ()
		{
			IRequires inst = new ImplementationClass ();
			inst.MethodWithRequires ();
		}

		class BaseReturnType { }
		class DerivedReturnType : BaseReturnType { }

		interface IRequires
		{
			[RequiresUnreferencedCode ("Message for --IRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("Message for --IRequires.MethodWithRequires--")]
			public void MethodWithRequires ();
		}

		class ImplementationClass : IRequires
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresMethod--")]
			[RequiresAssemblyFiles ("Message for --ImplementationClass.RequiresMethod--")]
			public void MethodWithRequires ()
			{
			}
		}

		abstract class CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnBase.GetRequires--")]
			[RequiresAssemblyFiles ("Message for --CovariantReturnBase.GetRequires--")]
			public abstract BaseReturnType GetRequires ();
		}

		class CovariantReturnDerived : CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnDerived.GetRequires--")]
			[RequiresAssemblyFiles ("Message for --CovariantReturnDerived.GetRequires--")]
			public override DerivedReturnType GetRequires ()
			{
				return null;
			}
		}

		[LogDoesNotContain ("--CovariantReturnBase.GetRequires--")]
		[ExpectedWarning ("IL2026", "--CovariantReturnDerived.GetRequires--")]
		[ExpectedWarning ("IL3002", "--CovariantReturnDerived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestCovariantReturnCallOnDerived ()
		{
			var tmp = new CovariantReturnDerived ();
			tmp.GetRequires ();
		}

		[ExpectedWarning ("IL2026", "--Method--")]
		[ExpectedWarning ("IL3002", "--Method--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--", ProducedBy = ProducedBy.Trimmer)]
		static void TestRequiresThroughReflectionInMethodFromCopiedAssembly ()
		{
			typeof (RequiresInCopyAssembly)
				.GetMethod ("MethodCalledThroughReflection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		static void TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresInDynamicDependency--")]
		[RequiresAssemblyFiles ("Message for --RequiresInDynamicDependency--")]
		static void RequiresInDynamicDependency ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresInDynamicDependency--")]
		[ExpectedWarning ("IL3002", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Analyzer)]
		[DynamicDependency ("RequiresInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresInDynamicDependency ();
		}

		[RequiresUnreferencedCode ("Linker adds a trailing period to this message")]
		[RequiresAssemblyFiles ("Linker adds a trailing period to this message")]
		static void WarningMessageWithoutEndingPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker adds a trailing period to this message.")]
		[ExpectedWarning ("IL3002", "Linker adds a trailing period to this message.", ProducedBy = ProducedBy.Analyzer)]
		static void TestThatTrailingPeriodIsAddedToMessage ()
		{
			WarningMessageWithoutEndingPeriod ();
		}

		[RequiresUnreferencedCode ("Linker does not add a period to this message.")]
		[RequiresAssemblyFiles ("Linker does not add a period to this message.")]
		static void WarningMessageEndsWithPeriod ()
		{
		}

		[LogDoesNotContain ("Linker does not add a period to this message..")]
		[ExpectedWarning ("IL2026", "Linker does not add a period to this message.")]
		[ExpectedWarning ("IL3002", "Linker does not add a period to this message.", ProducedBy = ProducedBy.Analyzer)]
		static void TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ()
		{
			WarningMessageEndsWithPeriod ();
		}

		class WarnIfRequiresOnStaticConstructor
		{
			class ClassWithRequiresOnStaticConstructor
			{
				[ExpectedWarning ("IL2116", ProducedBy = ProducedBy.Trimmer)]
				[RequiresUnreferencedCode ("This attribute shouldn't be allowed")]
				static ClassWithRequiresOnStaticConstructor () { }
			}

			public static void Test ()
			{
				typeof (ClassWithRequiresOnStaticConstructor).RequiresNonPublicConstructors ();
			}
		}

		[ExpectedNoWarnings]
		class RequiresOnAttribute
		{
			class AttributeWhichRequiresAttribute : Attribute
			{
				[RequiresUnreferencedCode ("Message for --AttributeWhichRequiresAttribute.ctor--")]
				[RequiresAssemblyFiles ("Message for --AttributeWhichRequiresAttribute.ctor--")]
				public AttributeWhichRequiresAttribute ()
				{
				}
			}

			class AttributeWhichRequiresOnPropertyAttribute : Attribute
			{
				public AttributeWhichRequiresOnPropertyAttribute ()
				{
				}

				public bool PropertyWhichRequires {
					get => false;

					[RequiresUnreferencedCode ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
					[RequiresAssemblyFiles ("--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
					set { }
				}
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			class GenericTypeWithAttributedParameter<[AttributeWhichRequires] T>
			{
				public static void TestMethod () { }
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			static void GenericMethodWithAttributedParameter<[AttributeWhichRequires] T> () { }

			static void TestRequiresOnAttributeOnGenericParameter ()
			{
				GenericTypeWithAttributedParameter<int>.TestMethod ();
				GenericMethodWithAttributedParameter<int> ();
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			class TypeWithAttributeWhichRequires
			{
			}

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static void MethodWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static int _fieldWithAttributeWhichRequires;

			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresAttribute.ctor--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresAttribute.ctor--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2026", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--")]
			[ExpectedWarning ("IL3002", "--AttributeWhichRequiresOnPropertyAttribute.PropertyWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			static bool PropertyWithAttributeWhichRequires { get; set; }

			[AttributeWhichRequires]
			[AttributeWhichRequiresOnProperty (PropertyWhichRequires = true)]
			[RequiresUnreferencedCode ("--MethodWhichRequiresWithAttributeWhichRequires--")]
			[RequiresAssemblyFiles ("--MethodWhichRequiresWithAttributeWhichRequires--")]
			static void MethodWhichRequiresWithAttributeWhichRequires () { }

			[ExpectedWarning ("IL2026", "--MethodWhichRequiresWithAttributeWhichRequires--")]
			[ExpectedWarning ("IL3002", "--MethodWhichRequiresWithAttributeWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
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

		[RequiresUnreferencedCode ("Message for --RequiresOnlyViaDescriptor--")]
		static void RequiresOnlyViaDescriptor ()
		{
		}

		class RequiresOnGenerics
		{
			class GenericWithStaticMethod<T>
			{
				[RequiresUnreferencedCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				[RequiresAssemblyFiles ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				public static void GenericTypeWithStaticMethodWhichRequires () { }
			}

			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
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
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Base.GetRequires--")]
				[RequiresAssemblyFiles ("Message for --CovariantReturnViaLdftn.Base.GetRequires--")]
				public abstract BaseReturnType GetRequires ();
			}

			class Derived : Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequires--")]
				[RequiresAssemblyFiles ("Message for --CovariantReturnViaLdftn.Derived.GetRequires--")]
				public override DerivedReturnType GetRequires ()
				{
					return null;
				}
			}

			[ExpectedWarning ("IL2026", "--CovariantReturnViaLdftn.Derived.GetRequires--")]
			[ExpectedWarning ("IL3002", "--CovariantReturnViaLdftn.Derived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				var tmp = new Derived ();
				var _ = new Func<DerivedReturnType> (tmp.GetRequires);
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

			// https://github.com/mono/linker/issues/2116
			[ExpectedWarning ("IL2026", "--PInvokeReturnType.ctor--", ProducedBy = ProducedBy.Trimmer)]
			[DllImport ("nonexistent")]
			static extern PInvokeReturnType PInvokeReturnsType ();

			// Analyzer doesn't support IL2050 yet
			[ExpectedWarning ("IL2050", ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				PInvokeReturnsType ();
			}
		}

		class OnEventMethod
		{
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Trimmer)]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				[RequiresAssemblyFiles ("Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Trimmer)]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				[RequiresAssemblyFiles ("Message for --EventToTestAdd.add--")]
				add { }
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--")]
			[ExpectedWarning ("IL3002", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			[ExpectedWarning ("IL3002", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				EventToTestRemove -= (sender, e) => { };
				EventToTestAdd += (sender, e) => { };
			}
		}

		class AccessThroughNewConstraint
		{
			class NewConstraintTestType
			{
				[RequiresUnreferencedCode ("Message for --NewConstraintTestType.ctor--")]
				[RequiresAssemblyFiles ("Message for --NewConstraintTestType.ctor--")]
				public NewConstraintTestType () { }
			}

			static void GenericMethod<T> () where T : new() { }

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
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
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			public static void TestNewConstraintOnTypeParameter ()
			{
				_ = new NewConstaintOnTypeParameter<NewConstraintTestType> ();
			}

			[ExpectedWarning ("IL2026", "--NewConstraintTestType.ctor--")]
			[ExpectedWarning ("IL3002", "--NewConstraintTestType.ctor--", ProducedBy = ProducedBy.Analyzer)]
			public static void TestNewConstraintOnTypeParameterOfStaticType ()
			{
				NewConstraintOnTypeParameterOfStaticType<NewConstraintTestType>.DoNothing ();
			}
		}

		class AccessThroughLdToken
		{
			static bool PropertyWithLdToken {
				[RequiresUnreferencedCode ("Message for --PropertyWithLdToken.get--")]
				[RequiresAssemblyFiles ("Message for --PropertyWithLdToken.get--")]
				get {
					return false;
				}
			}

			[ExpectedWarning ("IL2026", "--PropertyWithLdToken.get--")]
			[ExpectedWarning ("IL3002", "--PropertyWithLdToken.get--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				Expression<Func<bool>> getter = () => PropertyWithLdToken;
			}
		}

		class AttributeMismatch
		{
			static void RequirePublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
			}

			class BaseClassWithRequires
			{
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				public virtual void VirtualMethod ()
				{
				}

				public virtual string VirtualPropertyAnnotationInAccesor {
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get;
					set;
				}

				[RequiresAssemblyFiles ("Message")]
				public virtual string VirtualPropertyAnnotationInProperty { get; set; }
			}

			class BaseClassWithoutRequires
			{
				public virtual void VirtualMethod ()
				{
				}

				public virtual string VirtualPropertyAnnotationInAccesor { get; set; }

				public virtual string VirtualPropertyAnnotationInProperty { get; set; }
			}

			class DerivedClassWithRequires : BaseClassWithoutRequires
			{
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()")]
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer)]
				public override void VirtualMethod ()
				{
				}

				private string name;
				public override string VirtualPropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get { return name; }
					set { name = value; }
				}

				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithoutRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				public override string VirtualPropertyAnnotationInProperty { get; set; }
			}

			class DerivedClassWithoutRequires : BaseClassWithRequires
			{
				[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer)]
				public override void VirtualMethod ()
				{
				}

				private string name;
				public override string VirtualPropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					get { return name; }
					set { name = value; }
				}

				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				public override string VirtualPropertyAnnotationInProperty { get; set; }
			}

			public interface IBaseWithRequires
			{
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				void Method ();

				string PropertyAnnotationInAccesor {
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get;
					set;
				}

				[RequiresAssemblyFiles ("Message")]
				string PropertyAnnotationInProperty { get; set; }
			}

			public interface IBaseWithoutRequires
			{
				void Method ();

				string PropertyAnnotationInAccesor { get; set; }

				string PropertyAnnotationInProperty { get; set; }
			}

			class ImplementationClassWithRequires : IBaseWithoutRequires
			{
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
				public void Method ()
				{
				}

				private string name;
				public string PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get { return name; }
					set { name = value; }
				}

				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				public string PropertyAnnotationInProperty { get; set; }
			}

			class ExplicitImplementationClassWithRequires : IBaseWithoutRequires
			{
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()")]
				[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
				void IBaseWithoutRequires.Method ()
				{
				}

				private string name;
				string IBaseWithoutRequires.PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get { return name; }
					set { name = value; }
				}

				[RequiresAssemblyFiles ("Message")]
				[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				string IBaseWithoutRequires.PropertyAnnotationInProperty { get; set; }
			}

			class ImplementationClassWithoutRequires : IBaseWithRequires
			{
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
				public void Method ()
				{
				}

				private string name;
				public string PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					get { return name; }
					set { name = value; }
				}

				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				public string PropertyAnnotationInProperty { get; set; }
			}

			class ExplicitImplementationClassWithoutRequires : IBaseWithRequires
			{
				[ExpectedWarning ("IL2046", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithRequires.Method()")]
				[ExpectedWarning ("IL3003", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
				void IBaseWithRequires.Method ()
				{
				}

				private string name;
				string IBaseWithRequires.PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					get { return name; }
					set { name = value; }
				}

				[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresCapability.AttributeMismatch.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				string IBaseWithRequires.PropertyAnnotationInProperty { get; set; }
			}

			class ImplementationClassWithoutRequiresInSource : ReferenceInterfaces.IBaseWithRequiresInReference
			{
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer)]
				public void Method ()
				{
				}

				private string name;
				public string PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					get { return name; }
					set { name = value; }
				}

				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty", "IBaseWithRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				public string PropertyAnnotationInProperty { get; set; }
			}

			class ImplementationClassWithRequiresInSource : ReferenceInterfaces.IBaseWithoutRequiresInReference
			{
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				public void Method ()
				{
				}

				private string name;
				public string PropertyAnnotationInAccesor {
					[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get")]
					[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer)]
					[RequiresUnreferencedCode ("Message")]
					[RequiresAssemblyFiles ("Message")]
					get { return name; }
					set { name = value; }
				}

				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
				[RequiresAssemblyFiles ("Message")]
				public string PropertyAnnotationInProperty { get; set; }
			}

			[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				RequirePublicMethods (typeof (BaseClassWithRequires));
				RequirePublicMethods (typeof (BaseClassWithoutRequires));
				RequirePublicMethods (typeof (DerivedClassWithRequires));
				RequirePublicMethods (typeof (DerivedClassWithoutRequires));
				RequirePublicMethods (typeof (IBaseWithRequires));
				RequirePublicMethods (typeof (IBaseWithoutRequires));
				RequirePublicMethods (typeof (ImplementationClassWithRequires));
				RequirePublicMethods (typeof (ImplementationClassWithoutRequires));
				RequirePublicMethods (typeof (ExplicitImplementationClassWithRequires));
				RequirePublicMethods (typeof (ExplicitImplementationClassWithoutRequires));
				RequirePublicMethods (typeof (ImplementationClassWithoutRequiresInSource));
				RequirePublicMethods (typeof (ImplementationClassWithRequiresInSource));
			}
		}

		class RequiresOnClass
		{
			[RequiresUnreferencedCode ("Message for --ClassWithRequires--")]
			class ClassWithRequires
			{
				public static object Instance;

				public ClassWithRequires () { }

				public static void StaticMethod () { }

				public void NonStaticMethod () { }

				// RequiresOnMethod.MethodWithRequires generates a warning that gets suppressed because the declaring type has RUC
				public static void CallMethodWithRequires () => RequiresOnMethod.MethodWithRequires ();

				public class NestedClass
				{
					public static void NestedStaticMethod () { }

					// This warning doesn't get suppressed since the declaring type NestedClass is not annotated with Requires
					[ExpectedWarning ("IL2026", "RequiresOnClass.RequiresOnMethod.MethodWithRequires()", "MethodWithRequires")]
					public static void CallMethodWithRequires () => RequiresOnMethod.MethodWithRequires ();
				}

				// RequiresUnfereferencedCode on the type will suppress IL2072
				static ClassWithRequires ()
				{
					Instance = Activator.CreateInstance (Type.GetType ("SomeText"));
				}

				public static void TestSuppressions (Type[] types)
				{
					// StaticMethod is a static method on a Requires annotated type, so it should warn. But Requires in the
					// class suppresses other Requires messages
					StaticMethod ();

					var nested = new NestedClass ();

					// Requires in the class suppresses DynamicallyAccessedMembers messages
					types[1].GetMethods ();

					void LocalFunction (int a) { }
					LocalFunction (2);
				}
			}

			class RequiresOnMethod
			{
				[RequiresUnreferencedCode ("MethodWithRequires")]
				public static void MethodWithRequires () { }
			}

			[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithoutRequires", "RequiresOnClass.ClassWithRequires", "--ClassWithRequires--", ProducedBy = ProducedBy.Trimmer)]
			private class DerivedWithoutRequires : ClassWithRequires
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}

				public static void ShouldntWarn (object objectToCast)
				{
					_ = typeof (ClassWithRequires);
					var type = (ClassWithRequires) objectToCast;
				}
			}

			// In order to generate IL2109 the nested class would also need to be annotated with Requires
			// otherwise we threat the nested class as safe
			private class DerivedWithoutRequires2 : ClassWithRequires.NestedClass
			{
				public static void StaticMethod () { }
			}

			[UnconditionalSuppressMessage ("trim", "IL2109")]
			class TestUnconditionalSuppressMessage : ClassWithRequires
			{
				public static void StaticMethodInTestSuppressionClass () { }
			}

			class ClassWithoutRequires
			{
				public ClassWithoutRequires () { }

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

			[ExpectedWarning ("IL2026", "RequiresOnClass.StaticCtor.StaticCtor()", "Message for --StaticCtor--")]
			static void TestStaticCctorRequires ()
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

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--")]
			static void TestStaticCtorMarkingIsTriggeredByFieldAccessWrite ()
			{
				StaticCtorTriggeredByFieldAccess.field = 1;
			}

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--")]
			static void TestStaticCtorMarkingTriggeredOnSecondAccessWrite ()
			{
				StaticCtorTriggeredByFieldAccess.field = 2;
			}

			[RequiresUnreferencedCode ("--TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner--")]
			static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner ()
			{
				StaticCtorTriggeredByFieldAccess.field = 3;
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod ()
			{
				TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner ();
			}

			[RequiresUnreferencedCode ("Message for --StaticCCtorTriggeredByFieldAccessRead--")]
			class StaticCCtorTriggeredByFieldAccessRead
			{
				public static int field = 42;
			}

			[ExpectedWarning ("IL2026", "StaticCCtorTriggeredByFieldAccessRead.field", "Message for --StaticCCtorTriggeredByFieldAccessRead--")]
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

			[ExpectedWarning ("IL2026", "StaticCtorTriggeredByCtorCalls.StaticCtorTriggeredByCtorCalls()")]
			static void TestStaticCtorTriggeredByCtorCall ()
			{
				new StaticCtorTriggeredByCtorCalls ();
			}

			[RequiresUnreferencedCode ("Message for --ClassWithInstanceField--")]
			class ClassWithInstanceField
			{
				public int field = 42;
			}

			[ExpectedWarning ("IL2026", "ClassWithInstanceField.ClassWithInstanceField()")]
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
			private class DerivedWithRequires : ClassWithoutRequires
			{
				public static void StaticMethodInInheritedClass () { }

				public class DerivedNestedClass
				{
					public static void NestedStaticMethod () { }
				}
			}

			[RequiresUnreferencedCode ("Message for --DerivedWithRequires2--")]
			private class DerivedWithRequires2 : ClassWithRequires
			{
				public static void StaticMethodInInheritedClass () { }

				// A nested class is not considered a static method nor constructor therefore RequiresUnreferencedCode doesnt apply
				// and this warning is not suppressed
				[ExpectedWarning ("IL2109", "RequiresOnClass/DerivedWithRequires2/DerivedNestedClass", "--ClassWithRequires--", ProducedBy = ProducedBy.Trimmer)]
				public class DerivedNestedClass : ClassWithRequires
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
				// Bug https://github.com/dotnet/linker/issues/2379
				[ExpectedWarning ("IL2046", ProducedBy = ProducedBy.Analyzer)]
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

				// Bug https://github.com/dotnet/linker/issues/2379
				[ExpectedWarning ("IL2046", ProducedBy = ProducedBy.Analyzer)]
				public int Method (int a)
				{
					return a;
				}
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
			static void TestRequiresInClassAccessedByStaticMethod ()
			{
				ClassWithRequires.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires", "--ClassWithRequires--")]
			static void TestRequiresInClassAccessedByCctor ()
			{
				var classObject = new ClassWithRequires ();
			}

			static void TestRequiresInParentClassAccesedByStaticMethod ()
			{
				ClassWithRequires.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
			// Although we suppress the warning from RequiresOnMethod.MethodWithRequires () we still get a warning because we call CallRequiresMethod() which is an static method on a type with RUC
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.CallMethodWithRequires()", "--ClassWithRequires--")]
			[ExpectedWarning ("IL2026", "ClassWithRequires.Instance", "--ClassWithRequires--")]
			static void TestRequiresOnBaseButNotOnDerived ()
			{
				DerivedWithoutRequires.StaticMethodInInheritedClass ();
				DerivedWithoutRequires.StaticMethod ();
				DerivedWithoutRequires.CallMethodWithRequires ();
				DerivedWithoutRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.NestedStaticMethod ();
				DerivedWithoutRequires.NestedClass.CallMethodWithRequires ();
				DerivedWithoutRequires.ShouldntWarn (null);
				DerivedWithoutRequires.Instance.ToString ();
				DerivedWithoutRequires2.StaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires.StaticMethodInInheritedClass()", "--DerivedWithRequires--")]
			static void TestRequiresOnDerivedButNotOnBase ()
			{
				DerivedWithRequires.StaticMethodInInheritedClass ();
				DerivedWithRequires.StaticMethod ();
				DerivedWithRequires.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires.NestedClass.NestedStaticMethod ();
			}

			[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires2.StaticMethodInInheritedClass()", "--DerivedWithRequires2--")]
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
			static void TestRequiresOnBaseAndDerived ()
			{
				DerivedWithRequires2.StaticMethodInInheritedClass ();
				DerivedWithRequires2.StaticMethod ();
				DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod ();
				DerivedWithRequires2.NestedClass.NestedStaticMethod ();
			}

			// TODO: Parameter signature differs between linker and analyzer
			[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.TestSuppressions(", "Type[])")]
			static void TestSuppressionsOnClass ()
			{
				ClassWithRequires.TestSuppressions (new[] { typeof (ClassWithRequires) });
				TestUnconditionalSuppressMessage.StaticMethodInTestSuppressionClass ();
			}

			[RequiresUnreferencedCode ("--StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod--")]
			static void StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ()
			{
				DerivedWithRequires.StaticMethodInInheritedClass ();
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			static void TestStaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ()
			{
				StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ();
			}

			static void TestStaticConstructorCalls ()
			{
				TestStaticCctorRequires ();
				TestStaticCtorMarkingIsTriggeredByFieldAccessWrite ();
				TestStaticCtorMarkingTriggeredOnSecondAccessWrite ();
				TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod ();
				TestStaticCtorMarkingIsTriggeredByFieldAccessRead ();
				TestStaticCtorTriggeredByMethodCall ();
				TestStaticCtorTriggeredByCtorCall ();
				TestInstanceFieldCallDontWarn ();
			}

			[RequiresUnreferencedCode ("--MemberTypesWithRequires--")]
			class MemberTypesWithRequires
			{
				public static int field;
				public static int Property { get; set; }

				// These should not be reported https://github.com/mono/linker/issues/2218
				[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.add", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Trimmer)]
				public static event EventHandler Event;
			}

			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.field")]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Property.set")]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove")]
			static void TestOtherMemberTypesWithRequires ()
			{
				MemberTypesWithRequires.field = 1;
				MemberTypesWithRequires.Property = 1;
				MemberTypesWithRequires.Event -= null;
			}

			class ReflectionAccessOnMethod
			{
				// Analyzer still dont understand RUC on type
				[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()", ProducedBy = ProducedBy.Trimmer)]
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

				[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()", ProducedBy = ProducedBy.Trimmer)]
				static void TestDirectReflectionAccess ()
				{
					// Requires on the method itself
					typeof (BaseWithoutRequiresOnType).GetMethod (nameof (BaseWithoutRequiresOnType.Method));

					// Requires on the method itself
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
				[RequiresUnreferencedCode ("--BaseWithRequires--")]
				class BaseWithRequires
				{
					public BaseWithRequires () { }
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnCtor/DerivedWithoutRequires", "ReflectionAccessOnCtor.BaseWithRequires", ProducedBy = ProducedBy.Trimmer)]
				class DerivedWithoutRequires : BaseWithRequires
				{
					[ExpectedWarning ("IL2026", "--BaseWithRequires--", ProducedBy = ProducedBy.Trimmer)] // The body has direct call to the base.ctor()
					public DerivedWithoutRequires () { }
				}

				[RequiresUnreferencedCode ("--DerivedWithRequiresOnBaseWithRequires--")]
				class DerivedWithRequiresOnBaseWithRequires : BaseWithRequires
				{
					// No warning - suppressed by the Requires on this type
					private DerivedWithRequiresOnBaseWithRequires () { }
				}

				class BaseWithoutRequires { }

				[RequiresUnreferencedCode ("--DerivedWithRequiresOnBaseWithout--")]
				class DerivedWithRequiresOnBaseWithoutRequires : BaseWithoutRequires
				{
					public DerivedWithRequiresOnBaseWithoutRequires () { }
				}

				[ExpectedWarning ("IL2026", "BaseWithRequires.BaseWithRequires()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()", ProducedBy = ProducedBy.Trimmer)]
				static void TestDAMAccess ()
				{
					// Warns because the type has Requires
					typeof (BaseWithRequires).RequiresPublicConstructors ();

					// Doesn't warn since there's no Requires on this type
					typeof (DerivedWithoutRequires).RequiresPublicParameterlessConstructor ();

					// Warns - Requires on the type
					typeof (DerivedWithRequiresOnBaseWithRequires).RequiresNonPublicConstructors ();

					// Warns - Requires On the type
					typeof (DerivedWithRequiresOnBaseWithoutRequires).RequiresPublicConstructors ();
				}

				[ExpectedWarning ("IL2026", "BaseWithRequires.BaseWithRequires()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()", ProducedBy = ProducedBy.Trimmer)]
				static void TestDirectReflectionAccess ()
				{
					typeof (BaseWithRequires).GetConstructor (Type.EmptyTypes);
					typeof (DerivedWithoutRequires).GetConstructor (Type.EmptyTypes);
					typeof (DerivedWithRequiresOnBaseWithRequires).GetConstructor (BindingFlags.NonPublic, Type.EmptyTypes);
					typeof (DerivedWithRequiresOnBaseWithoutRequires).GetConstructor (Type.EmptyTypes);
				}

				public static void Test ()
				{
					TestDAMAccess ();
					TestDirectReflectionAccess ();
				}
			}

			class ReflectionAccessOnField
			{
				[RequiresUnreferencedCode ("--WithRequires--")]
				class WithRequires
				{
					public int InstanceField;
					public static int StaticField;
					private static int PrivateStaticField;
				}

				[RequiresUnreferencedCode ("--WithRequiresOnlyInstanceFields--")]
				class WithRequiresOnlyInstanceFields
				{
					public int InstanceField;
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnField/DerivedWithoutRequires", "ReflectionAccessOnField.WithRequires", ProducedBy = ProducedBy.Trimmer)]
				class DerivedWithoutRequires : WithRequires
				{
					public static int DerivedStaticField;
				}

				[RequiresUnreferencedCode ("--DerivedWithRequires--")]
				class DerivedWithRequires : WithRequires
				{
					public static int DerivedStaticField;
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticField", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticField", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField", ProducedBy = ProducedBy.Trimmer)]
				static void TestDAMAccess ()
				{
					typeof (WithRequires).RequiresPublicFields ();
					typeof (WithRequires).RequiresNonPublicFields ();
					typeof (WithRequiresOnlyInstanceFields).RequiresPublicFields ();
					typeof (DerivedWithoutRequires).RequiresPublicFields ();
					typeof (DerivedWithRequires).RequiresPublicFields ();
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
				// Analyzer does not recognize the binding flags
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticField", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField")]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField", ProducedBy = ProducedBy.Analyzer)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRequires).GetField (nameof (WithRequires.StaticField));
					typeof (WithRequires).GetField (nameof (WithRequires.InstanceField)); // Doesn't warn
					typeof (WithRequires).GetField ("PrivateStaticField", BindingFlags.NonPublic);
					typeof (WithRequiresOnlyInstanceFields).GetField (nameof (WithRequiresOnlyInstanceFields.InstanceField)); // Doesn't warn
					typeof (DerivedWithoutRequires).GetField (nameof (DerivedWithRequires.DerivedStaticField)); // Doesn't warn
					typeof (DerivedWithRequires).GetField (nameof (DerivedWithRequires.DerivedStaticField));
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
				[DynamicDependency (nameof (WithRequires.StaticField), typeof (WithRequires))]
				[DynamicDependency (nameof (WithRequires.InstanceField), typeof (WithRequires))] // Doesn't warn
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithoutRequires))] // Doesn't warn
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField", ProducedBy = ProducedBy.Trimmer)]
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithRequires))]
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
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseField", ProducedBy = ProducedBy.Trimmer)]
				class DAMAnnotatedClass : BaseForDAMAnnotatedClass
				{
					[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicField", ProducedBy = ProducedBy.Trimmer)]
					public static int publicField;

					[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privatefield", ProducedBy = ProducedBy.Trimmer)]
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

				[RequiresUnreferencedCode ("--WithRequires--")]
				class WithRequires
				{
					// These should be reported only in TestDirectReflectionAccess
					// https://github.com/mono/linker/issues/2218
					[ExpectedWarning ("IL2026", "StaticEvent.add", ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL2026", "StaticEvent.remove", ProducedBy = ProducedBy.Trimmer)]
					public static event EventHandler StaticEvent;
				}

				[ExpectedWarning ("IL2026", "StaticEvent.add", ProducedBy = ProducedBy.Trimmer)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRequires).GetEvent (nameof (WithRequires.StaticEvent));
				}

				public static void Test ()
				{
					TestDirectReflectionAccess ();
				}
			}

			class ReflectionAccessOnProperties
			{
				[RequiresUnreferencedCode ("--WithRequires--")]
				class WithRequires
				{
					public int InstanceProperty { get; set; }
					public static int StaticProperty { get; set; }
					private static int PrivateStaticProperty { get; set; }
				}

				[RequiresUnreferencedCode ("--WithRequiresOnlyInstanceProperties--")]
				class WithRequiresOnlyInstanceProperties
				{
					public int InstnaceProperty { get; set; }
				}

				[ExpectedWarning ("IL2109", "ReflectionAccessOnProperties/DerivedWithoutRequires", "ReflectionAccessOnProperties.WithRequires", ProducedBy = ProducedBy.Trimmer)]
				class DerivedWithoutRequires : WithRequires
				{
					public static int DerivedStaticProperty { get; set; }
				}

				[RequiresUnreferencedCode ("--DerivedWithRequires--")]
				class DerivedWithRequires : WithRequires
				{
					public static int DerivedStaticProperty { get; set; }
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				static void TestDAMAccess ()
				{
					typeof (WithRequires).RequiresPublicProperties ();
					typeof (WithRequires).RequiresNonPublicProperties ();
					typeof (WithRequiresOnlyInstanceProperties).RequiresPublicProperties ();
					typeof (DerivedWithoutRequires).RequiresPublicProperties ();
					typeof (DerivedWithRequires).RequiresPublicProperties ();
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				static void TestDirectReflectionAccess ()
				{
					typeof (WithRequires).GetProperty (nameof (WithRequires.StaticProperty));
					typeof (WithRequires).GetProperty (nameof (WithRequires.InstanceProperty)); // Doesn't warn
					typeof (WithRequires).GetProperty ("PrivateStaticProperty", BindingFlags.NonPublic);
					typeof (WithRequiresOnlyInstanceProperties).GetProperty (nameof (WithRequiresOnlyInstanceProperties.InstnaceProperty)); // Doesn't warn
					typeof (DerivedWithoutRequires).GetProperty (nameof (DerivedWithRequires.DerivedStaticProperty)); // Doesn't warn
					typeof (DerivedWithRequires).GetProperty (nameof (DerivedWithRequires.DerivedStaticProperty));
				}

				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[DynamicDependency (nameof (WithRequires.StaticProperty), typeof (WithRequires))]
				[DynamicDependency (nameof (WithRequires.InstanceProperty), typeof (WithRequires))] // Doesn't warn
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (DerivedWithoutRequires))] // Doesn't warn
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
				[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (DerivedWithRequires))]
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
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseProperty.get", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2113", "BaseForDAMAnnotatedClass.baseProperty.set", ProducedBy = ProducedBy.Trimmer)]
				class DAMAnnotatedClass : BaseForDAMAnnotatedClass
				{
					public static int publicProperty {
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicProperty.get", ProducedBy = ProducedBy.Trimmer)]
						get;
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.publicProperty.set", ProducedBy = ProducedBy.Trimmer)]
						set;
					}

					static int privateProperty {
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privateProperty.get", ProducedBy = ProducedBy.Trimmer)]
						get;
						[ExpectedWarning ("IL2112", "DAMAnnotatedClass.privateProperty.set", ProducedBy = ProducedBy.Trimmer)]
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
			public class AttributeWithRequires : Attribute
			{
				public static int field;

				// `field` cannot be used as named attribute argument because is static, and if accessed via
				// a property the property will be the one generating the warning, but then the warning will
				// be suppresed by the Requires on the declaring type
				public int PropertyOnAttribute {
					get { return field; }
					set { field = value; }
				}
			}

			[AttributeWithRequires (PropertyOnAttribute = 42)]
			[ExpectedWarning ("IL2026", "AttributeWithRequires.AttributeWithRequires()", ProducedBy = ProducedBy.Trimmer)]
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
				TestStaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ();
				TestStaticConstructorCalls ();
				TestOtherMemberTypesWithRequires ();
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