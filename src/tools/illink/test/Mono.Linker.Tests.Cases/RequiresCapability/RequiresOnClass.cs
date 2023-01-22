// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresOnClass
	{
		public static void Main ()
		{
			TestRequiresInClassAccessedByStaticMethod ();
			TestRequiresInParentClassAccesedByStaticMethod ();
			TestRequiresInClassAccessedByCctor ();
			TestRequiresOnBaseButNotOnDerived ();
			TestRequiresOnDerivedButNotOnBase ();
			TestRequiresOnBaseAndDerived ();
			TestInstanceFieldSuppression ();
			TestSuppressionsOnClass ();
			TestStaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ();
			TestStaticConstructorCalls ();
			TestOtherMemberTypesWithRequires ();
			TestNameOfDoesntWarn ();
			ReflectionAccessOnMethod.Test ();
			ReflectionAccessOnCtor.Test ();
			ReflectionAccessOnField.Test ();
			ReflectionAccessOnEvents.Test ();
			ReflectionAccessOnProperties.Test ();
			KeepFieldOnAttribute ();
			AttributeParametersAndProperties.Test ();
		}

		[RequiresUnreferencedCode ("Message for --ClassWithRequires--")]
		[RequiresDynamicCode ("Message for --ClassWithRequires--")]
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
				[ExpectedWarning ("IL3050", "RequiresOnClass.RequiresOnMethod.MethodWithRequires()", "MethodWithRequires", ProducedBy = ProducedBy.Analyzer)]
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

			// The attribute would generate warning, but it is suppressed due to the Requires on the type
			[AttributeWithRequires ()]
			public static void AttributedMethod () { }
		}

		class RequiresOnMethod
		{
			[RequiresUnreferencedCode ("MethodWithRequires")]
			[RequiresDynamicCode ("MethodWithRequires")]
			public static void MethodWithRequires () { }
		}

		[ExpectedWarning ("IL2109", "RequiresOnClass.DerivedWithoutRequires", "RequiresOnClass.ClassWithRequires", "--ClassWithRequires--")]
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
		[RequiresDynamicCode ("Message for --StaticCtor--")]
		class StaticCtor
		{
			static StaticCtor ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "RequiresOnClass.StaticCtor.StaticCtor()", "Message for --StaticCtor--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.StaticCtor.StaticCtor()", "Message for --StaticCtor--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCctorRequires ()
		{
			_ = new StaticCtor ();
		}

		[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess--")]
		[RequiresDynamicCode ("Message for --StaticCtorTriggeredByFieldAccess--")]
		class StaticCtorTriggeredByFieldAccess
		{
			static StaticCtorTriggeredByFieldAccess ()
			{
				field = 0;
			}

			public static int field;
		}

		[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--")]
		[ExpectedWarning ("IL3050", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccessWrite ()
		{
			StaticCtorTriggeredByFieldAccess.field = 1;
		}

		[ExpectedWarning ("IL2026", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--")]
		[ExpectedWarning ("IL3050", "StaticCtorTriggeredByFieldAccess.field", "Message for --StaticCtorTriggeredByFieldAccess--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorMarkingTriggeredOnSecondAccessWrite ()
		{
			StaticCtorTriggeredByFieldAccess.field = 2;
		}

		[RequiresUnreferencedCode ("--TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner--")]
		[RequiresDynamicCode ("--TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner--")]
		static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner ()
		{
			StaticCtorTriggeredByFieldAccess.field = 3;
		}

		[UnconditionalSuppressMessage ("test", "IL2026")]
		[UnconditionalSuppressMessage ("test", "IL3050")]
		static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod ()
		{
			TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner ();
		}

		[RequiresUnreferencedCode ("Message for --StaticCCtorTriggeredByFieldAccessRead--")]
		[RequiresDynamicCode ("Message for --StaticCCtorTriggeredByFieldAccessRead--")]
		class StaticCCtorTriggeredByFieldAccessRead
		{
			public static int field = 42;
		}

		[ExpectedWarning ("IL2026", "StaticCCtorTriggeredByFieldAccessRead.field", "Message for --StaticCCtorTriggeredByFieldAccessRead--")]
		[ExpectedWarning ("IL3050", "StaticCCtorTriggeredByFieldAccessRead.field", "Message for --StaticCCtorTriggeredByFieldAccessRead--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorMarkingIsTriggeredByFieldAccessRead ()
		{
			var _ = StaticCCtorTriggeredByFieldAccessRead.field;
		}

		[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByCtorCalls--")]
		[RequiresDynamicCode ("Message for --StaticCtorTriggeredByCtorCalls--")]
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
		[ExpectedWarning ("IL3050", "StaticCtorTriggeredByCtorCalls.StaticCtorTriggeredByCtorCalls()", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorTriggeredByCtorCall ()
		{
			new StaticCtorTriggeredByCtorCalls ();
		}

		[RequiresUnreferencedCode ("Message for --ClassWithInstanceField--")]
		[RequiresDynamicCode ("Message for --ClassWithInstanceField--")]
		class ClassWithInstanceField
		{
			public int field = 42;
		}

		[ExpectedWarning ("IL2026", "ClassWithInstanceField.ClassWithInstanceField()")]
		[ExpectedWarning ("IL3050", "ClassWithInstanceField.ClassWithInstanceField()", ProducedBy = ProducedBy.Analyzer)]
		static void TestInstanceFieldCallDontWarn ()
		{
			ClassWithInstanceField instance = new ClassWithInstanceField ();
			var _ = instance.field;
		}

		public class ClassWithInstanceFieldWhichInitsDangerousClass
		{
			private ClassWithRequires _instanceField = new ClassWithRequires ();

			[RequiresUnreferencedCode ("Calling the constructor is dangerous")]
			[RequiresDynamicCode ("Calling the constructor is dangerous")]
			public ClassWithInstanceFieldWhichInitsDangerousClass () { }
		}

		[ExpectedWarning ("IL2026", "Calling the constructor is dangerous")]
		[ExpectedWarning ("IL3050", "Calling the constructor is dangerous", ProducedBy = ProducedBy.Analyzer)]
		static void TestInstanceFieldSuppression ()
		{
			_ = new ClassWithInstanceFieldWhichInitsDangerousClass ();
		}

		[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall2--")]
		[RequiresDynamicCode ("Message for --StaticCtorTriggeredByMethodCall2--")]
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
		[RequiresDynamicCode ("Message for --DerivedWithRequires--")]
		private class DerivedWithRequires : ClassWithoutRequires
		{
			public static void StaticMethodInInheritedClass () { }

			public class DerivedNestedClass
			{
				public static void NestedStaticMethod () { }
			}
		}

		[RequiresUnreferencedCode ("Message for --DerivedWithRequires2--")]
		[RequiresDynamicCode ("Message for --DerivedWithRequires2--")]
		private class DerivedWithRequires2 : ClassWithRequires
		{
			public static void StaticMethodInInheritedClass () { }

			// A nested class is not considered a static method nor constructor therefore RequiresUnreferencedCode doesnt apply
			// and this warning is not suppressed
			[ExpectedWarning ("IL2109", "RequiresOnClass.DerivedWithRequires2.DerivedNestedClass", "--ClassWithRequires--")]
			public class DerivedNestedClass : ClassWithRequires
			{
				public static void NestedStaticMethod () { }
			}
		}

		class BaseWithoutRequiresOnType
		{
			[RequiresUnreferencedCode ("RUC")]
			[RequiresDynamicCode ("RDC")]
			public virtual void Method () { }
		}

		[RequiresUnreferencedCode ("RUC")]
		[RequiresDynamicCode ("RDC")]
		class DerivedWithRequiresOnType : BaseWithoutRequiresOnType
		{
			public override void Method () { }
		}

		[RequiresUnreferencedCode ("RUC")]
		[RequiresDynamicCode ("RDC")]
		class BaseWithRequiresOnType
		{
			public virtual void Method () { }
		}

		[RequiresUnreferencedCode ("RUC")]
		[RequiresDynamicCode ("RDC")]
		class DerivedWithoutRequiresOnType : BaseWithRequiresOnType
		{
			public override void Method () { }
		}

		class BaseWithNoRequires
		{
			public virtual void Method () { }
		}

		[RequiresUnreferencedCode ("RUC")]
		[RequiresDynamicCode ("RDC")]
		class DerivedWithRequiresOnTypeOverBaseWithNoRequires : BaseWithNoRequires
		{
			// Should not warn since the members are not static
			public override void Method ()
			{
			}
		}

		public interface InterfaceWithoutRequires
		{
			[RequiresUnreferencedCode ("RUC")]
			[RequiresDynamicCode ("RDC")]
			static int Method ()
			{
				return 0;
			}

			[RequiresUnreferencedCode ("RUC")]
			[RequiresDynamicCode ("RDC")]
			int Method (int a);
		}

		[RequiresUnreferencedCode ("RUC")]
		[RequiresDynamicCode ("RDC")]
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

		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresInClassAccessedByStaticMethod ()
		{
			ClassWithRequires.StaticMethod ();
		}

		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresInClassAccessedByCctor ()
		{
			var classObject = new ClassWithRequires ();
		}

		static void TestRequiresInParentClassAccesedByStaticMethod ()
		{
			ClassWithRequires.NestedClass.NestedStaticMethod ();
		}

		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		// Although we suppress the warning from RequiresOnMethod.MethodWithRequires () we still get a warning because we call CallRequiresMethod() which is an static method on a type with RUC
		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.CallMethodWithRequires()", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires.CallMethodWithRequires()", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "ClassWithRequires.Instance", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "ClassWithRequires.Instance", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
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
		[ExpectedWarning ("IL3050", "RequiresOnClass.DerivedWithRequires.StaticMethodInInheritedClass()", "--DerivedWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnDerivedButNotOnBase ()
		{
			DerivedWithRequires.StaticMethodInInheritedClass ();
			DerivedWithRequires.StaticMethod ();
			DerivedWithRequires.DerivedNestedClass.NestedStaticMethod ();
			DerivedWithRequires.NestedClass.NestedStaticMethod ();
		}

		[ExpectedWarning ("IL2026", "RequiresOnClass.DerivedWithRequires2.StaticMethodInInheritedClass()", "--DerivedWithRequires2--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.DerivedWithRequires2.StaticMethodInInheritedClass()", "--DerivedWithRequires2--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires.StaticMethod()", "--ClassWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestRequiresOnBaseAndDerived ()
		{
			DerivedWithRequires2.StaticMethodInInheritedClass ();
			DerivedWithRequires2.StaticMethod ();
			DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod ();
			DerivedWithRequires2.NestedClass.NestedStaticMethod ();
		}

		// TODO: Parameter signature differs between linker and analyzer
		[ExpectedWarning ("IL2026", "RequiresOnClass.ClassWithRequires.TestSuppressions(", "Type[])")]
		[ExpectedWarning ("IL3050", "RequiresOnClass.ClassWithRequires.TestSuppressions(", "Type[])", ProducedBy = ProducedBy.Analyzer)]
		static void TestSuppressionsOnClass ()
		{
			ClassWithRequires.TestSuppressions (new[] { typeof (ClassWithRequires) });
			TestUnconditionalSuppressMessage.StaticMethodInTestSuppressionClass ();
		}

		[RequiresUnreferencedCode ("--StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod--")]
		[RequiresDynamicCode ("--StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod--")]
		static void StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod ()
		{
			DerivedWithRequires.StaticMethodInInheritedClass ();
		}

		[UnconditionalSuppressMessage ("test", "IL2026")]
		[UnconditionalSuppressMessage ("test", "IL3050")]
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
			//TestStaticCtorTriggeredByMethodCall ();
			TestStaticCtorTriggeredByCtorCall ();
			TestInstanceFieldCallDontWarn ();
		}

		[RequiresUnreferencedCode ("--MemberTypesWithRequires--")]
		[RequiresDynamicCode ("--MemberTypesWithRequires--")]
		class MemberTypesWithRequires
		{
			public static int field;
			public static int Property { get; set; }

			// These should not be reported https://github.com/mono/linker/issues/2218
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.add", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.add", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Trimmer)]
			public static event EventHandler Event;
		}

		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.field")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.field", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Property.set")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.Property.set", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Analyzer)]
		static void TestOtherMemberTypesWithRequires ()
		{
			MemberTypesWithRequires.field = 1;
			MemberTypesWithRequires.Property = 1;
			MemberTypesWithRequires.Event -= null;
		}

		static void TestNameOfDoesntWarn ()
		{
			_ = nameof (ClassWithRequires.StaticMethod);
			_ = nameof (MemberTypesWithRequires.field);
			_ = nameof (MemberTypesWithRequires.Property);
			_ = nameof (MemberTypesWithRequires.Event);
		}

		class ReflectionAccessOnMethod
		{
			[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()")]
			[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()")]
			[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)")]
			[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()")]
			[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()")]
			static void TestDAMAccess ()
			{
				// Warns because BaseWithoutRequiresOnType.Method has Requires on the method
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

				// Doesn't warn since the type has no statics
				typeof (DerivedWithRequiresOnTypeOverBaseWithNoRequires).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2026", "BaseWithoutRequiresOnType.Method()")]
			[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method(Int32)")]
			[ExpectedWarning ("IL2026", "InterfaceWithoutRequires.Method()")]
			[ExpectedWarning ("IL2026", "ImplementationWithRequiresOnType.Method()")]
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
			[RequiresDynamicCode ("--BaseWithRequires--")]
			class BaseWithRequires
			{
				public BaseWithRequires () { }
			}

			[ExpectedWarning ("IL2109", "ReflectionAccessOnCtor.DerivedWithoutRequires", "ReflectionAccessOnCtor.BaseWithRequires")]
			class DerivedWithoutRequires : BaseWithRequires
			{
				[ExpectedWarning ("IL2026", "--BaseWithRequires--", ProducedBy = ProducedBy.Trimmer)] // The body has direct call to the base.ctor()
				public DerivedWithoutRequires () { }
			}

			[RequiresUnreferencedCode ("--DerivedWithRequiresOnBaseWithRequires--")]
			[RequiresDynamicCode ("--DerivedWithRequiresOnBaseWithRequires--")]
			class DerivedWithRequiresOnBaseWithRequires : BaseWithRequires
			{
				// No warning - suppressed by the Requires on this type
				private DerivedWithRequiresOnBaseWithRequires () { }
			}

			class BaseWithoutRequires { }

			[RequiresUnreferencedCode ("--DerivedWithRequiresOnBaseWithout--")]
			[RequiresDynamicCode ("--DerivedWithRequiresOnBaseWithout--")]
			class DerivedWithRequiresOnBaseWithoutRequires : BaseWithoutRequires
			{
				public DerivedWithRequiresOnBaseWithoutRequires () { }
			}

			[ExpectedWarning ("IL2026", "BaseWithRequires.BaseWithRequires()")]
			[ExpectedWarning ("IL2026", "BaseWithRequires.BaseWithRequires()")]
			[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()")]
			[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
			[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
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

			[ExpectedWarning ("IL2026", "BaseWithRequires.BaseWithRequires()")]
			[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()")]
			[ExpectedWarning ("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
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
			[RequiresDynamicCode ("--WithRequires--")]
			class WithRequires
			{
				public int InstanceField;
				public static int StaticField;
				private static int PrivateStaticField;
			}

			[RequiresUnreferencedCode ("--WithRequiresOnlyInstanceFields--")]
			[RequiresDynamicCode ("--WithRequiresOnlyInstanceFields--")]
			class WithRequiresOnlyInstanceFields
			{
				public int InstanceField;
			}

			[ExpectedWarning ("IL2109", "ReflectionAccessOnField.DerivedWithoutRequires", "ReflectionAccessOnField.WithRequires")]
			class DerivedWithoutRequires : WithRequires
			{
				public static int DerivedStaticField;
			}

			[RequiresUnreferencedCode ("--DerivedWithRequires--")]
			[RequiresDynamicCode ("--DerivedWithRequires--")]
			class DerivedWithRequires : WithRequires
			{
				public static int DerivedStaticField;
			}

			[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticField")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField")]
			static void TestDAMAccess ()
			{
				typeof (WithRequires).RequiresPublicFields ();
				typeof (WithRequires).RequiresNonPublicFields ();
				typeof (WithRequiresOnlyInstanceFields).RequiresPublicFields ();
				typeof (DerivedWithoutRequires).RequiresPublicFields ();
				typeof (DerivedWithRequires).RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2026", "WithRequires.StaticField")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticField")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField")]
			static void TestDirectReflectionAccess ()
			{
				typeof (WithRequires).GetField (nameof (WithRequires.StaticField));
				typeof (WithRequires).GetField (nameof (WithRequires.InstanceField)); // Doesn't warn
				typeof (WithRequires).GetField ("PrivateStaticField", BindingFlags.NonPublic);
				typeof (WithRequiresOnlyInstanceFields).GetField (nameof (WithRequiresOnlyInstanceFields.InstanceField)); // Doesn't warn
				typeof (DerivedWithoutRequires).GetField (nameof (DerivedWithoutRequires.DerivedStaticField)); // Doesn't warn
				typeof (DerivedWithRequires).GetField (nameof (DerivedWithRequires.DerivedStaticField));
			}

			[ExpectedWarning ("IL2026", "WithRequires.StaticField", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "WithRequires.StaticField", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "WithRequires.StaticField", ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (nameof (WithRequires.StaticField), typeof (WithRequires))]
			[DynamicDependency (nameof (WithRequires.InstanceField), typeof (WithRequires))] // Doesn't warn
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithoutRequires))] // Doesn't warn
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticField", ProducedBy = ProducedBy.Trimmer)]
			[DynamicDependency (DynamicallyAccessedMemberTypes.PublicFields, typeof (DerivedWithRequires))]
			static void TestDynamicDependencyAccess ()
			{
			}

			[RequiresUnreferencedCode ("This class is dangerous")]
			[RequiresDynamicCode ("This class is dangerous")]
			class BaseForDAMAnnotatedClass
			{
				public static int baseField;
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			[RequiresUnreferencedCode ("This class is dangerous")]
			[RequiresDynamicCode ("This class is dangerous")]
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

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			class DAMAnnotatedClassAccessedFromRUCScope
			{
				[ExpectedWarning ("IL2112", "DAMAnnotatedClassAccessedFromRUCScope.RUCMethod", ProducedBy = ProducedBy.Trimmer)]
				[RequiresUnreferencedCode ("--RUCMethod--")]
				public static void RUCMethod () { }
			}

			// RUC on the callsite to GetType should not suppress warnings about the
			// attribute on the type.
			[RequiresUnreferencedCode ("--TestDAMOnTypeAccessInRUCScope--")]
			static void TestDAMOnTypeAccessInRUCScope (DAMAnnotatedClassAccessedFromRUCScope instance = null)
			{
				instance.GetType ().GetMethod ("RUCMethod");
			}

			[ExpectedWarning ("IL2026", "--TestDAMOnTypeAccessInRUCScope--")]
			public static void Test ()
			{
				TestDAMAccess ();
				TestDirectReflectionAccess ();
				TestDynamicDependencyAccess ();
				TestDAMOnTypeAccess (null);
				TestDAMOnTypeAccessInRUCScope ();
			}
		}

		class ReflectionAccessOnEvents
		{
			// Most of the tests in this run into https://github.com/dotnet/linker/issues/2218
			// So for now keeping just a very simple test

			[RequiresUnreferencedCode ("--WithRequires--")]
			[RequiresDynamicCode ("--WithRequires--")]
			class WithRequires
			{
				// These should be reported only in TestDirectReflectionAccess
				// https://github.com/mono/linker/issues/2218
				[ExpectedWarning ("IL2026", "StaticEvent.add", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "StaticEvent.add", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "StaticEvent.add", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "StaticEvent.remove", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "StaticEvent.remove", ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2026", "StaticEvent.remove", ProducedBy = ProducedBy.Trimmer)]
				public static event EventHandler StaticEvent;
			}

			[ExpectedWarning ("IL2026", "StaticEvent.add")]
			// https://github.com/mono/linker/issues/2218
			[ExpectedWarning ("IL2026", "StaticEvent.remove", ProducedBy = ProducedBy.Analyzer)]
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
			[RequiresDynamicCode ("--WithRequires--")]
			class WithRequires
			{
				public int InstanceProperty { get; set; }
				public static int StaticProperty { get; set; }
				private static int PrivateStaticProperty { get; set; }
			}

			[RequiresUnreferencedCode ("--WithRequiresOnlyInstanceProperties--")]
			[RequiresDynamicCode ("--WithRequiresOnlyInstanceProperties--")]
			class WithRequiresOnlyInstanceProperties
			{
				public int InstnaceProperty { get; set; }
			}

			[ExpectedWarning ("IL2109", "ReflectionAccessOnProperties.DerivedWithoutRequires", "ReflectionAccessOnProperties.WithRequires")]
			class DerivedWithoutRequires : WithRequires
			{
				public static int DerivedStaticProperty { get; set; }
			}

			[RequiresUnreferencedCode ("--DerivedWithRequires--")]
			[RequiresDynamicCode ("--DerivedWithRequires--")]
			class DerivedWithRequires : WithRequires
			{
				public static int DerivedStaticProperty { get; set; }
			}

			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.set")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.get")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.set")]
			static void TestDAMAccess ()
			{
				typeof (WithRequires).RequiresPublicProperties ();
				typeof (WithRequires).RequiresNonPublicProperties ();
				typeof (WithRequiresOnlyInstanceProperties).RequiresPublicProperties ();
				typeof (DerivedWithoutRequires).RequiresPublicProperties ();
				typeof (DerivedWithRequires).RequiresPublicProperties ();
			}

			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.get")]
			[ExpectedWarning ("IL2026", "WithRequires.PrivateStaticProperty.set")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.get")]
			[ExpectedWarning ("IL2026", "DerivedWithRequires.DerivedStaticProperty.set")]
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
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.get", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "WithRequires.StaticProperty.set", ProducedBy = ProducedBy.Trimmer)]
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
			[RequiresDynamicCode ("This class is dangerous")]
			class BaseForDAMAnnotatedClass
			{
				public static int baseProperty { get; set; }
			}

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			[RequiresUnreferencedCode ("This class is dangerous")]
			[RequiresDynamicCode ("This class is dangerous")]
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
		[RequiresDynamicCode ("The attribute is dangerous")]
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
		[ExpectedWarning ("IL2026", "AttributeWithRequires.AttributeWithRequires()")]
		[ExpectedWarning ("IL3050", "AttributeWithRequires.AttributeWithRequires()", ProducedBy = ProducedBy.Analyzer)]
		static void KeepFieldOnAttribute () { }

		public class AttributeParametersAndProperties
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			public static Type AnnotatedField;

			[AttributeUsage (AttributeTargets.Method, AllowMultiple = true)]
			public class AttributeWithRequirementsOnParameters : Attribute
			{
				public AttributeWithRequirementsOnParameters ()
				{
				}

				public AttributeWithRequirementsOnParameters ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
				{
				}

				public int PropertyWithRequires {
					get => 0;

					[RequiresUnreferencedCode ("--PropertyWithRequires--")]
					[RequiresDynamicCode ("--PropertyWithRequires--")]
					set { }
				}

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
				public Type AnnotatedField;
			}

			[RequiresUnreferencedCode ("--AttributeParametersAndProperties--")]
			[RequiresDynamicCode ("--AttributeParametersAndProperties--")]
			class TestClass
			{
				[AttributeWithRequirementsOnParameters (typeof (AttributeParametersAndProperties))]
				[AttributeWithRequirementsOnParameters (PropertyWithRequires = 1)]
				[AttributeWithRequirementsOnParameters (AnnotatedField = typeof (AttributeParametersAndProperties))]
				public static void Test () { }
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				TestClass.Test ();
			}
		}
	}
}
