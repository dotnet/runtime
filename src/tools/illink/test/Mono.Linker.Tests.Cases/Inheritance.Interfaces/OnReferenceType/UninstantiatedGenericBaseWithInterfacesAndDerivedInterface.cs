using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
    public class UninstantiatedGenericBaseWithInterfacesAndDerivedInterface
    {
        [Kept]
        public static void Main()
        {
            Console.WriteLine(typeof(Derived<>).Name);
            UseIBar(null);
        }

        [Kept]
        static void UseIBar(IBar<int> bar)
        {
            bar.Bar();
            ((IFoo)bar).Foo();
        }

        [Kept]
        interface IFoo
        {
            [Kept]
            void Foo();
        }

        [Kept]
        [KeptInterface(typeof(IFoo))]
        interface IBar<T> : IFoo
        {
            [Kept]
            void Bar();
        }

        interface IDerived<T> : IBar<T>
        {
        }

        [Kept]
        [KeptInterface(typeof(IBar<>), "T")]
        [KeptInterface(typeof(IFoo))]
        abstract class Base<T> : IBar<T>
        {
            [Kept]
            public void Bar()
            {
            }

            [Kept]
            void IFoo.Foo()
            {
            }
        }

        [Kept]
        [KeptBaseType(typeof(Base<>), "T")]
        [KeptInterface(typeof(IBar<>), "T")]
        [KeptInterface(typeof(IFoo))]
        class Derived<T> : Base<T>, IDerived<T>
        {
        }
    }
}
