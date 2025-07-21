// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    class RequiresOnClassExcludeStatics
    {
        public static void Main()
        {
            TestRequiresInClassAccessedByStaticMethod();
            TestRequiresInParentClassAccesedByStaticMethod();
            TestRequiresInClassAccessedByCctor();
            TestRequiresOnBaseButNotOnDerived();
            TestRequiresOnDerivedButNotOnBase();
            TestRequiresOnBaseAndDerived();
            TestInstanceFieldSuppression();
            TestSuppressionsOnClass();
            TestStaticMethodOnRequiresTypeSuppressedByRequiresOnMethod();
            TestStaticConstructorCalls();
            TestOtherMemberTypesWithRequires();
            TestNameOfDoesntWarn();
            ReflectionAccessOnMethod.Test();
            ReflectionAccessOnCtor.Test();
            ReflectionAccessOnField.Test();
            ReflectionAccessOnEvents.Test();
            ReflectionAccessOnProperties.Test();
            KeepFieldOnAttribute();
            AttributeParametersAndProperties.Test();
            MembersOnClassWithRequires<int>.Test();
            ConstFieldsOnClassWithRequires.Test();
        }

        [RequiresUnreferencedCode("Message for --ClassWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --ClassWithRequires--", ExcludeStatics = true)]
        class ClassWithRequires
        {
            public static object Instance;

            public ClassWithRequires() { }

            public static void StaticMethod() { }

            public void NonStaticMethod() { }

            [ExpectedWarning("IL2026", "RequiresOnMethod.MethodWithRequires()", "MethodWithRequires")]
            [ExpectedWarning("IL3050", "RequiresOnMethod.MethodWithRequires()", "MethodWithRequires", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
            public static void CallMethodWithRequires() => RequiresOnMethod.MethodWithRequires();

            public class NestedClass
            {
                public static void NestedStaticMethod() { }

                // This warning doesn't get suppressed since the declaring type NestedClass is not annotated with Requires
                [ExpectedWarning("IL2026", "RequiresOnClassExcludeStatics.RequiresOnMethod.MethodWithRequires()", "MethodWithRequires")]
                [ExpectedWarning("IL3050", "RequiresOnClassExcludeStatics.RequiresOnMethod.MethodWithRequires()", "MethodWithRequires", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
                public static void CallMethodWithRequires() => RequiresOnMethod.MethodWithRequires();
            }

            // RequiresUnfereferencedCode on the type will suppress IL2072
            static ClassWithRequires()
            {
                Instance = Activator.CreateInstance(Type.GetType("SomeText"));
            }

            [ExpectedWarning("IL2065", "GetMethods")]
            public static void TestSuppressions(Type[] types)
            {
                // StaticMethod is a static method on a Requires annotated type, so it should warn. But Requires in the
                // class suppresses other Requires messages
                StaticMethod();

                var nested = new NestedClass();

                // Requires in the class suppresses DynamicallyAccessedMembers messages
                types[1].GetMethods();

                void LocalFunction(int a) { }
                LocalFunction(2);

                AttributedMethod();
                typeof(ClassWithRequires).GetMethod("AttributedMethod");
            }

            [ExpectedWarning("IL2026", "AttributeWithRequires.AttributeWithRequires()")]
            [ExpectedWarning("IL3050", "AttributeWithRequires.AttributeWithRequires()", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
            [AttributeWithRequires()]
            public static void AttributedMethod() { }
        }

        class RequiresOnMethod
        {
            [RequiresUnreferencedCode("MethodWithRequires", ExcludeStatics = true)]
            [RequiresDynamicCode("MethodWithRequires", ExcludeStatics = true)]
            public static void MethodWithRequires() { }
        }

        [ExpectedWarning("IL2109", "RequiresOnClassExcludeStatics.DerivedWithoutRequires", "RequiresOnClassExcludeStatics.ClassWithRequires", "--ClassWithRequires--")]
        private class DerivedWithoutRequires : ClassWithRequires
        {
            // This method contains implicit call to ClassWithRequires.ctor()
            [ExpectedWarning("IL2026")]
            [ExpectedWarning("IL3050", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
            public DerivedWithoutRequires() { }

            public static void StaticMethodInInheritedClass() { }

            public class DerivedNestedClass
            {
                public static void NestedStaticMethod() { }
            }

            public static void ShouldntWarn(object objectToCast)
            {
                _ = typeof(ClassWithRequires);
                var type = (ClassWithRequires)objectToCast;
            }
        }

        // In order to generate IL2109 the nested class would also need to be annotated with Requires
        // otherwise we threat the nested class as safe
        private class DerivedWithoutRequires2 : ClassWithRequires.NestedClass
        {
            public static void StaticMethod() { }
        }

        [UnconditionalSuppressMessage("trim", "IL2109")]
        class TestUnconditionalSuppressMessage : ClassWithRequires
        {
            public static void StaticMethodInTestSuppressionClass() { }
        }

        class ClassWithoutRequires
        {
            public ClassWithoutRequires() { }

            public static void StaticMethod() { }

            public void NonStaticMethod() { }

            public class NestedClass
            {
                public static void NestedStaticMethod() { }
            }
        }

        [RequiresUnreferencedCode("Message for --StaticCtor--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --StaticCtor--", ExcludeStatics = true)]
        class StaticCtor
        {
            static StaticCtor()
            {
            }
        }

        [ExpectedWarning("IL2026", "RequiresOnClassExcludeStatics.StaticCtor.StaticCtor()", "Message for --StaticCtor--")]
        [ExpectedWarning("IL3050", "RequiresOnClassExcludeStatics.StaticCtor.StaticCtor()", "Message for --StaticCtor--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
        static void TestStaticCctorRequires()
        {
            _ = new StaticCtor();
        }

        [RequiresUnreferencedCode("Message for --StaticCtorTriggeredByFieldAccess--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --StaticCtorTriggeredByFieldAccess--", ExcludeStatics = true)]
        class StaticCtorTriggeredByFieldAccess
        {
            static StaticCtorTriggeredByFieldAccess()
            {
                field = 0;
            }

            public static int field;
        }

        static void TestStaticCtorMarkingIsTriggeredByFieldAccessWrite()
        {
            StaticCtorTriggeredByFieldAccess.field = 1;
        }

        static void TestStaticCtorMarkingTriggeredOnSecondAccessWrite()
        {
            StaticCtorTriggeredByFieldAccess.field = 2;
        }

        [RequiresUnreferencedCode("--TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner--", ExcludeStatics = true)]
        [RequiresDynamicCode("--TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner--", ExcludeStatics = true)]
        static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner()
        {
            StaticCtorTriggeredByFieldAccess.field = 3;
        }

        [UnconditionalSuppressMessage("test", "IL2026")]
        [UnconditionalSuppressMessage("test", "IL3050")]
        static void TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod()
        {
            TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod_Inner();
        }

        [RequiresUnreferencedCode("Message for --StaticCCtorTriggeredByFieldAccessRead--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --StaticCCtorTriggeredByFieldAccessRead--", ExcludeStatics = true)]
        class StaticCCtorTriggeredByFieldAccessRead
        {
            public static int field = 42;
        }

        static void TestStaticCtorMarkingIsTriggeredByFieldAccessRead()
        {
            var _ = StaticCCtorTriggeredByFieldAccessRead.field;
        }

        [RequiresUnreferencedCode("Message for --StaticCtorTriggeredByCtorCalls--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --StaticCtorTriggeredByCtorCalls--", ExcludeStatics = true)]
        class StaticCtorTriggeredByCtorCalls
        {
            static StaticCtorTriggeredByCtorCalls()
            {
            }

            public void TriggerStaticCtorMarking()
            {
            }
        }

        [ExpectedWarning("IL2026", "StaticCtorTriggeredByCtorCalls.StaticCtorTriggeredByCtorCalls()")]
        [ExpectedWarning("IL3050", "StaticCtorTriggeredByCtorCalls.StaticCtorTriggeredByCtorCalls()", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
        static void TestStaticCtorTriggeredByCtorCall()
        {
            new StaticCtorTriggeredByCtorCalls();
        }

        [RequiresUnreferencedCode("Message for --ClassWithInstanceField--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --ClassWithInstanceField--", ExcludeStatics = true)]
        class ClassWithInstanceField
        {
            public int field = 42;
        }

        [ExpectedWarning("IL2026", "ClassWithInstanceField.ClassWithInstanceField()")]
        [ExpectedWarning("IL3050", "ClassWithInstanceField.ClassWithInstanceField()", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
        static void TestInstanceFieldCallDontWarn()
        {
            ClassWithInstanceField instance = new ClassWithInstanceField();
            var _ = instance.field;
        }

        public class ClassWithInstanceFieldWhichInitsDangerousClass
        {
            private ClassWithRequires _instanceField = new ClassWithRequires();

            [RequiresUnreferencedCode("Calling the constructor is dangerous", ExcludeStatics = true)]
            [RequiresDynamicCode("Calling the constructor is dangerous", ExcludeStatics = true)]
            public ClassWithInstanceFieldWhichInitsDangerousClass() { }
        }

        [ExpectedWarning("IL2026", "Calling the constructor is dangerous")]
        [ExpectedWarning("IL3050", "Calling the constructor is dangerous", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
        static void TestInstanceFieldSuppression()
        {
            _ = new ClassWithInstanceFieldWhichInitsDangerousClass();
        }

        [RequiresUnreferencedCode("Message for --StaticCtorTriggeredByMethodCall2--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --StaticCtorTriggeredByMethodCall2--", ExcludeStatics = true)]
        class StaticCtorTriggeredByMethodCall2
        {
            static StaticCtorTriggeredByMethodCall2()
            {
            }

            public void TriggerStaticCtorMarking()
            {
            }
        }

        static void TestNullInstanceTryingToCallMethod()
        {
            StaticCtorTriggeredByMethodCall2 instance = null;
            instance.TriggerStaticCtorMarking();
        }

        [RequiresUnreferencedCode("Message for --DerivedWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --DerivedWithRequires--", ExcludeStatics = true)]
        private class DerivedWithRequires : ClassWithoutRequires
        {
            public static void StaticMethodInInheritedClass() { }

            public class DerivedNestedClass
            {
                public static void NestedStaticMethod() { }
            }
        }

        [RequiresUnreferencedCode("Message for --DerivedWithRequires2--", ExcludeStatics = true)]
        [RequiresDynamicCode("Message for --DerivedWithRequires2--", ExcludeStatics = true)]
        private class DerivedWithRequires2 : ClassWithRequires
        {
            public static void StaticMethodInInheritedClass() { }

            // A nested class is not considered a static method nor constructor therefore RequiresUnreferencedCode doesn't apply
            // and this warning is not suppressed
            [ExpectedWarning("IL2109", "RequiresOnClassExcludeStatics.DerivedWithRequires2.DerivedNestedClass", "--ClassWithRequires--")]
            public class DerivedNestedClass : ClassWithRequires
            {
                // This method contains implicit call to ClassWithRequires.ctor()
                [ExpectedWarning("IL2026")]
                [ExpectedWarning("IL3050", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
                public DerivedNestedClass() { }

                public static void NestedStaticMethod() { }
            }
        }

        class BaseWithoutRequiresOnType
        {
            [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
            [RequiresDynamicCode("RDC", ExcludeStatics = true)]
            public virtual void Method() { }
        }

        [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
        [RequiresDynamicCode("RDC", ExcludeStatics = true)]
        class DerivedWithRequiresOnType : BaseWithoutRequiresOnType
        {
            public override void Method() { }
        }

        [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
        [RequiresDynamicCode("RDC", ExcludeStatics = true)]
        class BaseWithRequiresOnType
        {
            public virtual void Method() { }
        }

        [ExpectedWarning("IL2109", nameof(BaseWithRequiresOnType))]
        class DerivedWithoutRequiresOnType : BaseWithRequiresOnType
        {
            public override void Method() { }
        }

        class BaseWithNoRequires
        {
            public virtual void Method() { }
        }

        [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
        [RequiresDynamicCode("RDC", ExcludeStatics = true)]
        class DerivedWithRequiresOnTypeOverBaseWithNoRequires : BaseWithNoRequires
        {
            // Should not warn since the members are not static
            public override void Method()
            {
            }
        }

        public interface InterfaceWithoutRequires
        {
            [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
            [RequiresDynamicCode("RDC", ExcludeStatics = true)]
            static int Method()
            {
                return 0;
            }

            [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
            [RequiresDynamicCode("RDC", ExcludeStatics = true)]
            int Method(int a);
        }

        [RequiresUnreferencedCode("RUC", ExcludeStatics = true)]
        [RequiresDynamicCode("RDC", ExcludeStatics = true)]
        class ImplementationWithRequiresOnType : InterfaceWithoutRequires
        {
            public static int Method()
            {
                return 1;
            }

            public int Method(int a)
            {
                return a;
            }
        }

        static void TestRequiresInClassAccessedByStaticMethod()
        {
            ClassWithRequires.StaticMethod();
        }

        [ExpectedWarning("IL2026", "RequiresOnClassExcludeStatics.ClassWithRequires", "--ClassWithRequires--")]
        [ExpectedWarning("IL3050", "RequiresOnClassExcludeStatics.ClassWithRequires", "--ClassWithRequires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warnings")]
        static void TestRequiresInClassAccessedByCctor()
        {
            var classObject = new ClassWithRequires();
        }

        static void TestRequiresInParentClassAccesedByStaticMethod()
        {
            ClassWithRequires.NestedClass.NestedStaticMethod();
        }

        static void TestRequiresOnBaseButNotOnDerived()
        {
            var a = new DerivedWithoutRequires(); // Must instantiate to force checks on the base type (otherwise base type is non-interesting)
            DerivedWithoutRequires.StaticMethodInInheritedClass();
            DerivedWithoutRequires.StaticMethod();
            DerivedWithoutRequires.CallMethodWithRequires();
            DerivedWithoutRequires.DerivedNestedClass.NestedStaticMethod();
            DerivedWithoutRequires.NestedClass.NestedStaticMethod();
            DerivedWithoutRequires.NestedClass.CallMethodWithRequires();
            DerivedWithoutRequires.ShouldntWarn(null);
            DerivedWithoutRequires.Instance.ToString();
            DerivedWithoutRequires2.StaticMethod();
        }

        static void TestRequiresOnDerivedButNotOnBase()
        {
            DerivedWithRequires.StaticMethodInInheritedClass();
            DerivedWithRequires.StaticMethod();
            DerivedWithRequires.DerivedNestedClass.NestedStaticMethod();
            DerivedWithRequires.NestedClass.NestedStaticMethod();
        }

        static void TestRequiresOnBaseAndDerived()
        {
            DerivedWithRequires2.StaticMethodInInheritedClass();
            DerivedWithRequires2.StaticMethod();
            var a = new DerivedWithRequires2.DerivedNestedClass();
            DerivedWithRequires2.DerivedNestedClass.NestedStaticMethod();
            DerivedWithRequires2.NestedClass.NestedStaticMethod();
        }

        static void TestSuppressionsOnClass()
        {
            ClassWithRequires.TestSuppressions(new[] { typeof(ClassWithRequires) });
            TestUnconditionalSuppressMessage.StaticMethodInTestSuppressionClass();
        }

        [RequiresUnreferencedCode("--StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod--", ExcludeStatics = true)]
        [RequiresDynamicCode("--StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod--", ExcludeStatics = true)]
        static void StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod()
        {
            DerivedWithRequires.StaticMethodInInheritedClass();
        }

        [UnconditionalSuppressMessage("test", "IL2026")]
        [UnconditionalSuppressMessage("test", "IL3050")]
        static void TestStaticMethodOnRequiresTypeSuppressedByRequiresOnMethod()
        {
            StaticMethodOnRequiresTypeSuppressedByRequiresOnMethod();
        }

        static void TestStaticConstructorCalls()
        {
            TestStaticCctorRequires();
            TestStaticCtorMarkingIsTriggeredByFieldAccessWrite();
            TestStaticCtorMarkingTriggeredOnSecondAccessWrite();
            TestStaticRequiresFieldAccessSuppressedByRequiresOnMethod();
            TestStaticCtorMarkingIsTriggeredByFieldAccessRead();
            //TestStaticCtorTriggeredByMethodCall();
            TestStaticCtorTriggeredByCtorCall();
            TestInstanceFieldCallDontWarn();
        }

        [RequiresUnreferencedCode("--MemberTypesWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("--MemberTypesWithRequires--", ExcludeStatics = true)]
        class MemberTypesWithRequires
        {
            public static int field;
            public static int Property { get; set; }

            public static event EventHandler Event;
        }

        static void TestOtherMemberTypesWithRequires()
        {
            MemberTypesWithRequires.field = 1;
            MemberTypesWithRequires.Property = 1;
            MemberTypesWithRequires.Event -= null;
        }

        static void TestNameOfDoesntWarn()
        {
            _ = nameof(ClassWithRequires.StaticMethod);
            _ = nameof(MemberTypesWithRequires.field);
            _ = nameof(MemberTypesWithRequires.Property);
            _ = nameof(MemberTypesWithRequires.Event);
        }

        class ReflectionAccessOnMethod
        {
            [ExpectedWarning("IL2026", "BaseWithRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "BaseWithRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "BaseWithoutRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithoutRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "BaseWithoutRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithoutRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "InterfaceWithoutRequires.Method(Int32)")]
            [ExpectedWarning("IL3050", "InterfaceWithoutRequires.Method(Int32)", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "InterfaceWithoutRequires.Method()")]
            [ExpectedWarning("IL3050", "InterfaceWithoutRequires.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "ImplementationWithRequiresOnType.Method(Int32)")]
            [ExpectedWarning("IL3050", "ImplementationWithRequiresOnType.Method(Int32)", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnTypeOverBaseWithNoRequires.Method()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnTypeOverBaseWithNoRequires.Method()", Tool.NativeAot, "")]
            static void TestDAMAccess()
            {
                // Warns because BaseWithoutRequiresOnType.Method has Requires on the method
                typeof(BaseWithoutRequiresOnType).RequiresPublicMethods();

                // Doesn't warn because DerivedWithRequiresOnType doesn't have any static methods
                typeof(DerivedWithRequiresOnType).RequiresPublicMethods();

                // Warns twice since both methods on InterfaceWithoutRequires have RUC on the method
                typeof(InterfaceWithoutRequires).RequiresPublicMethods();

                // Warns because ImplementationWithRequiresOnType.Method is a static public method on a RUC type
                typeof(ImplementationWithRequiresOnType).RequiresPublicMethods();

                // Warns for instance method on BaseWithRequiresOnType
                typeof(BaseWithRequiresOnType).RequiresPublicMethods();

                // Warns for instance method on base type
                typeof(DerivedWithoutRequiresOnType).RequiresPublicMethods();

                // Doesn't warn since the type has no statics
                typeof(DerivedWithRequiresOnTypeOverBaseWithNoRequires).RequiresPublicMethods();
            }

            [ExpectedWarning("IL2026", "BaseWithRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "BaseWithoutRequiresOnType.Method()")]
            [ExpectedWarning("IL3050", "BaseWithoutRequiresOnType.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "InterfaceWithoutRequires.Method(Int32)")]
            [ExpectedWarning("IL3050", "InterfaceWithoutRequires.Method(Int32)", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "InterfaceWithoutRequires.Method()")]
            [ExpectedWarning("IL3050", "InterfaceWithoutRequires.Method()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "ImplementationWithRequiresOnType.Method(Int32)")]
            [ExpectedWarning("IL3050", "ImplementationWithRequiresOnType.Method(Int32)", Tool.NativeAot, "")]
            static void TestDirectReflectionAccess()
            {
                // Requires on the method itself
                typeof(BaseWithoutRequiresOnType).GetMethod(nameof(BaseWithoutRequiresOnType.Method));

                // Requires on the method itself
                typeof(InterfaceWithoutRequires).GetMethod(nameof(InterfaceWithoutRequires.Method));

                // Warns for static and instance methods on ImplementationWithRequiresOnType
                typeof(ImplementationWithRequiresOnType).GetMethod(nameof(ImplementationWithRequiresOnType.Method));

                // Warns for instance Method on RUC type
                typeof(BaseWithRequiresOnType).GetMethod(nameof(BaseWithRequiresOnType.Method));
            }

            public static void Test()
            {
                TestDAMAccess();
                TestDirectReflectionAccess();
            }
        }

        class ReflectionAccessOnCtor
        {
            [RequiresUnreferencedCode("--BaseWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--BaseWithRequires--", ExcludeStatics = true)]
            class BaseWithRequires
            {
                public BaseWithRequires() { }
            }

            [ExpectedWarning("IL2109", "ReflectionAccessOnCtor.DerivedWithoutRequires", "ReflectionAccessOnCtor.BaseWithRequires")]
            class DerivedWithoutRequires : BaseWithRequires
            {
                [ExpectedWarning("IL2026", "--BaseWithRequires--")] // The body has direct call to the base.ctor()
                [ExpectedWarning("IL3050", "--BaseWithRequires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warning")]
                public DerivedWithoutRequires() { }
            }

            [RequiresUnreferencedCode("--DerivedWithRequiresOnBaseWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--DerivedWithRequiresOnBaseWithRequires--", ExcludeStatics = true)]
            class DerivedWithRequiresOnBaseWithRequires : BaseWithRequires
            {
                // No warning - suppressed by the Requires on this type
                private DerivedWithRequiresOnBaseWithRequires() { }
            }

            class BaseWithoutRequires { }

            [RequiresUnreferencedCode("--DerivedWithRequiresOnBaseWithout--", ExcludeStatics = true)]
            [RequiresDynamicCode("--DerivedWithRequiresOnBaseWithout--", ExcludeStatics = true)]
            class DerivedWithRequiresOnBaseWithoutRequires : BaseWithoutRequires
            {
                public DerivedWithRequiresOnBaseWithoutRequires() { }
            }

            [ExpectedWarning("IL2026", "BaseWithRequires.BaseWithRequires()")]
            [ExpectedWarning("IL3050", "BaseWithRequires.BaseWithRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "BaseWithRequires.BaseWithRequires()")]
            [ExpectedWarning("IL3050", "BaseWithRequires.BaseWithRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()", Tool.NativeAot, "")]
            static void TestDAMAccess()
            {
                // Warns because the type has Requires
                typeof(BaseWithRequires).RequiresPublicConstructors();

                // Doesn't warn since there's no Requires on this type
                typeof(DerivedWithoutRequires).RequiresPublicParameterlessConstructor();

                // Warns - Requires on the type
                typeof(DerivedWithRequiresOnBaseWithRequires).RequiresNonPublicConstructors();

                // Warns - Requires On the type
                typeof(DerivedWithRequiresOnBaseWithoutRequires).RequiresPublicConstructors();
            }

            [ExpectedWarning("IL2026", "BaseWithRequires.BaseWithRequires()")]
            [ExpectedWarning("IL3050", "BaseWithRequires.BaseWithRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnBaseWithRequires.DerivedWithRequiresOnBaseWithRequires()", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()")]
            [ExpectedWarning("IL3050", "DerivedWithRequiresOnBaseWithoutRequires.DerivedWithRequiresOnBaseWithoutRequires()", Tool.NativeAot, "")]
            static void TestDirectReflectionAccess()
            {
                typeof(BaseWithRequires).GetConstructor(Type.EmptyTypes);
                typeof(DerivedWithoutRequires).GetConstructor(Type.EmptyTypes);
                typeof(DerivedWithRequiresOnBaseWithRequires).GetConstructor(BindingFlags.NonPublic, Type.EmptyTypes);
                typeof(DerivedWithRequiresOnBaseWithoutRequires).GetConstructor(Type.EmptyTypes);
            }

            public static void Test()
            {
                TestDAMAccess();
                TestDirectReflectionAccess();
            }
        }

        class ReflectionAccessOnField
        {
            [RequiresUnreferencedCode("--WithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--WithRequires--", ExcludeStatics = true)]
            class WithRequires
            {
                public int InstanceField;
                public static int StaticField;
                private static int PrivateStaticField;
            }

            [RequiresUnreferencedCode("--WithRequiresOnlyInstanceFields--", ExcludeStatics = true)]
            [RequiresDynamicCode("--WithRequiresOnlyInstanceFields--", ExcludeStatics = true)]
            class WithRequiresOnlyInstanceFields
            {
                public int InstanceField;
            }

            [ExpectedWarning("IL2109", "ReflectionAccessOnField.DerivedWithoutRequires", "ReflectionAccessOnField.WithRequires")]
            class DerivedWithoutRequires : WithRequires
            {
                public static int DerivedStaticField;
            }

            [RequiresUnreferencedCode("--DerivedWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--DerivedWithRequires--", ExcludeStatics = true)]
            class DerivedWithRequires : WithRequires
            {
                public static int DerivedStaticField;
            }

            static void TestDAMAccess()
            {
                typeof(WithRequires).RequiresPublicFields();
                typeof(WithRequires).RequiresNonPublicFields();
                typeof(WithRequiresOnlyInstanceFields).RequiresPublicFields();
                typeof(DerivedWithoutRequires).RequiresPublicFields();
                typeof(DerivedWithRequires).RequiresPublicFields();
            }

            static void TestDirectReflectionAccess()
            {
                typeof(WithRequires).GetField(nameof(WithRequires.StaticField));
                typeof(WithRequires).GetField(nameof(WithRequires.InstanceField)); // Doesn't warn
                typeof(WithRequires).GetField("PrivateStaticField", BindingFlags.NonPublic);
                typeof(WithRequiresOnlyInstanceFields).GetField(nameof(WithRequiresOnlyInstanceFields.InstanceField)); // Doesn't warn
                typeof(DerivedWithoutRequires).GetField(nameof(DerivedWithoutRequires.DerivedStaticField)); // Doesn't warn
                typeof(DerivedWithRequires).GetField(nameof(DerivedWithRequires.DerivedStaticField));
            }

            [DynamicDependency(nameof(WithRequires.StaticField), typeof(WithRequires))]
            [DynamicDependency(nameof(WithRequires.InstanceField), typeof(WithRequires))] // Doesn't warn
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(DerivedWithoutRequires))] // Doesn't warn
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(DerivedWithRequires))]
            static void TestDynamicDependencyAccess()
            {
            }

            [RequiresUnreferencedCode("This class is dangerous", ExcludeStatics = true)]
            [RequiresDynamicCode("This class is dangerous", ExcludeStatics = true)]
            class BaseForDAMAnnotatedClass
            {
                public static int baseField;
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            [RequiresUnreferencedCode("This class is dangerous", ExcludeStatics = true)]
            [RequiresDynamicCode("This class is dangerous", ExcludeStatics = true)]
            class DAMAnnotatedClass : BaseForDAMAnnotatedClass
            {
                public static int publicField;

                static int privatefield;
            }

            static void TestDAMOnTypeAccess(DAMAnnotatedClass instance)
            {
                instance.GetType().GetField("publicField");
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            class DAMAnnotatedClassAccessedFromRUCScope
            {
                [ExpectedWarning("IL2112", "DAMAnnotatedClassAccessedFromRUCScope.RUCMethod")]
                [RequiresUnreferencedCode("--RUCMethod--", ExcludeStatics = true)]
                public static void RUCMethod() { }
            }

            // RUC on the callsite to GetType should not suppress warnings about the
            // attribute on the type.
            [RequiresUnreferencedCode("--TestDAMOnTypeAccessInRUCScope--", ExcludeStatics = true)]
            static void TestDAMOnTypeAccessInRUCScope(DAMAnnotatedClassAccessedFromRUCScope instance = null)
            {
                instance.GetType().GetMethod("RUCMethod");
            }

            [RequiresUnreferencedCode("--GenericTypeWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--GenericTypeWithRequires--", ExcludeStatics = true)]
            class GenericTypeWithRequires<T>
            {
                public static int NonGenericField;
            }

            static void TestDAMAccessOnOpenGeneric()
            {
                typeof(GenericTypeWithRequires<>).RequiresPublicFields();
            }

            static void TestDAMAccessOnInstantiatedGeneric()
            {
                typeof(GenericTypeWithRequires<int>).RequiresPublicFields();
            }

            [ExpectedWarning("IL2026", "--TestDAMOnTypeAccessInRUCScope--")]
            [ExpectedWarning("IL2026", "DAMAnnotatedClass.DAMAnnotatedClass()")]
            [ExpectedWarning("IL3050", "DAMAnnotatedClass.DAMAnnotatedClass()", Tool.Analyzer | Tool.NativeAot, "NativeAOT-specific warning")]
            public static void Test()
            {
                TestDAMAccess();
                TestDirectReflectionAccess();
                TestDynamicDependencyAccess();
                TestDAMOnTypeAccess(new DAMAnnotatedClass());
                TestDAMOnTypeAccessInRUCScope(new DAMAnnotatedClassAccessedFromRUCScope());
                TestDAMAccessOnOpenGeneric();
                TestDAMAccessOnInstantiatedGeneric();
            }
        }

        class ReflectionAccessOnEvents
        {
            [RequiresUnreferencedCode("--WithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--WithRequires--", ExcludeStatics = true)]
            class WithRequires
            {
                public static event EventHandler StaticEvent;
                public event EventHandler InstanceEvent;
                private event EventHandler PrivateInstanceEvent;
            }

            [RequiresUnreferencedCode("--DerivedRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--DerivedRequires--", ExcludeStatics = true)]
            class DerivedRequires : WithRequires
            {
                public static event EventHandler DerivedStaticEvent;
                public event EventHandler DerivedInstanceEvent;
                private event EventHandler DerivedPrivateInstanceEvent;
            }

            [ExpectedWarning("IL2109", "ReflectionAccessOnEvents.DerivedWithoutRequires", "ReflectionAccessOnEvents.WithRequires")]
            class DerivedWithoutRequires : WithRequires
            {
                public static event EventHandler DerivedStaticEvent;
                public event EventHandler DerivedInstanceEvent;
                private event EventHandler DerivedPrivateInstanceEvent;
            }

            static void ReflectOverSingleEvent()
            {
                typeof(WithRequires).GetEvent(nameof(WithRequires.StaticEvent));
                typeof(DerivedRequires).GetEvent(nameof(DerivedRequires.DerivedStaticEvent));
                typeof(DerivedWithoutRequires).GetEvent(nameof(DerivedWithoutRequires.DerivedStaticEvent));
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            static void ReflectOverAllEvents()
            {
                typeof(WithRequires).GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove", Tool.NativeAot, "")]
            static void DerivedReflectOverAllEvents()
            {
                typeof(DerivedRequires).GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            static void RequiresAllEvents()
            {
                RequiresAllEvents(typeof(WithRequires));

                static void RequiresAllEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] Type t)
                { }
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove", Tool.NativeAot, "")]
            static void DerivedRequiresAllEvents()
            {
                RequiresAllEvents(typeof(DerivedRequires));

                static void RequiresAllEvents([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] Type t)
                { }
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            static void RequiresPublicEvents()
            {
                typeof(WithRequires).RequiresPublicEvents();
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "InstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "InstanceEvent.remove", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedInstanceEvent.remove", Tool.NativeAot, "")]
            static void DerivedRequiresPublicEvents()
            {
                typeof(DerivedRequires).RequiresPublicEvents();
            }

            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(WithRequires), "PrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(WithRequires), "PrivateInstanceEvent.remove", Tool.NativeAot, "")]
            static void RequiresNonPublicEvents()
            {
                typeof(WithRequires).RequiresNonPublicEvents();
            }

            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.add", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove")]
            [ExpectedWarning("IL3050", nameof(DerivedRequires), "DerivedPrivateInstanceEvent.remove", Tool.NativeAot, "")]
            static void DerivedRequiresNonPublicEvents()
            {
                typeof(DerivedRequires).RequiresNonPublicEvents();
            }

            public static void Test()
            {
                ReflectOverSingleEvent();
                ReflectOverAllEvents();
                RequiresAllEvents();
                RequiresPublicEvents();
                RequiresNonPublicEvents();
                DerivedReflectOverAllEvents();
                DerivedRequiresPublicEvents();
                DerivedRequiresNonPublicEvents();
                DerivedRequiresAllEvents();
            }
        }

        class ReflectionAccessOnProperties
        {
            [RequiresUnreferencedCode("--WithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--WithRequires--", ExcludeStatics = true)]
            class WithRequires
            {
                public int InstanceProperty { get; set; }
                public static int StaticProperty { get; set; }
                private static int PrivateStaticProperty { get; set; }
            }

            [RequiresUnreferencedCode("--WithRequiresOnlyInstanceProperties--", ExcludeStatics = true)]
            [RequiresDynamicCode("--WithRequiresOnlyInstanceProperties--", ExcludeStatics = true)]
            class WithRequiresOnlyInstanceProperties
            {
                public int InstanceProperty { get; set; }
            }

            [ExpectedWarning("IL2109", "ReflectionAccessOnProperties.DerivedWithoutRequires", "ReflectionAccessOnProperties.WithRequires")]
            class DerivedWithoutRequires : WithRequires
            {
                public static int DerivedStaticProperty { get; set; }
            }

            [RequiresUnreferencedCode("--DerivedWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--DerivedWithRequires--", ExcludeStatics = true)]
            class DerivedWithRequires : WithRequires
            {
                public static int DerivedStaticProperty { get; set; }
            }

            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequiresOnlyInstanceProperties.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequiresOnlyInstanceProperties.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequiresOnlyInstanceProperties.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequiresOnlyInstanceProperties.InstanceProperty.set", Tool.NativeAot, "")]
            static void TestDAMAccess()
            {
                typeof(WithRequires).RequiresPublicProperties();
                typeof(WithRequires).RequiresNonPublicProperties();
                typeof(WithRequiresOnlyInstanceProperties).RequiresPublicProperties();
                typeof(DerivedWithoutRequires).RequiresPublicProperties();
                typeof(DerivedWithRequires).RequiresPublicProperties();
            }

            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequiresOnlyInstanceProperties.InstanceProperty.get")]
            [ExpectedWarning("IL3050", "WithRequiresOnlyInstanceProperties.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequiresOnlyInstanceProperties.InstanceProperty.set")]
            [ExpectedWarning("IL3050", "WithRequiresOnlyInstanceProperties.InstanceProperty.set", Tool.NativeAot, "")]
            static void TestDirectReflectionAccess()
            {
                typeof(WithRequires).GetProperty(nameof(WithRequires.StaticProperty));
                typeof(WithRequires).GetProperty(nameof(WithRequires.InstanceProperty));
                typeof(WithRequires).GetProperty("PrivateStaticProperty", BindingFlags.NonPublic);
                typeof(WithRequiresOnlyInstanceProperties).GetProperty(nameof(WithRequiresOnlyInstanceProperties.InstanceProperty));
                typeof(DerivedWithoutRequires).GetProperty(nameof(DerivedWithRequires.DerivedStaticProperty)); // Doesn't warn
                typeof(DerivedWithRequires).GetProperty(nameof(DerivedWithRequires.DerivedStaticProperty));
            }

            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.get", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.get", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [ExpectedWarning("IL2026", "WithRequires.InstanceProperty.set", Tool.Trimmer | Tool.NativeAot, "")]
            [ExpectedWarning("IL3050", "WithRequires.InstanceProperty.set", Tool.NativeAot, "")]
            [DynamicDependency(nameof(WithRequires.StaticProperty), typeof(WithRequires))]
            [DynamicDependency(nameof(WithRequires.InstanceProperty), typeof(WithRequires))] // Doesn't warn
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(DerivedWithoutRequires))] // Doesn't warn
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(DerivedWithRequires))]
            static void TestDynamicDependencyAccess()
            {
            }

            [RequiresUnreferencedCode("This class is dangerous", ExcludeStatics = true)]
            [RequiresDynamicCode("This class is dangerous", ExcludeStatics = true)]
            class BaseForDAMAnnotatedClass
            {
                public static int baseProperty { get; set; }
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            [RequiresUnreferencedCode("This class is dangerous", ExcludeStatics = true)]
            [RequiresDynamicCode("This class is dangerous", ExcludeStatics = true)]
            class DAMAnnotatedClass : BaseForDAMAnnotatedClass
            {
                public static int publicProperty
                {
                    get;
                    set;
                }

                static int privateProperty
                {
                    get;
                    set;
                }
            }

            static void TestDAMOnTypeAccess(DAMAnnotatedClass instance)
            {
                instance.GetType().GetProperty("publicProperty");
            }

            [ExpectedWarning("IL2026", "DAMAnnotatedClass.DAMAnnotatedClass()")]
            [ExpectedWarning("IL3050", "DAMAnnotatedClass.DAMAnnotatedClass()", Tool.Analyzer | Tool.NativeAot, "NativeAOT-specific warning")]
            public static void Test()
            {
                TestDAMAccess();
                TestDirectReflectionAccess();
                TestDynamicDependencyAccess();
                TestDAMOnTypeAccess(new DAMAnnotatedClass());
            }
        }

        [RequiresUnreferencedCode("The attribute is dangerous", ExcludeStatics = true)]
        [RequiresDynamicCode("The attribute is dangerous", ExcludeStatics = true)]
        public class AttributeWithRequires : Attribute
        {
            public static int field;

            // `field` cannot be used as named attribute argument because is static, and if accessed via
            // a property the property will be the one generating the warning, but then the warning will
            // be suppressed by the Requires on the declaring type
            public int PropertyOnAttribute
            {
                get { return field; }
                set { field = value; }
            }
        }

        [AttributeWithRequires(PropertyOnAttribute = 42)]
        [ExpectedWarning("IL2026", "AttributeWithRequires.AttributeWithRequires()")]
        [ExpectedWarning("IL3050", "AttributeWithRequires.AttributeWithRequires()", Tool.Analyzer | Tool.NativeAot, "NativeAOT-specific warning")]
        static void KeepFieldOnAttributeInner() { }

        static void KeepFieldOnAttribute()
        {
            KeepFieldOnAttributeInner();

            // NativeAOT only considers attribute on reflection visible members
            typeof(RequiresOnClassExcludeStatics).GetMethod(nameof(KeepFieldOnAttributeInner), BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { });
        }

        public class AttributeParametersAndProperties
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            public static Type AnnotatedField;

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
            public class AttributeWithRequirementsOnParameters : Attribute
            {
                public AttributeWithRequirementsOnParameters()
                {
                }

                public AttributeWithRequirementsOnParameters([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
                {
                }

                public int PropertyWithRequires
                {
                    get => 0;

                    [RequiresUnreferencedCode("--PropertyWithRequires--", ExcludeStatics = true)]
                    [RequiresDynamicCode("--PropertyWithRequires--", ExcludeStatics = true)]
                    set { }
                }

                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                public Type AnnotatedField;
            }

            [RequiresUnreferencedCode("--AttributeParametersAndProperties--", ExcludeStatics = true)]
            [RequiresDynamicCode("--AttributeParametersAndProperties--", ExcludeStatics = true)]
            class TestClass
            {
                [ExpectedWarning("IL2110", "AttributeParametersAndProperties.AnnotatedField", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
                [ExpectedWarning("IL2110", "AttributeParametersAndProperties.AnnotatedField", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
                [ExpectedWarning("IL2026", "AttributeParametersAndProperties.AttributeWithRequirementsOnParameters.PropertyWithRequires.set", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
                [ExpectedWarning("IL3050", "AttributeParametersAndProperties.AttributeWithRequirementsOnParameters.PropertyWithRequires.set", Tool.Analyzer, "NativeAOT-specific warning, https://github.com/dotnet/runtime/issues/117899")]
                [AttributeWithRequirementsOnParameters(typeof(AttributeParametersAndProperties))]
                [AttributeWithRequirementsOnParameters(PropertyWithRequires = 1)]
                [AttributeWithRequirementsOnParameters(AnnotatedField = typeof(AttributeParametersAndProperties))]
                public static void Test() { }
            }

            public static void Test()
            {

                TestClass.Test();
            }
        }

        class RequiresOnCtorAttribute : Attribute
        {
            [RequiresUnreferencedCode("--RequiresOnCtorAttribute--", ExcludeStatics = true)]
            public RequiresOnCtorAttribute()
            {
            }
        }

        class MembersOnClassWithRequires<T>
        {
            public class RequiresAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] U>
            {
            }

            class RequiresNew<T> where T : new()
            {
            }

            [RequiresUnreferencedCode("--ClassWithRequires--", ExcludeStatics = true)]
            public class ClassWithRequires
            {
                [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
                public static RequiresAll<T> field;

                [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/108523")]
                public RequiresAll<T> instanceField;

                [RequiresOnCtor]
                [ExpectedWarning("IL2026", "--RequiresOnCtorAttribute--", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
                public static int fieldWithAttribute;

                // Instance fields get attribute warnings but static fields don't.
                [ExpectedWarning("IL2026", "--RequiresOnCtorAttribute--", Tool.Trimmer, "https://github.com/dotnet/linker/issues/3140")]
                [RequiresOnCtor]
                public int instanceFieldWithAttribute;

                [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
                public static void GenericMethod<U>(RequiresAll<U> r) { }

                public void GenericInstanceMethod<U>(RequiresAll<U> r) { }

                [ExpectedWarning("IL2026", "--RequiresOnCtorAttribute--", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
                [RequiresOnCtor]
                public static void MethodWithAttribute() { }

                [RequiresOnCtor]
                public void InstanceMethodWithAttribute() { }

                // NOTE: The enclosing RUC does not apply to nested types.
                [ExpectedWarning("IL2091")]
                public class ClassWithWarning : RequiresAll<T>
                {
                    [ExpectedWarning("IL2091")]
                    public ClassWithWarning()
                    {
                    }
                }

                // NOTE: The enclosing RUC does not apply to nested types.
                [ExpectedWarning("IL2026", "--RequiresOnCtorAttribute--")]
                [RequiresOnCtor]
                public class ClassWithAttribute
                {
                }
            }

            // This warning should ideally be suppressed by the RUC on the type:
            [UnexpectedWarning("IL2091", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/108523")]
            [RequiresUnreferencedCode("--GenericClassWithWarningWithRequires--", ExcludeStatics = true)]
            public class GenericClassWithWarningWithRequires<U> : RequiresAll<U>
            {
            }

            // This warning should ideally be suppressed by the RUC on the type:
            [UnexpectedWarning("IL2091", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/108523")]
            [RequiresUnreferencedCode("--ClassWithWarningWithRequires--", ExcludeStatics = true)]
            public class ClassWithWarningWithRequires : RequiresAll<T>
            {
            }

            [ExpectedWarning("IL2026", "ClassWithRequires()", "--ClassWithRequires--")]
            class ClassWithWarningOnGenericArgumentConstructor : RequiresNew<ClassWithRequires>
            {
                // Analyzer misses warning for implicit call to the base constructor, because the new constraint is not checked in dataflow.
                [ExpectedWarning("IL2026", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/108507")]
                public ClassWithWarningOnGenericArgumentConstructor()
                {
                }
            }

            [UnexpectedWarning("IL2026", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/108507")]
            [RequiresUnreferencedCode("--ClassWithWarningOnGenericArgumentConstructorWithRequires--", ExcludeStatics = true)]
            class ClassWithWarningOnGenericArgumentConstructorWithRequires : RequiresNew<ClassWithRequires>
            {
            }

            [UnexpectedWarning("IL2091", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/108523")]
            [RequiresUnreferencedCode("--GenericAnnotatedWithWarningWithRequires--", ExcludeStatics = true)]
            public class GenericAnnotatedWithWarningWithRequires<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TFields> : RequiresAll<TFields>
            {
            }

            [ExpectedWarning("IL2026", "--GenericClassWithWarningWithRequires--")]
            [ExpectedWarning("IL2026", "--ClassWithWarningWithRequires--")]
            [ExpectedWarning("IL2026", "--ClassWithWarningOnGenericArgumentConstructorWithRequires--")]
            [ExpectedWarning("IL2026", "--GenericAnnotatedWithWarningWithRequires--")]
            [ExpectedWarning("IL2091", Tool.Trimmer, "")]
            public static void Test(ClassWithRequires inst = null)
            {
                var f = ClassWithRequires.field;
                f = inst.instanceField;
                int i = ClassWithRequires.fieldWithAttribute;
                i = inst.instanceFieldWithAttribute;
                ClassWithRequires.GenericMethod<int>(new());
                inst.GenericInstanceMethod<int>(new());
                ClassWithRequires.MethodWithAttribute();
                inst.InstanceMethodWithAttribute();
                var c = new ClassWithRequires.ClassWithWarning();
                var d = new ClassWithRequires.ClassWithAttribute();
                var g = new GenericClassWithWarningWithRequires<int>();
                var h = new ClassWithWarningWithRequires();
                var j = new ClassWithWarningOnGenericArgumentConstructor();
                var k = new ClassWithWarningOnGenericArgumentConstructorWithRequires();
                var l = new GenericAnnotatedWithWarningWithRequires<int>();
            }
        }

        class ConstFieldsOnClassWithRequires
        {
            [RequiresUnreferencedCode("--ConstClassWithRequires--", ExcludeStatics = true)]
            [RequiresDynamicCode("--ConstClassWithRequires--", ExcludeStatics = true)]
            class ConstClassWithRequires
            {
                public const string Message = "Message";
                public const int Number = 42;

                public static void Method() { }
            }

            static void TestClassWithRequires()
            {
                var a = ConstClassWithRequires.Message;
                var b = ConstClassWithRequires.Number;

                ConstClassWithRequires.Method();
            }

            [RequiresUnreferencedCode(ConstClassWithRequiresUsingField.Message, ExcludeStatics = true)]
            [RequiresDynamicCode(ConstClassWithRequiresUsingField.Message, ExcludeStatics = true)]
            class ConstClassWithRequiresUsingField
            {
                public const string Message = "--ConstClassWithRequiresUsingField--";

                public static void Method() { }
            }

            static void TestClassUsingFieldInAttribute()
            {
                ConstClassWithRequiresUsingField.Method();
            }

            public static void Test()
            {
                TestClassWithRequires();
                TestClassUsingFieldInAttribute();
            }
        }
    }
}
