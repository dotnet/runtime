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
	public class ObjectGetType
	{
		public static void Main ()
		{
			SealedType.Test ();
			UnsealedType.Test ();
			TypeOfGenericParameters.Test ();

			BasicAnnotationWithNoDerivedClasses.Test ();
			MultipleValuesWithAnnotations.Test (0);
			MultipleValuesWithAnnotations.Test (1);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (0);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (1);
			MultipleValuesWithAndWithoutAnnotationsWarns.Test (2);

			SingleDerivedWithAnnotatedParent.Test ();
			DerivedWithAnnotationOnDerived.Test ();
			DerivedWithBaseAndAnnotatedInterface.Test ();
			DeepHierarchy.Test ();
			DeepInterfaceHierarchy.Test ();

			ConstructorAsSource.Test ();

			InterfaceSeenFirst.Test ();
			AnnotationsRequestedOnImplementation.Test ();
			AnnotationsRequestedOnInterface.Test ();

			AllAnnotationsAreApplied.Test ();
			SealedWithAnnotation.Test ();

			DiamondShapeWithUnannotatedInterface.Test ();
			DiamondShapeWithAnnotatedInterface.Test ();

			ApplyingAnnotationIntroducesTypesToApplyAnnotationTo.Test ();
			ApplyingAnnotationIntroducesTypesToApplyAnnotationToViaInterfaces.Test ();
			ApplyingAnnotationIntroducesTypesToApplyAnnotationToMultipleAnnotations.Test ();
			ApplyingAnnotationIntroducesTypesToApplyAnnotationToEntireType.Test ();

			EnumerationOverInstances.Test ();

			DataFlowUnusedGetType.Test ();

			NullValue.Test ();
			NoValue.Test ();
			UnknownValue.Test ();

			PrivateMembersOnBaseTypesAppliedToDerived.Test ();

			IsInstOf.Test ();

			UsedByDerived.Test ();
		}

		[Kept]
		class SealedType
		{
			[Kept]
			static SealedClass s_sealedClassField;

			[Kept]
			sealed class SealedClass
			{
				[Kept]
				public SealedClass () { }

				[Kept]
				public static void Method () { }

				public static void UnusedMethod () { }
			}

			[Kept]
			public static void Test ()
			{
				s_sealedClassField = new SealedClass ();
				s_sealedClassField.GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class UnsealedType
		{
			[Kept]
			static UnsealedClass s_unsealedClassField;

			[Kept]
			class UnsealedClass
			{
				[Kept]
				public UnsealedClass () { }

				public static void Method () { }
			}

			[Kept]
			[ExpectedWarning ("IL2075", "GetMethod")]
			public static void Test ()
			{
				s_unsealedClassField = new UnsealedClass ();

				// GetType call on an unsealed type is not recognized and produces a warning
				s_unsealedClassField.GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class TypeOfGenericParameters
		{
			[Kept]
			class MethodWithRequirementsTest
			{
				[Kept]
				[KeptMember (".ctor()")]
				class TestType
				{
					[Kept] // Due to the annotation on the generic parameter
					public static void PublicMethod () { }
				}

				[Kept]
				[ExpectedWarning ("IL2075", "GetMethod")]
				static void MethodWithRequirements<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TWithMethods> (TWithMethods instance)
				{
					instance.GetType ().GetMethod ("PublicMethod");
				}

				[Kept]
				public static void Test ()
				{
					MethodWithRequirements<TestType> (new TestType ());
				}
			}

			[Kept]
			class MethodWithRequirementsAndDerivedTypeTest
			{
				[Kept]
				[KeptMember (".ctor()")]
				class Base
				{
					[Kept]
					public static void PublicMethodOnBase () { }
				}

				[Kept]
				[KeptBaseType (typeof (Base))]
				[KeptMember (".ctor()")]
				class Derived : Base
				{
					// This should not be kept
					public static void PublicMethodOnDerived () { }
				}

				[Kept]
				[ExpectedWarning ("IL2075", "GetMethod")]
				static void MethodWithRequirements<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				TWithMethods>
					(TWithMethods instance)
				{
					instance.GetType ().GetMethod ("PublicMethod");
				}

				[Kept]
				public static void Test ()
				{
					MethodWithRequirements<Base> (new Derived ());
				}
			}

			[Kept]
			class GenericWithRequirements
			{
				[Kept]
				[KeptMember (".ctor()")]
				class TestType
				{
				}

				[Kept]
				[KeptMember (".cctor()")]
				class Generic<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				TWithMethods> where TWithMethods : new()
				{
					[Kept]
					static TWithMethods ReturnAnnotated () { return new TWithMethods (); }

					[Kept]
					static TWithMethods _fieldAnnotated = new TWithMethods ();

					[Kept]
					[ExpectedWarning ("IL2075", "GetMethod")]
					public static void TestReturn ()
					{
						ReturnAnnotated ().GetType ().GetMethod ("Test");
					}

					[Kept]
					[ExpectedWarning ("IL2075", "GetMethod")]
					public static void TestField ()
					{
						_fieldAnnotated.GetType ().GetMethod ("Test");
					}
				}

				[Kept]
				public static void Test ()
				{
					Generic<TestType>.TestReturn ();
					Generic<TestType>.TestField ();
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithRequirementsTest.Test ();
				MethodWithRequirementsAndDerivedTypeTest.Test ();
				GenericWithRequirements.Test ();
			}
		}

		[Kept]
		class BasicAnnotationWithNoDerivedClasses
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IBasicAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			class ClassImplementingAnnotatedInterface : IBasicAnnotatedInterface
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestInterface (ClassImplementingAnnotatedInterface classImplementingInterface)
			{
				// The interface is not referred to anywhere, so it will be trimmed
				// but its annotation still applies
				classImplementingInterface.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class BasicAnnotatedClass
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestClass (BasicAnnotatedClass instance)
			{
				instance.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			struct BasicAnnotatedStruct
			{
				// Handle boxing and unboxing operations
				// https://github.com/dotnet/linker/issues/1951
				// [Kept]
				public void UsedMethod () { }
				public void UnusedMethod () { }
			}

			[Kept]
			// https://github.com/dotnet/linker/issues/1951
			// This should not warn
			[ExpectedWarning ("IL2075", "GetMethod")]
			static void TestStruct (BasicAnnotatedStruct instance)
			{
				instance.GetType ().GetMethod ("UsedMethod");
			}

			[Kept]
			public static void Test ()
			{
				TestInterface (new ClassImplementingAnnotatedInterface ());
				TestClass (new BasicAnnotatedClass ());
				TestStruct (new BasicAnnotatedStruct ());
			}
		}

		[Kept]
		class MultipleValuesWithAnnotations
		{
			[Kept]
			sealed class SealedClass
			{
				[Kept]
				public SealedClass () { }

				[Kept]
				public void UsedMethod () { }

				public static void UnusedMethod () { }

				[Kept]
				public static SealedClass Instance () => new SealedClass ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedClass
			{
				[Kept]
				public AnnotatedClass () { }

				[Kept]
				public void UsedMethod () { }

				[Kept] // Type is not sealed, so the annotation is applied instead
				public static void UnusedMethod () { }

				[Kept]
				public static AnnotatedClass Instance () => new AnnotatedClass ();
			}

			[Kept]
			public static void Test (int param)
			{
				object instance;
				switch (param) {
				case 0:
					instance = SealedClass.Instance ();
					break;
				default:
					instance = AnnotatedClass.Instance ();
					break;
				}

				instance.GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class MultipleValuesWithAndWithoutAnnotationsWarns
		{
			[Kept]
			sealed class SealedClass
			{
				[Kept]
				public SealedClass () { }

				[Kept]
				public void UsedMethod () { }

				public static void UnusedMethod () { }

				[Kept]
				public static SealedClass Instance () => new SealedClass ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedClass
			{
				[Kept]
				public AnnotatedClass () { }

				[Kept]
				public void UsedMethod () { }

				[Kept] // Type is not sealed, so the annotation is applied instead
				public static void UnusedMethod () { }

				[Kept]
				public static AnnotatedClass Instance () => new AnnotatedClass ();
			}

			[Kept]
			class UnannotatedClass
			{
				[Kept]
				public UnannotatedClass () { }

				public void UsedMethod () { }

				public static void UnusedMethod () { }

				[Kept]
				public static UnannotatedClass Instance () => new UnannotatedClass ();
			}

			[Kept]
			[ExpectedWarning ("IL2075", "GetMethod")]
			public static void Test (int param)
			{
				object instance;
				switch (param) {
				case 0:
					instance = SealedClass.Instance ();
					break;
				case 1:
					instance = AnnotatedClass.Instance ();
					break;
				default:
					instance = UnannotatedClass.Instance ();
					break;
				}

				instance.GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class SingleDerivedWithAnnotatedParent
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class HierarchyAnnotatedParentClass
			{
			}

			[Kept]
			[KeptBaseType (typeof (HierarchyAnnotatedParentClass))]
			class HierarchyAnnotatedParentClassChild : HierarchyAnnotatedParentClass
			{
				[Kept]
				public HierarchyAnnotatedParentClassChild () { }

				[Kept]
				public void UsedMethod () { }

				[Kept] // Marked through annotations
				public static void UnusedMethod () { }

				[Kept]
				public static HierarchyAnnotatedParentClassChild Instance () => new HierarchyAnnotatedParentClassChild ();
			}

			[Kept]
			public static void Test ()
			{
				HierarchyAnnotatedParentClassChild.Instance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DerivedWithAnnotationOnDerived
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.None)]
			class Base
			{
				[Kept]
				public virtual void Method () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class Derived : Base
			{
				[Kept]
				public override void Method () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Derived))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			class MostDerived : Derived
			{
				[Kept]
				public override void Method () { }

				[Kept]
				private void PrivateMethod () { }
			}

			[Kept]
			static MostDerived GetInstance () => new MostDerived ();

			[Kept]
			public static void Test ()
			{
				GetInstance ().GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class DerivedWithBaseAndAnnotatedInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IHierarchyAnnotatedParentInterface
			{
			}

			[Kept]
			[KeptInterface (typeof (IHierarchyAnnotatedParentInterface))]
			[KeptMember (".ctor()")]
			class HierarchyAnnotatedParentInterfaceParent : IHierarchyAnnotatedParentInterface
			{
			}

			[Kept]
			[KeptBaseType (typeof (HierarchyAnnotatedParentInterfaceParent))]
			[KeptMember (".ctor()")]
			class HierarchyAnnotatedParentInterfaceChild : HierarchyAnnotatedParentInterfaceParent
			{
				[Kept]
				public void UsedMethod () { }

				[Kept] // Marked through annotations
				public static void UnusedMethod () { }

				[Kept]
				public static HierarchyAnnotatedParentInterfaceChild Instance () => new HierarchyAnnotatedParentInterfaceChild ();
			}

			[Kept]
			public static void Test ()
			{
				// Reference the interface directly so that it's preserved
				var a = typeof (IHierarchyAnnotatedParentInterface);
				HierarchyAnnotatedParentInterfaceChild.Instance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DeepHierarchy
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class BaseClass
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseClass))]
			class DerivedClass1 : BaseClass
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (DerivedClass1))]
			class DerivedClass2 : DerivedClass1
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (DerivedClass2))]
			class DerivedClass3 : DerivedClass2
			{
				[Kept]
				public void UsedMethod () { }
				[Kept]
				public void UnusedMethod () { }
			}

			[Kept]
			static DerivedClass1 GetInstance () => new DerivedClass3 ();

			[Kept]
			public static void Test ()
			{
				GetInstance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class DeepInterfaceHierarchy
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class BaseImplementingInterface : IAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseImplementingInterface))]
			class Derived1 : BaseImplementingInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Derived1))]
			class Derived2 : Derived1
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Derived2))]
			class Derived3 : Derived2
			{
				[Kept]
				public void UsedMethod () { }

				[Kept]
				public void UnusedMethod () { }
			}

			[Kept]
			static Derived1 GetInstance () => new Derived3 ();

			[Kept]
			public static void Test ()
			{
				var a = typeof (IAnnotatedInterface); // Preserve the interface
				GetInstance ().GetType ().GetMethod ("UsedMethod");
			}
		}

		[Kept]
		class ConstructorAsSource
		{
			[Kept]
			[KeptMember (".ctor()")]
			public class Base
			{

			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			public class Derived : Base
			{
				// new() doesn't propagate static type
				// https://github.com/dotnet/linker/issues/1952
				// [Kept]
				public void Method () { }
			}

			[Kept]
			[ExpectedWarning ("IL2075", "GetMethod")]
			public static void Test ()
			{
				new Derived ().GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class InterfaceSeenFirst
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept]
				void InterfaceMethod ();
			}

			[Kept]
			class FirstImplementationClass : IAnnotatedInterface
			{
				public void InterfaceMethod () { }

				[Kept]
				public static void Do () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			static ImplementationClass GetInstance () => new ImplementationClass ();

			[Kept]
			public static void Test ()
			{
				// This is to force marking of a type which implements the interface in question
				FirstImplementationClass.Do ();
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);

				// Now force walk of the annotations for the ImplementationClass
				// At this point the interface should already have been processed - which is the point of this test
				// that it reuses the already processed info correctly.
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AnnotationsRequestedOnImplementation
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept] // Kept because it's implemented on the class
				void InterfaceMethod ();

				// Annotation will not be applied to the interface, since nothing
				// asked for it via reflection.
				static void DoSomething () { }

				void DefaultInterfaceMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				// Annotation will be applied to the implementation type which the reflection
				// asked for
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (ImplementationClass))]
			class Derived : ImplementationClass
			{
				// Annotation will be applied to a derived type as well
				[Kept]
				public void NewMethod () { }
			}

			[Kept]
			static ImplementationClass GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AnnotationsRequestedOnInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			interface IAnnotatedInterface
			{
				[Kept] // Kept because it's implemented on the class
				void InterfaceMethod ();

				// Annotation applied to the interface since that's what reflection asked about
				[Kept]
				static void DoSomething () { }

				[Kept]
				void DefaultInterfaceMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementationClass : IAnnotatedInterface
			{
				[Kept]
				void IAnnotatedInterface.InterfaceMethod ()
				{
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (ImplementationClass))]
			class Derived : ImplementationClass
			{
				[Kept]
				public void NewMethod () { }
			}

			[Kept]
			static IAnnotatedInterface GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				// Make sure the interface is kept
				var i = typeof (IAnnotatedInterface);
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class AllAnnotationsAreApplied
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			interface IMethodsAnnotatedInterface
			{
				[Kept]
				void InterfaceMethod ();
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			interface IPropertiesAnnotatedInterface
			{
				bool Property { get; }
			}

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
			interface IEventsAnnotatedInterface
			{
				[Kept]
				[KeptEventAddMethod]
				[KeptEventRemoveMethod]
				event EventHandler MyEvent;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IMethodsAnnotatedInterface))]
			[KeptInterface (typeof (IEventsAnnotatedInterface))]
			class ImplementationClass : IMethodsAnnotatedInterface, IPropertiesAnnotatedInterface, IEventsAnnotatedInterface
			{
				[Kept]
				public bool Property { [Kept] get => false; }

				[Kept]
				[KeptBackingField]
				public int AnotherProperty { [Kept] get; }

				[Kept]
				[KeptEventAddMethod]
				[KeptEventRemoveMethod]
				[KeptBackingField]
				public event EventHandler MyEvent;

				[Kept]
				public void InterfaceMethod () { }

				[Kept]
				public void AnotherMethod () { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementationClass))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			class DerivedClassWithCtors : ImplementationClass
			{
				[Kept]
				public DerivedClassWithCtors () { }

				[Kept] // Annotation is applied even if reflection didn't ask for it explicitly
				public DerivedClassWithCtors (int i) { }

				private DerivedClassWithCtors (string s) { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementationClass))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			[KeptMember (".ctor()")]
			class DerivedClassWithPrivateMethods : ImplementationClass
			{
				[Kept] // Kept due to annotation on the interface
				public void PublicMethod () { }

				[Kept] // Kept due to annotation on this class
				private void PrivateMethod () { }
			}

			[Kept]
			static IMethodsAnnotatedInterface GetMethodsInstance () => new ImplementationClass ();

			[Kept]
			static IEventsAnnotatedInterface GetEventsInstance () => new ImplementationClass ();

			[Kept]
			public static void Test ()
			{
				// Instantiate the derived classes so that they're preserved - this should not do anything with annotations
				var withCtors = new DerivedClassWithCtors ();
				var withPrivateMethods = new DerivedClassWithPrivateMethods ();

				// The reflection is only asking about IMethodsAnnotatedInterface
				// and only needs methods, but for now we will apply annotations
				// from the entire hierarchy - so even properties should be marked
				// Note that the IPropertiesAnnotatedInterface is not actually going to be kept
				// but its annotations still apply
				GetMethodsInstance ().GetType ().GetMethod ("InterfaceMethod");

				// Ask again on a different interface - same type impacted
				GetEventsInstance ().GetType ().GetEvent ("MyEvent");
			}
		}

		[Kept]
		class SealedWithAnnotation
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			sealed class SealedDerived : AnnotatedBase
			{
				// This is preserved because the exact type is used and the method is directly found on it
				[Kept]
				private void PrivateMethod () { }

				// Annotation is not applied, because the reflection can be solved without using it
				public void PublicMethod () { }
			}

			[Kept]
			static SealedDerived GetInstance () => new SealedDerived ();

			[Kept]
			public static void Test ()
			{
				// Explicitly ask for the private method
				GetInstance ().GetType ().GetMethod ("PrivateMethod", System.Reflection.BindingFlags.NonPublic);
			}
		}

		[Kept]
		class DiamondShapeWithUnannotatedInterface
		{
			[Kept]
			interface ICommon
			{
				// Not kept as there's no reference to the interface method
				// Only the implementations are marked, but there's no reason to mark the method on the interface
				void InterfaceMethod ();
			}

			[Kept]
			[KeptInterface (typeof (ICommon))]
			[KeptMember (".ctor()")]
			class ImplementsCommonInterface : ICommon
			{
				[Kept]
				public virtual void InterfaceMethod () { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementsCommonInterface))]
			[KeptInterface (typeof (ICommon))]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class DerivedAndImplementsSameInterface : ImplementsCommonInterface, ICommon
			{
			}

			[Kept]
			[KeptBaseType (typeof (DerivedAndImplementsSameInterface))]
			[KeptMember (".ctor()")]
			class MostDerived : DerivedAndImplementsSameInterface
			{
				[Kept]
				public override void InterfaceMethod () { }
			}

			[Kept]
			static DerivedAndImplementsSameInterface GetInstance () => new MostDerived ();

			[Kept]
			public static void Test ()
			{
				var i = typeof (ICommon); // Explicitely keep the interface (otherwise trimming tools would remove it as it's not used)
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class DiamondShapeWithAnnotatedInterface
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			interface IAnnotatedCommon
			{
				[Kept] // Due to the annotation
				void InterfaceMethod ();
			}

			[Kept]
			[KeptInterface (typeof (IAnnotatedCommon))]
			[KeptMember (".ctor()")]
			class ImplementsCommonInterface : IAnnotatedCommon
			{
				[Kept]
				public virtual void InterfaceMethod () { }
			}

			[Kept]
			[KeptBaseType (typeof (ImplementsCommonInterface))]
			[KeptInterface (typeof (IAnnotatedCommon))]
			[KeptMember (".ctor()")]
			class DerivedAndImplementsSameInterface : ImplementsCommonInterface, IAnnotatedCommon
			{
			}

			[Kept]
			[KeptBaseType (typeof (DerivedAndImplementsSameInterface))]
			[KeptMember (".ctor()")]
			class MostDerived : DerivedAndImplementsSameInterface
			{
				[Kept]
				public override void InterfaceMethod () { }
			}

			[Kept]
			static IAnnotatedCommon GetInstance () => new MostDerived ();

			[Kept]
			public static void Test ()
			{
				GetInstance ().GetType ().GetMethod ("InterfaceMethod");
			}
		}

		[Kept]
		class ApplyingAnnotationIntroducesTypesToApplyAnnotationTo
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)]
			class AnnotatedBase
			{
			}

			[Kept]
			interface IUnannotatedInterface
			{
			}

			interface IUnusedInterface
			{
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			class Derived : AnnotatedBase
			{
				[Kept]
				[KeptMember (".ctor()")]
				[KeptBaseType (typeof (AnnotatedBase))]
				public class NestedDerived : AnnotatedBase
				{
					[Kept]
					[KeptBaseType (typeof (NestedDerived))]
					[KeptMember (".ctor()")]
					public class DeepNestedDerived : NestedDerived
					{
						[Kept] // Marked due to the annotation
						[KeptMember (".ctor()")]
						public class DeepNestedChild
						{
						}

						[Kept] // Marked due to the annotation
						[KeptMember (".ctor()")]
						private class DeepNestedPrivateChild
						{
						}
					}

					[Kept] // Marked due to the annotation
					[KeptInterface (typeof (IUnannotatedInterface))]
					[KeptMember (".ctor()")]
					public class NestedChild : IUnannotatedInterface
					{
					}
				}

				// Not used - not marked
				private class PrivateNested : IUnusedInterface
				{
				}
			}

			[Kept]
			static AnnotatedBase GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				var t = typeof (IUnannotatedInterface);
				GetInstance ().GetType ().GetNestedTypes ();
			}
		}

		[Kept]
		class ApplyingAnnotationIntroducesTypesToApplyAnnotationToViaInterfaces
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			interface IAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class ImplementsInterface : IAnnotatedInterface
			{
				[Kept]
				public FieldTypeAlsoImplementsInterface _publicFieldWithInterface;

				UnusedFieldTypeImplementsInterface _privateFieldWithInterface;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IAnnotatedInterface))]
			class FieldTypeAlsoImplementsInterface : IAnnotatedInterface
			{
				[Kept]
				public FieldTypeAlsoImplementsInterface _selfReference;

				[Kept]
				public NestedFieldType _nestedField;

				[Kept]
				public class NestedFieldType
				{
				}
			}

			class UnusedFieldTypeImplementsInterface : IAnnotatedInterface
			{
			}

			[Kept]
			static IAnnotatedInterface GetInstance () => new ImplementsInterface ();

			[Kept]
			public static void Test ()
			{
				var t = new FieldTypeAlsoImplementsInterface (); // Instantiate the type so that it gets interfaces
				GetInstance ().GetType ().GetFields ();
			}
		}

		[Kept]
		class ApplyingAnnotationIntroducesTypesToApplyAnnotationToMultipleAnnotations
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class MethodAnnotatedBase
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (MethodAnnotatedBase))]
			class DerivedFromMethodsBase : MethodAnnotatedBase
			{
				[Kept]
				public AnotherMethodsDerived PublicMethod () { return null; }

				void PrivateMethod () { }
			}

			[Kept]
			[KeptBaseType (typeof (MethodAnnotatedBase))]
			class AnotherMethodsDerived : MethodAnnotatedBase
			{
				[Kept]
				public static void PublicStaticMethod (DerivedFromPropertiesBase p) { }

				static void PrivateStaticMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			class PropertiesAnnotatedBase
			{
			}

			[Kept]
			[KeptBaseType (typeof (PropertiesAnnotatedBase))]
			class DerivedFromPropertiesBase : PropertiesAnnotatedBase
			{
				[Kept]
				public static AnotherPropertiesDerived PublicProperty { [Kept] get => null; }

				private static UnusedType PrivateProperty { get => null; }
			}

			[Kept]
			[KeptBaseType (typeof (PropertiesAnnotatedBase))]
			class AnotherPropertiesDerived : PropertiesAnnotatedBase
			{
				[Kept]
				public static UsedType PublicProperty { [Kept] get => null; }

				private static UnusedType PrivateProperty { get => null; }
			}

			[Kept]
			class UsedType { }

			class UnusedType { }

			[Kept]
			static MethodAnnotatedBase GetMethodsInstance () => new DerivedFromMethodsBase ();

			[Kept]
			static PropertiesAnnotatedBase GetPropertiesInstance () => new PropertiesAnnotatedBase ();

			[Kept]
			public static void Test ()
			{
				GetMethodsInstance ().GetType ().GetMethods ();

				// Note that the DerivedFromPropertiesBase type is not referenced any other way then through
				// the PublicStaticMethod parameter which is only kept due to the hierarchy walk of the MethodAnnotatedBase.
				// This is to test that hierarchy walking of one annotation type tree will correctly mark/cache things
				// for a different annotatin tree.
				GetPropertiesInstance ().GetType ().GetProperties ();
			}
		}

		[Kept]
		class ApplyingAnnotationIntroducesTypesToApplyAnnotationToEntireType
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			class AnnotatedBase
			{
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			class Derived : AnnotatedBase
			{
				[Kept]
				public void Method () { }

				[Kept]
				[KeptMember (".ctor()")]
				[KeptInterface (typeof (INestedInterface))]
				class Nested : INestedInterface
				{
					[Kept]
					public void InterfaceMethod () { }
				}

				[Kept]
				public AnotherAnnotatedType PublicProperty { [Kept] get => null; }

				[Kept]
				[KeptMember (".ctor()")]
				[KeptBaseType (typeof (AnnotatedBase))]
				class NestedAnnotatedType : AnnotatedBase
				{
					[Kept]
					int _field;
				}

				[Kept]
				int _field;
			}

			[Kept]
			interface INestedInterface
			{
				[Kept]
				void InterfaceMethod ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (AnnotatedBase))]
			class AnotherAnnotatedType : AnnotatedBase
			{
				[Kept]
				int _field;
			}

			[Kept]
			interface InterfaceImplementedByDerived
			{
				[Kept]
				void Method ();
			}

			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptInterface (typeof (InterfaceImplementedByDerived))]
			class DerivedWithInterface : AnnotatedBase, InterfaceImplementedByDerived
			{
				[Kept]
				public void Method () { }
			}

			[Kept]
			static AnnotatedBase GetInstance () => new Derived ();

			[Kept]
			public static void Test ()
			{
				Type t = GetInstance ().GetType ();
				t.RequiresAll ();
				var t2 = typeof (DerivedWithInterface);
			}
		}

		[Kept]
		class EnumerationOverInstances
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
			}

			[Kept]
			[KeptBaseType (typeof (AnnotatedBase))]
			[KeptMember (".ctor()")]
			class Derived : AnnotatedBase
			{
				// https://github.com/dotnet/linker/issues/2027
				// [Kept]
				public void Method () { }
			}

			[Kept]
			static IEnumerable<AnnotatedBase> GetInstances () => new AnnotatedBase[] { new Derived () };

			[Kept]
			// https://github.com/dotnet/linker/issues/2027
			[ExpectedWarning ("IL2075", nameof (Type.GetType))]
			public static void Test ()
			{
				foreach (var instance in GetInstances ()) {
					instance.GetType ().GetMethod ("Method");
				}
			}
		}

		[Kept]
		class DataFlowUnusedGetType
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedType
			{
				[RequiresUnreferencedCode ("AnnotatedType.Method")]
				public void Method () { }
			}

			[Kept]
			static AnnotatedType GetInstance () => new AnnotatedType ();

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
			}

			class DerivedFromAnnotatedBase : AnnotatedBase
			{
				[RequiresUnreferencedCode ("DerivedFromAnnotatedBase.Method")]
				public void Method () { }
			}

			[Kept]
			static AnnotatedBase GetBaseInstance () => new AnnotatedBase ();

			[Kept]
			public static void Test ()
			{
				// Call GetType, but don't use it for any data flow related stuff (no reflection or annotations)
				// trimming tools have an optimization which avoids marking the type for type hierarchy annotations in this case
				var name = GetInstance ().GetType ().Name;

				// Using GetType and isinst should not mark the type or apply the annotation either
				// This is a specific test for a pattern we want the trimming tools to trim correctly
				// that is the type which is only referenced in the isinst in a condition like this should not be kept
				if (GetBaseInstance ().GetType () is DerivedFromAnnotatedBase) {
					Console.WriteLine ("Never get here");
				}
			}
		}

		[Kept]
		class NullValue
		{
			[Kept]
			class TestType
			{
			}

			[Kept]
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			public static void Test ()
			{
				TestType nullInstance = null;
				// Even though this throws at runtime, we warn about the return value of GetType
				nullInstance.GetType ().RequiresAll ();
			}
		}

		[Kept]
		class NoValue
		{
			[Kept]
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			public static void Test ()
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
				// Even though the above throws at runtime, we warn about the return value of GetType
				noValue.GetType ().RequiresAll ();
			}
		}

		[Kept]
		class UnknownValue
		{
			[Kept]
			[KeptMember (".ctor()")]
			class TestType
			{
			}

			[Kept]
			static TestType GetInstance () => new TestType ();

			[Kept]
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			public static void Test ()
			{
				TestType unknownValue = GetInstance ();
				// Should warn about the return value of GetType
				unknownValue.GetType ().RequiresAll ();
			}
		}

		[Kept]
		class PrivateMembersOnBaseTypesAppliedToDerived
		{
			[Kept]
			[KeptMember (".ctor()")]
			class SuperBase
			{
				// Is not kept - correct
				private void SuperBasePrivateMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (SuperBase))]
			class Base : SuperBase
			{
				// https://github.com/dotnet/linker/issues/2813
				// This doesn't need to be preserved - non-public annotations don't apply to base types
				[Kept]
				private void BasePrivateMethod () { }
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)]
			class Derived : Base
			{
				[Kept]
				private void DerivedPrivateMethod () { }
			}

			[Kept]
			static Derived _instance;

			[Kept]
			[ExpectedWarning ("IL2072", "System.Type.BaseType.get")]
			public static void Test ()
			{
				_instance = new Derived ();
				Type type = _instance.GetType ();

				// This should work - the statically typed _instance (Derived) has NonPublicMethods annotation
				type.RequiresNonPublicMethods ();

				// This should warn - non-public annotations don't propagate to base types
				type.BaseType.RequiresNonPublicMethods ();
			}
		}

		[Kept]
		class IsInstOf
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			class Target
			{
			}

			[Kept]
			// https://github.com/dotnet/linker/issues/2819
			[ExpectedWarning ("IL2072", ProducedBy = Tool.Trimmer)]
			static void TestIsInstOf (object o)
			{
				if (o is Target t) {
					t.GetType ().RequiresPublicParameterlessConstructor ();
				}
			}

			[Kept]
			public static void Test ()
			{
				var target = new Target ();
				TestIsInstOf (target);
			}
		}

		[Kept]
		class UsedByDerived
		{
			class AnnotatedBase
			{
				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				class Base
				{
					[Kept]
					public void Method () { }
				}

				[Kept]
				[KeptBaseType (typeof (Base))]
				class Derived : Base
				{
				}

				[Kept]
				static Derived derivedInstance;

				[Kept]
				public static void Test ()
				{
					derivedInstance.GetType ().RequiresPublicMethods ();
				}
			}

			class AnnotatedDerived
			{
				[Kept]
				class Base
				{
					[Kept]
					public void Method () { }
				}

				[Kept]
				[KeptBaseType (typeof (Base))]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				class Derived : Base
				{
				}

				[Kept]
				static Derived derivedInstance;

				[Kept]
				public static void Test ()
				{
					derivedInstance.GetType ().RequiresPublicMethods ();
				}
			}

			class AnnotatedInterface
			{
				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
				interface IBase
				{
					[Kept]
					public void Method () { }
				}

				[Kept]
				[KeptMember (".ctor()")]
				[KeptInterface (typeof (IBase))]
				class Implementation : IBase
				{
				}

				[Kept]
				static Implementation implementationInstance;

				[Kept]
				public static void Test ()
				{
					implementationInstance = new Implementation ();
					var a = implementationInstance as IBase;
					implementationInstance.GetType ().RequiresPublicMethods ();
				}
			}

			class AnnotatedImplementation
			{
				[Kept]
				interface IBase
				{
					[Kept]
					public void Method () { }
				}

				[Kept]
				[KeptMember (".ctor()")]
				[KeptInterface (typeof (IBase))]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
				class Implementation : IBase
				{
				}

				[Kept]
				static Implementation implementationInstance;

				[Kept]
				public static void Test ()
				{
					implementationInstance = new Implementation ();
					var a = implementationInstance as IBase;
					implementationInstance.GetType ().RequiresPublicMethods ();
				}
			}

			[Kept]
			public static void Test ()
			{
				AnnotatedBase.Test (); ;
				AnnotatedDerived.Test ();
				AnnotatedInterface.Test ();
				AnnotatedImplementation.Test ();
			}
		}
	}
}
