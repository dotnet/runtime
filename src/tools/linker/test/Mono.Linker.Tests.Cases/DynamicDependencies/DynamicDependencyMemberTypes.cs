using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	class DynamicDependencyMemberTypes
	{
		public static void Main ()
		{
			B.Method ();
		}

		static class B
		{
			[Kept]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof (TypeWithAutoImplementedPublicParameterlessConstructor))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof (TypeWithPublicParameterlessConstructor))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicConstructors, typeof (TypeWithPublicConstructor))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof (TypeWithNonPublicConstructor))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicMethods, typeof (TypeWithPublicMethod))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicMethods, typeof (TypeWithNonPublicMethod))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (TypeWithPublicField))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicFields, typeof (TypeWithNonPublicField))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof (TypeWithPublicNestedType))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicNestedTypes, typeof (TypeWithNonPublicNestedType))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicProperties, typeof (TypeWithPublicProperty))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicProperties, typeof (TypeWithNonPublicProperty))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicEvents, typeof (TypeWithPublicEvent))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicEvents, typeof (TypeWithNonPublicEvent))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.All, typeof (TypeWithAll))]
			[DynamicDependency (DynamicallyAccessedMemberTypes.None, typeof (TypeWithNone))]
			public static void Method ()
			{
			}
		}


		[KeptMember (".ctor()")]
		class TypeWithAutoImplementedPublicParameterlessConstructor
		{
			public void Method () { }

			public int field;
		}

		class TypeWithPublicParameterlessConstructor
		{
			[Kept]
			public TypeWithPublicParameterlessConstructor () { }

			public TypeWithPublicParameterlessConstructor (int i) { }
		}

		class TypeWithPublicConstructor
		{
			[Kept]
			public TypeWithPublicConstructor (int i) { }

			[Kept]
			public TypeWithPublicConstructor (string s) { }

			TypeWithPublicConstructor () { }
		}

		class TypeWithNonPublicConstructor
		{
			public TypeWithNonPublicConstructor (int i) { }

			[Kept]
			TypeWithNonPublicConstructor () { }

			[Kept]
			TypeWithNonPublicConstructor (string s) { }
		}


		class TypeWithPublicMethod
		{
			[Kept]
			public void PublicMethod () { }

			void PrivateMethod () { }

			public int field;
		}

		class TypeWithNonPublicMethod
		{
			[Kept]
			void PrivateMethod () { }

			public void NonPublicMethod () { }
		}

		class TypeWithPublicField
		{
			[Kept]
			public int publicField;

			string nonPublicField;

			public void Method () { }
		}

		class TypeWithNonPublicField
		{
			[Kept]
			int nonPublicField;

			public string publicField;

			void Method () { }
		}

		[Kept]
		class TypeWithPublicNestedType
		{
			[Kept]
			public class PublicNestedType
			{
				public void Method () { }
				public int field;

				void NonPublicMethod () { }
			}

			public void Method () { }

			void NonPublicMethod () { }

			class NonPublicNestedType
			{
			}
		}

		[Kept]
		class TypeWithNonPublicNestedType
		{
			[Kept]
			class NonPublicNestedType
			{
				public void Method () { }

				public int field;

				void NonPublicMethod () { }
			}

			public void Method () { }

			void NonPublicMethod () { }

			public class PublicNestedType
			{
			}
		}

		class TypeWithPublicProperty
		{
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }

			int NonPublicProperty { get; set; }

			public void Method () { }
		}

		class TypeWithNonPublicProperty
		{
			[Kept]
			[KeptBackingField]
			int NonPublicProperty { [Kept] get; [Kept] set; }

			public int PublicProperty { get; set; }

			void NonPublicMethod () { }
		}

		class TypeWithPublicEvent
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler PublicEvent;

			event EventHandler NonPublicEvent;

			public void Method () { }
		}

		class TypeWithNonPublicEvent
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler NonPublicEvent;

			public event EventHandler PublicEven;

			void NonPublicMethod () { }
		}

		[KeptMember (".ctor()")]
		class TypeWithAll
		{
			[Kept]
			public void PublicMethod () { }

			[Kept]
			void NonPublicMehod () { }

			[Kept]
			public int publicField;

			[Kept]
			int nonPublicField;

			[Kept]
			[KeptBackingField]
			public int PublicProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			int NonPublicProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler PublicEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler NonPublicEvent;

			[Kept]
			[KeptMember (".ctor()")]
			public class PublicNestedType
			{
				[Kept]
				[KeptMember (".ctor()")]
				class RecursiveNestedType
				{
					[Kept]
					void Method () { }
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class NonPublicNestedType
			{
				[Kept]
				[KeptMember (".ctor()")]
				class RecursiveNestedType
				{
					[Kept]
					void Method () { }
				}
			}
		}

		public class TypeWithNone
		{
			public void Method () { }

			public int field;

			public class NestedType { }
		}
	}
}
