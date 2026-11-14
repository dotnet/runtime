using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

#if NATIVEAOT
using Mono.Linker.Tests.Cases.Expectations.Helpers;
#endif

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
    class DynamicDependencyMethod
    {
        public static void Main()
        {
            new B(); // Needed to avoid lazy body marking stubbing

            B.Method();
            B.SameContext();
            B.Broken();
            B.Conditional();
            ExplicitInterfaceMembers();
#if NATIVEAOT
            ReferenceViaReflection.Test();
#endif
        }

        const string ExplicitInterface = "Mono#Linker#Tests#Cases#DynamicDependencies#DynamicDependencyMethod#";

        [Kept]
        [DynamicDependency(ExplicitInterface + "IGeneric{System#String}#M(System.String)", typeof(ExplicitImplementation))]
        [DynamicDependency(ExplicitInterface + "IGeneric<System#String>#M(System.String)", typeof(ExplicitImplementation))]
        [DynamicDependency(ExplicitInterface + "IDict{System#String,System#Int32}#M(System.Collections.Generic.Dictionary{System.String,System.Int32})", typeof(ExplicitImplementation))]
        [DynamicDependency(ExplicitInterface + "IDict<System#String,System#Int32>#M(System.Collections.Generic.Dictionary{System.String,System.Int32})", typeof(ExplicitImplementation))]
        [DynamicDependency(ExplicitInterface + "IGeneric{System#Collections#Generic#List{System#Collections#Generic#Dictionary{System#String,System#Int32}}}#M", typeof(ExplicitImplementation))]
        [DynamicDependency(ExplicitInterface + "IGeneric<System#Collections#Generic#List<System#Collections#Generic#Dictionary<System#String,System#Int32>>>#M", typeof(ExplicitImplementation))]
        static void ExplicitInterfaceMembers() { }

        [Kept(By = Tool.Trimmer)]
        interface IGeneric<T>
        {
            [Kept(By = Tool.Trimmer)]
            void M(T value);
        }

        [Kept(By = Tool.Trimmer)]
        interface IDict<K, V>
        {
            [Kept(By = Tool.Trimmer)]
            void M(Dictionary<K, V> value);
        }

        [Kept]
        [KeptInterface(typeof(IGeneric<string>), By = Tool.Trimmer)]
        [KeptInterface(typeof(IDict<string, int>), By = Tool.Trimmer)]
        [KeptInterface(typeof(IGeneric<List<Dictionary<string, int>>>), By = Tool.Trimmer)]
        class ExplicitImplementation : IGeneric<string>, IDict<string, int>, IGeneric<List<Dictionary<string, int>>>
        {
            [Kept]
            void IGeneric<string>.M(string value) { }

            [Kept]
            void IDict<string, int>.M(Dictionary<string, int> value) { }

            [Kept]
            void IGeneric<List<Dictionary<string, int>>>.M(List<Dictionary<string, int>> value) { }
        }

        [KeptMember(".ctor()")]
        class B
        {
            [Kept]
            int field;

            [Kept]
            void Method2(out sbyte arg)
            {
                arg = 1;
            }

            [Kept]
            [DynamicDependency("Dependency1()", typeof(C))]
            [DynamicDependency("Dependency2``1(``0[],System.Int32", typeof(C))]
            [DynamicDependency("Dependency3", typeof(C))]
            [DynamicDependency("RecursiveDependency", typeof(C))]
            [DynamicDependency("#ctor()", typeof(C))] // To avoid lazy body marking stubbing
            [DynamicDependency("field", typeof(C))]
            [DynamicDependency("NextOne(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Nested@)", typeof(Nested))]
            [DynamicDependency("#cctor()", typeof(Nested))]
            // Dependency on a property itself should be expressed as a dependency on one or both accessor methods
            [DynamicDependency("get_Property()", typeof(C))]
            [DynamicDependency("get_Property2", typeof(C))]
            [DynamicDependency("M``1(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.S{" +
                "Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.G{" +
                    "Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.A,``0}}" +
                    "[0:,0:,0:][][][0:,0:]@)", typeof(Complex))]
            public static void Method()
            {
            }

            [Kept]
            [DynamicDependency("field")]
            [DynamicDependency("Method2(System.SByte@)")]
            public static void SameContext()
            {
            }

            [Kept]

            [ExpectedWarning("IL2037", "MissingMethod", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
            [DynamicDependency("MissingMethod", typeof(C))]

            [ExpectedWarning("IL2037", "Dependency2``1(``0,System.Int32,System.Object)", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
            [DynamicDependency("Dependency2``1(``0,System.Int32,System.Object)", typeof(C))]

            [ExpectedWarning("IL2037", "''", "'Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B'")]
            [DynamicDependency("")]

            [ExpectedWarning("IL2037", "#ctor()", "'Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.NestedStruct'")]
            [DynamicDependency("#ctor()", typeof(NestedStruct))]

            [ExpectedWarning("IL2037", "#cctor()", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
            [DynamicDependency("#cctor()", typeof(C))]

            [ExpectedWarning("IL2036", "NonExistentType")]
            [DynamicDependency("method", "NonExistentType", "test")]
            public static void Broken()
            {
            }

            [Kept]
            [DynamicDependency("ConditionalTest()", typeof(C), Condition = "don't have it")]
            public static void Conditional()
            {
            }
        }

        class Nested
        {
            [Kept]
            private static void NextOne(ref Nested arg1)
            {
            }

            [Kept]
            static Nested()
            {

            }
        }

        class Complex
        {
            [Kept(By = Tool.Trimmer)]
            public struct S<T> { }
            [Kept(By = Tool.Trimmer)]
            public class A { }
            [Kept(By = Tool.Trimmer)]
            public class G<T, U> { }

            [Kept]
            public void M<V>(ref S<G<A, V>>[,][][][,,] a)
            {
            }
        }

        struct NestedStruct
        {
            public string Name;

            public NestedStruct(string name)
            {
                Name = name;
            }
        }
    }

    [KeptMember(".ctor()")]
    class C
    {
        [Kept]
        internal string field;

        [Kept]
        internal void Dependency1()
        {
        }

        internal void Dependency1(long arg1)
        {
        }

        [Kept]
        internal void Dependency2<T>(T[] arg1, int arg2)
        {
        }

        [Kept]
        internal void Dependency3(string str)
        {
        }

        [Kept]
        [DynamicDependency("#ctor", typeof(NestedInC))]
        internal void RecursiveDependency()
        {
        }

        [KeptMember(".ctor()")]
        class NestedInC
        {
        }

        [Kept]
        [KeptBackingField]
        internal string Property { [Kept] get; set; }

        [Kept]
        [KeptBackingField]
        internal string Property2 { [Kept] get; set; }

        // For now, Condition has no effect: https://github.com/dotnet/linker/issues/1231
        [Kept]
        internal void ConditionalTest()
        {
        }
    }
#if NATIVEAOT
    abstract class ReferenceViaReflection
    {
        [Kept]
        [DynamicDependency(nameof(TargetMethodViaReflection))]
        public static void SourceMethodViaReflection() { }

        [Kept]
        private static void TargetMethodViaReflection() { }


        [Kept]
        public static void Test()
        {
            var i = new Impl(); // Avoid removal of non-implemented abstract methods

            typeof(ReferenceViaReflection).RequiresPublicMethods();
            typeof(AbstractMethods).RequiresPublicMethods();
        }

        [KeptMember(".ctor()")]
        private abstract class AbstractMethods
        {
            [Kept]
            [DynamicDependency(nameof(TargetMethod))]
            public abstract void SourceAbstractViaReflection();

            [Kept]
            private static void TargetMethod() { }
        }

        [KeptMember(".ctor()")]
        private class Impl : AbstractMethods
        {
            [Kept]
            public override void SourceAbstractViaReflection() { }
        }
    }
#endif
}
