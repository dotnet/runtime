// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation (By = Tool.NativeAot)]
	public class TypeHierarchyReflectionWarnings
	{
		public static void Main ()
		{
			annotatedBase.GetType ().RequiresPublicMethods ();
			var baseType = annotatedBaseSharedByNestedTypes.GetType ();
			baseType.RequiresPublicNestedTypes ();
			baseType.RequiresPublicMethods ();
			var derivedType = typeof (DerivedWithNestedTypes);
			// Reference to the derived type should apply base annotations
			var t1 = typeof (DerivedFromAnnotatedBase);
			var t2 = typeof (AnnotatedDerivedFromAnnotatedBase);
			var t3 = typeof (AnnotatedAllDerivedFromAnnotatedBase);
			annotatedDerivedFromBase.GetType ().RequiresPublicMethods ();
			annotatedPublicNestedTypes.GetType ().RequiresPublicNestedTypes ();
			derivedFromAnnotatedDerivedFromBase.GetType ().RequiresPublicFields ();
			annotatedPublicMethods.GetType ().RequiresPublicMethods ();
			annotatedPublicFields.GetType ().RequiresPublicFields ();
			annotatedPublicProperties.GetType ().RequiresPublicProperties ();
			annotatedPublicEvents.GetType ().RequiresPublicEvents ();
			annotatedPublicNestedTypes.GetType ().RequiresPublicNestedTypes ();
			annotatedInterfaces.GetType ().RequiresInterfaces ();
			annotatedPublicParameterlessConstructor.GetType ().RequiresPublicParameterlessConstructor ();
			annotatedAll.GetType ().RequiresAll ();
			var t4 = typeof (DerivedFromAnnotatedAll1);
			var t5 = typeof (DerivedFromAnnotatedAll2);
			var t6 = typeof (DerivedFromAnnotatedAllWithInterface);
			var t7 = typeof (DerivedFromAnnotatedPublicParameterlessConstructor);
			annotatedRUCPublicMethods.GetType ().RequiresPublicMethods ();

			// Instantiate this type just so its property getters are considered reachable
			var b = new DerivedFromAnnotatedDerivedFromBase ();

			// Check that this field doesn't produce a warning even if it is kept
			// for some non-reflection access.
			var f = AnnotatedPublicMethods.DAMField;

			RUCOnNewSlotVirtualMethodDerivedAnnotated.Test ();

			CompilerGeneratedBackingField.Test ();

			RUCOnVirtualOnAnnotatedBase.Test ();
			RUCOnVirtualOnAnnotatedBaseUsedByDerived.Test ();
			UseByDerived.Test ();
		}

		[Kept]
		static AnnotatedAll annotatedAll;
		[Kept]
		static AnnotatedPublicMethods annotatedPublicMethods;
		[Kept]
		static AnnotatedPublicFields annotatedPublicFields;
		[Kept]
		static AnnotatedPublicProperties annotatedPublicProperties;
		[Kept]
		static AnnotatedPublicEvents annotatedPublicEvents;
		[Kept]
		static AnnotatedInterfaces annotatedInterfaces;
		[Kept]
		static AnnotatedPublicParameterlessConstructor annotatedPublicParameterlessConstructor;
		[Kept]
		static AnnotatedBase annotatedBase;
		[Kept]
		static AnnotatedBaseSharedByNestedTypes annotatedBaseSharedByNestedTypes;
		[Kept]
		static AnnotatedDerivedFromBase annotatedDerivedFromBase;
		[Kept]
		static AnnotatedPublicNestedTypes annotatedPublicNestedTypes;
		[Kept]
		static DerivedFromAnnotatedDerivedFromBase derivedFromAnnotatedDerivedFromBase;
		[Kept]
		static AnnotatedRUCPublicMethods annotatedRUCPublicMethods;

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class AnnotatedAll
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			[ExpectedWarning ("IL2114", nameof (AnnotatedAll), nameof (DAMField))]
			public Type DAMField;

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedAll.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedAll.RUCMethod--")]
			public void RUCMethod () { }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedAll), nameof (DAMMethod))]
			public void DAMMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type t
			)
			{ }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (AnnotatedAll))]
		class DerivedFromAnnotatedAll1 : AnnotatedAll
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (AnnotatedAll))]
		class DerivedFromAnnotatedAll2 : AnnotatedAll
		{
		}

		interface InterfaceImplementedByDerived
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on InterfaceImplementedByDerived.RUCMethod--")]
			void RUCInterfaceMethod () { }
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (AnnotatedAll))]
		[KeptInterface (typeof (InterfaceImplementedByDerived))]
		[ExpectedWarning ("IL2113", "--RUC on InterfaceImplementedByDerived.RUCMethod--")]
		class DerivedFromAnnotatedAllWithInterface : AnnotatedAll, InterfaceImplementedByDerived
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedPublicMethods
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedPublicMethods.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedPublicMethods.RUCMethod--")]
			public void RUCMethod () { }

			// No warning for members not selected by the type's annotation
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type UnusedDAMField;

			// No warning for members not selected by the type's annotation, even if field is referenced statically
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type DAMField;

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicMethods), nameof (DAMMethod))]
			public void DAMMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type t
			)
			{ }

			[Kept]
			// No warning for non-virtual method which only has DAM on return parameter
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type DAMReturnMethod () => null;

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicMethods), nameof (DAMVirtualMethod))]
			public virtual void DAMVirtualMethod (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type type
			)
			{ }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicMethods), nameof (DAMReturnVirtualMethod))]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.PublicMethods)]
			public virtual Type DAMReturnVirtualMethod () => null;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		class AnnotatedPublicFields
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			[ExpectedWarning ("IL2114", nameof (AnnotatedPublicFields), nameof (DAMField))]
			public Type DAMField;

		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		class AnnotatedPublicProperties
		{
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static string DAMProperty {
				[Kept]
				// No warning for non-virtual getter since return value is not annotated
				get;
				[Kept]
				// Property access reports warnings on getter/setter
				[ExpectedWarning ("IL2114", nameof (AnnotatedPublicProperties), nameof (DAMProperty) + ".set")]
				set;
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
		class AnnotatedPublicEvents
		{
			[Kept]
			[KeptMember (".ctor(System.Object,System.IntPtr)")]
			[KeptMember ("Invoke(System.Object,System.Int32)")]
			[KeptBaseType (typeof (MulticastDelegate))]
			public delegate void MyEventHandler (object sender, int i);

			[Kept]
			// ILLink always keeps event methods when an event is kept, so this generates warnings
			// on the event itself (since an event access is considered to reference the annotated add method),
			// and on the add method (if it is accessed through reflection).
			[ExpectedWarning ("IL2026", "--RUC on add_RUCEvent--", ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", "--RUC on add_RUCEvent--", ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", "--RUC on add_RUCEvent--", ProducedBy = Tool.Trimmer)]
			public event MyEventHandler RUCEvent {
				[Kept]
				[ExpectedWarning ("IL2112", nameof (AnnotatedPublicEvents), "--RUC on add_RUCEvent--")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUC on add_RUCEvent--")]
				add { }
				[Kept]
				remove { }
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		interface RequiredInterface
		{
			// Removed, because keeping the interface on its own
			// doesn't apply its type annotations
			[RequiresUnreferencedCode ("--RUC on RequiredInterface.UnusedMethod--")]
			void RUCMethod ();
		}

		[Kept]
		[KeptInterface (typeof (RequiredInterface))]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)]
		class AnnotatedInterfaces : RequiredInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// This should produce a warning: https://github.com/dotnet/linker/issues/2161
			[ExpectedWarning("IL2112", "--RUC on AnnotatedInterfaces.UnusedMethod--", ProducedBy = Tool.NativeAot)]
			[RequiresUnreferencedCode ("--RUC on AnnotatedInterfaces.UnusedMethod--")]
			public void RUCMethod () { }
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		class AnnotatedPublicParameterlessConstructor
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedPublicParameterlessConstructor()--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedPublicParameterlessConstructor()--")]
			public AnnotatedPublicParameterlessConstructor () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on AnnotatedPublicParameterlessConstructor(int)--")]
			public AnnotatedPublicParameterlessConstructor (int i) { }
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (AnnotatedPublicParameterlessConstructor))]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedPublicParameterlessConstructor(int)--")]
		// This warning is redundant because the base type already has DAMT.PublicParameterlessConstructors,
		// but we produce it anyway due to implementation difficulties in the case of DAMT.PublicConstructors.
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedPublicParameterlessConstructor()--")]
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedPublicParameterlessConstructor()--")]
		class DerivedFromAnnotatedPublicParameterlessConstructor : AnnotatedPublicParameterlessConstructor
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromAnnotatedPublicParameterlessConstructor()--")]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromAnnotatedPublicParameterlessConstructor()--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[RequiresUnreferencedCode ("--RUC on DerivedFromAnnotatedPublicParameterlessConstructor()--")]
			public DerivedFromAnnotatedPublicParameterlessConstructor () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromAnnotatedPublicParameterlessConstructor(int)--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromAnnotatedPublicParameterlessConstructor(int)--")]
			public DerivedFromAnnotatedPublicParameterlessConstructor (int i) { }
		}

		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedBase--")]
			public void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMField1;
		}

		[KeptBaseType (typeof (AnnotatedBase))]
		class DerivedFromAnnotatedBase : AnnotatedBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on DerivedFromAnnotatedBase--")]
			[RequiresUnreferencedCode ("--RUC on DerivedFromAnnotatedBase--")]
			public void RUCMethod () { }
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptBaseType (typeof (AnnotatedBase))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)]
		// No warning for public methods on base type because that annotation is already
		// inherited from the base type.
		[ExpectedWarning ("IL2115", nameof (AnnotatedBase), nameof (AnnotatedBase.DAMField1))]
		class AnnotatedDerivedFromAnnotatedBase : AnnotatedBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedDerivedFromAnnotatedBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedDerivedFromAnnotatedBase--")]
			public void DerivedRUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[ExpectedWarning ("IL2114", nameof (AnnotatedDerivedFromAnnotatedBase), nameof (DAMField2))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMField2;
		}

		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptBaseType (typeof (AnnotatedBase))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		// This warning is redundant because the base type already has DAMT.PublicMethods,
		// but we produce it anyway due to implementation difficulties in the case of DAMT.All.
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedBase--")]
		[ExpectedWarning ("IL2115", nameof (AnnotatedBase), nameof (AnnotatedBase.DAMField1))]
		class AnnotatedAllDerivedFromAnnotatedBase : AnnotatedBase
		{
		}

		[KeptMember (".ctor()")]
		class Base
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUCBaseMethod--")]
			public void RUCBaseMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--Base.RUCVirtualMethod--")]
			public virtual void RUCVirtualMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMField1;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public virtual string DAMVirtualProperty { [Kept] get; }
		}

		[KeptBaseType (typeof (Base))]
		[KeptMember (".ctor()")]
		[ExpectedWarning ("IL2113", "--RUCBaseMethod--")]
		[ExpectedWarning ("IL2113", "--Base.RUCVirtualMethod--")]
		[ExpectedWarning ("IL2115", nameof (Base), nameof (Base.DAMVirtualProperty) + ".get")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class AnnotatedDerivedFromBase : Base
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedDerivedFromBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedDerivedFromBase--")]
			public void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// shouldn't warn because we warn on the base method instead
			[RequiresUnreferencedCode ("--AnnotatedDerivedFromBase.RUCVirtualMethod--")]
			public override void RUCVirtualMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMField2;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			// shouldn't warn because we warn on the base getter instead
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public override string DAMVirtualProperty { [Kept] get; }

		}

		[KeptBaseType (typeof (AnnotatedDerivedFromBase))]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		// The annotation from this type should warn about all public fields including those
		// from base types, but the inherited PublicMethods annotation should not warn
		// again about base methods.
		[ExpectedWarning ("IL2115", nameof (Base), nameof (DAMField1))]
		[ExpectedWarning ("IL2115", nameof (AnnotatedDerivedFromBase), nameof (DAMField2))]
		class DerivedFromAnnotatedDerivedFromBase : AnnotatedDerivedFromBase
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedDerivedFromBase--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedDerivedFromBase--")]
			public void RUCMethod () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			// shouldn't warn because we warn on the base method instead
			[RequiresUnreferencedCode ("--DerivedFromAnnotatedDerivedFromBase.RUCVirtualMethod--")]
			public override void RUCVirtualMethod () { }

			[Kept]
			[ExpectedWarning ("IL2114", nameof (DerivedFromAnnotatedDerivedFromBase), nameof (DAMField3))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public string DAMField3;

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			// shouldn't warn because we warn on the base getter instead
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			public override string DAMVirtualProperty { [Kept] get; }
		}

		[KeptMember (".ctor()")]
		public class BaseTypeOfNestedType
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on BaseTypeOfNestedType.RUCMethod--")]
			public void RUCMethod () { }
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
		// Warnings about base types of nested types are shown at the (outer) type level.
		[ExpectedWarning ("IL2113", nameof (BaseTypeOfNestedType), nameof (BaseTypeOfNestedType.RUCMethod))]
		class AnnotatedPublicNestedTypes
		{
			[KeptMember (".ctor()")]
			public class NestedType
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedType.RUCMethod--")]
				void RUCMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseTypeOfNestedType))]
			public class NestedTypeWithBase : BaseTypeOfNestedType
			{
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public class NestedAnnotatedType
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedAnnotatedType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedAnnotatedType.RUCMethod--")]
				void RUCMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode ("--RUC on NestedRUCType--")]
			public class NestedRUCType
			{
				[Kept]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType--")]
				public NestedRUCType () { }

				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType.RUCMethod--")]
				[RequiresUnreferencedCode ("--RUC on NestedRUCType.RUCMethod--")]
				void RUCMethod () { }

				[Kept]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType--")]
				void Method () { }

				[Kept]
				[ExpectedWarning ("IL2112", "--RUC on NestedRUCType--")]
				static void StaticMethod () { }
			}

			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", nameof (NestedRUCTypeWithDefaultConstructor) + "()", "--RUC on NestedRUCTypeWithDefaultConstructor--", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUC on NestedRUCTypeWithDefaultConstructor--")]
			public class NestedRUCTypeWithDefaultConstructor
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
		class AnnotatedBaseSharedByNestedTypes
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedBaseSharedByNestedTypes.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedBaseSharedByNestedTypes.RUCMethod--")]
			public void RUCMethod () { }
		}

		[KeptBaseType (typeof (AnnotatedBaseSharedByNestedTypes))]
		// Nested types that share the outer class base type can produce warnings about base methods of the annotated type.
		[ExpectedWarning ("IL2113", "--RUC on AnnotatedBaseSharedByNestedTypes.RUCMethod--")]
		class DerivedWithNestedTypes : AnnotatedBaseSharedByNestedTypes
		{

			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (AnnotatedBaseSharedByNestedTypes))]
			public class NestedType : AnnotatedBaseSharedByNestedTypes
			{
			}
		}

		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[RequiresUnreferencedCode ("--AnnotatedRUCPublicMethods--")]
		public class AnnotatedRUCPublicMethods
		{
			public AnnotatedRUCPublicMethods () { }

			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on AnnotatedRUCPublicMethods.RUCMethod--")]
			[RequiresUnreferencedCode ("--RUC on AnnotatedRUCPublicMethods.RUCMethod--")]
			public void RUCMethod () { }

			[Kept]
			[ExpectedWarning ("IL2112", "--AnnotatedRUCPublicMethods--")]
			public void Method () { }

			[Kept]
			[ExpectedWarning ("IL2112", "--AnnotatedRUCPublicMethods--")]
			public static void StaticMethod () { }
		}

		[Kept]
		class RUCOnNewSlotVirtualMethodDerivedAnnotated
		{
			[Kept]
			[KeptMember (".ctor()")]
			public class Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
				public virtual void RUCVirtualMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptBaseType (typeof (Base))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			[ExpectedWarning ("IL2113", "--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
			public class Derived : Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Derived.RUCVirtualMethod--")]
				// https://github.com/dotnet/linker/issues/2815
				[ExpectedWarning ("IL2112", "--RUCOnVirtualMethodDerivedAnnotated.Derived.RUCVirtualMethod--", ProducedBy = Tool.NativeAot)]
				public virtual void RUCVirtualMethod () { }
			}

			[Kept]
			static Derived _derivedInstance;

			[Kept]
			public static void Test ()
			{
				_derivedInstance = new Derived ();
				_derivedInstance.GetType ().RequiresPublicMethods ();
			}
		}

		[Kept]
		class RUCOnVirtualOnAnnotatedBase
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public class Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
				[ExpectedWarning ("IL2112", "--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
				public virtual void RUCVirtualMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			public class Derived : Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Derived.RUCVirtualMethod--")]
				public override void RUCVirtualMethod () { }
			}

			[Kept]
			static Base _baseInstance;

			[Kept]
			public static void Test ()
			{
				_baseInstance = new Derived ();
				_baseInstance.GetType ().RequiresPublicMethods ();
			}
		}

		[Kept]
		class RUCOnVirtualOnAnnotatedBaseUsedByDerived
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public class Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
				// https://github.com/dotnet/runtime/issues/86580
				// Compare to the case above - the only difference is the type of the field
				// and it causes different warnings to be produced.
				// [ExpectedWarning ("IL2112", "--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--")]
				public virtual void RUCVirtualMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			// https://github.com/dotnet/runtime/issues/86580
			[ExpectedWarning ("IL2113", "--RUCOnVirtualMethodDerivedAnnotated.Base.RUCVirtualMethod--", ProducedBy = Tool.Trimmer)]
			public class Derived : Base
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode ("--RUCOnVirtualMethodDerivedAnnotated.Derived.RUCVirtualMethod--")]
				public override void RUCVirtualMethod () { }
			}

			[Kept]
			static Derived _baseInstance;

			[Kept]
			public static void Test ()
			{
				_baseInstance = new Derived ();
				_baseInstance.GetType ().RequiresPublicMethods ();
			}
		}

		[Kept]
		class UseByDerived
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptMember (".ctor()")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresDynamicCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
				[RequiresUnreferencedCode ("--AnnotatedBase.VirtualMethodWithRequires--")]
				[RequiresDynamicCode ("--AnnotatedBase.VirtualMethodWithRequires--")]
				[RequiresAssemblyFiles ("--AnnotatedBase.VirtualMethodWithRequires--")]
				public virtual void VirtualMethodWithRequires () { }
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			// https://github.com/dotnet/runtime/issues/86580
			[ExpectedWarning ("IL2113", "--AnnotatedBase.VirtualMethodWithRequires--", ProducedBy = Tool.Trimmer)]
			class Derived : AnnotatedBase
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresDynamicCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
				[ExpectedWarning ("IL2112", "--Derived.MethodWithRequires--")]
				[RequiresUnreferencedCode ("--Derived.MethodWithRequires--")]
				// Currently we decided to not warn on RDC and RAF due to type hierarchy marking
				[RequiresDynamicCode ("--Derived.MethodWithRequires--")]
				[RequiresAssemblyFiles ("--Derived.MethodWithRequires--")]
				public static void MethodWithRequires () { }

				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresDynamicCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
				[RequiresUnreferencedCode ("--Derived.VirtualMethodWithRequires--")]
				[RequiresDynamicCode ("--Derived.VirtualMethodWithRequires--")]
				[RequiresAssemblyFiles ("--Derived.VirtualMethodWithRequires--")]
				public override void VirtualMethodWithRequires () { }
			}

			[Kept]
			static void TestMethodOnDerived (Derived instance)
			{
				instance.GetType ().GetMethod ("MethodWithRequires");
			}

			[Kept]
			[KeptMember (".ctor()")]
			class BaseWithRequires
			{
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresDynamicCodeAttribute))]
				[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
				[RequiresUnreferencedCode ("--Base.MethodWithRequires--")]
				[RequiresDynamicCode ("--Base.MethodWithRequires--")]
				[RequiresAssemblyFiles ("--Base.MethodWithRequires--")]
				public static void MethodWithRequires () { }
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptBaseType (typeof (BaseWithRequires))]
			[KeptMember (".ctor()")]
			[ExpectedWarning ("IL2113", "--Base.MethodWithRequires--")]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedDerived : BaseWithRequires
			{
			}

			[Kept]
			static void TestMethodOnBase (AnnotatedDerived instance)
			{
				instance.GetType ().GetMethod (nameof (BaseWithRequires.MethodWithRequires));
			}

			[Kept]
			public static void Test ()
			{
				TestMethodOnDerived (new Derived ());
				TestMethodOnBase (new AnnotatedDerived ());
			}
		}

		[Kept]
		class CompilerGeneratedBackingField
		{
			[Kept]
			public class BaseWithField
			{
				[KeptBackingField]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type CompilerGeneratedProperty { get; set; }
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[KeptBaseType (typeof (BaseWithField))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)]
			[ExpectedWarning ("IL2115", nameof (BaseWithField), nameof (BaseWithField.CompilerGeneratedProperty))]
			public class DerivedWithAnnotation : BaseWithField
			{
			}

			[Kept]
			static DerivedWithAnnotation derivedInstance;

			[Kept]
			public static void Test ()
			{
				derivedInstance.GetType ().RequiresNonPublicFields ();
			}
		}
	}
}
