// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using Xunit;

// This regression test tracks the issue where variant static interface dispatch crashes the runtime, and behaves incorrectly

namespace VariantStaticInterfaceDispatchRegressionTest
{
    public class Test
    {
        [Fact]
        public static void TestEntryPoint()
        {
            Console.WriteLine("Test cases");

            Console.WriteLine("---FooBar");
            TestTheFooString<FooBar, Base>("IFoo<Base>");
            TestTheFooString<FooBar, Mid>("IFoo<Base>");
            TestTheFooString<FooBar, Derived>("IFoo<Base>");

            TestTheBarString<FooBar, Base>("IBar<Derived>");
            TestTheBarString<FooBar, Mid>("IBar<Derived>");
            TestTheBarString<FooBar, Derived>("IBar<Derived>");

            Console.WriteLine("---FooBar2");
            TestTheFooString<FooBar2, Base>("IFoo<Base>");
            TestTheFooString<FooBar2, Mid>("IFoo<Mid>");
            TestTheFooString<FooBar2, Derived>("IFoo<Base>");

            TestTheBarString<FooBar2, Base>("IBar<Derived>");
            TestTheBarString<FooBar2, Mid>("IBar<Mid>");
            TestTheBarString<FooBar2, Derived>("IBar<Derived>");

            Console.WriteLine("---FooBarBaz");
            TestTheFooString<FooBarBaz, Base>("IFoo<Base>");
            TestTheFooString<FooBarBaz, Mid>("IBaz");
            TestTheFooString<FooBarBaz, Derived>("IBaz");

            TestTheBarString<FooBarBaz, Base>("IBaz");
            TestTheBarString<FooBarBaz, Mid>("IBaz");
            TestTheBarString<FooBarBaz, Derived>("IBar<Derived>");

            Console.WriteLine("---FooBarBazBoz");
            TestTheFooString<FooBarBazBoz, Base>("IBoz");
            TestTheFooString<FooBarBazBoz, Mid>("IBaz");
            TestTheFooString<FooBarBazBoz, Derived>("IBoz");

            TestTheBarString<FooBarBazBoz, Base>("IBoz");
            TestTheBarString<FooBarBazBoz, Mid>("IBaz");
            TestTheBarString<FooBarBazBoz, Derived>("IBoz");

            Console.WriteLine("---FooBarBaz2");
            TestTheFooString<FooBarBaz2, Base>("IFoo<Base>");
            TestTheFooString<FooBarBaz2, Mid>("IBaz");
            TestTheFooString<FooBarBaz2, Derived>("IFoo<Base>");

            TestTheBarString<FooBarBaz2, Base>("IBar<Derived>");
            TestTheBarString<FooBarBaz2, Mid>("IBaz");
            TestTheBarString<FooBarBaz2, Derived>("IBar<Derived>");

            Console.WriteLine("---FooBarBazBoz2");
            TestTheFooString<FooBarBazBoz2, Base>("IBoz");
            TestTheFooString<FooBarBazBoz2, Mid>("IBaz");
            TestTheFooString<FooBarBazBoz2, Derived>("IBoz");

            TestTheBarString<FooBarBazBoz2, Base>("IBoz");
            TestTheBarString<FooBarBazBoz2, Mid>("IBaz");
            TestTheBarString<FooBarBazBoz2, Derived>("IBoz");
        }

        static string GetTheFooString<T, U>() where T : IFoo<U> { try { return T.GetString(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheBarString<T, U>() where T : IBar<U> { try { return T.GetString(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheFooStringInstance<T, U>() where T : IFoo<U>, new() { try { return (new T()).GetStringInstance(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheBarStringInstance<T, U>() where T : IBar<U>, new() { try { return (new T()).GetStringInstance(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }

        static void TestTheFooString<T, U>(string expected) where T : IFoo<U>, new()
        {
            Console.WriteLine($"TestTheFooString {typeof(T).Name} {typeof(T).Name} {expected}");
            Assert.Equal(expected, GetTheFooString<T, U>());
            Assert.Equal(expected, GetTheFooStringInstance<T, U>());
        }

        static void TestTheBarString<T, U>(string expected) where T : IBar<U>, new()
        {
            Console.WriteLine($"TestTheBarString {typeof(T).Name} {typeof(T).Name} {expected}");
            Assert.Equal(expected, GetTheBarString<T, U>());
            Assert.Equal(expected, GetTheBarStringInstance<T, U>());
        }

        interface IFoo<in T>
        {
            static virtual string GetString() => $"IFoo<{typeof(T).Name}>";
            virtual string GetStringInstance() => $"IFoo<{typeof(T).Name}>";
        };

        interface IBar<out T>
        {
            static virtual string GetString() => $"IBar<{typeof(T).Name}>";
            virtual string GetStringInstance() => $"IBar<{typeof(T).Name}>";
        };


        interface IBaz : IFoo<Mid>, IBar<Mid>
        {
            static string IFoo<Mid>.GetString() => "IBaz";
            static string IBar<Mid>.GetString() => "IBaz";
            string IFoo<Mid>.GetStringInstance() => "IBaz";
            string IBar<Mid>.GetStringInstance() => "IBaz";
        }

        interface IBoz : IFoo<Base>, IBar<Derived>
        {
            static string IFoo<Base>.GetString() => "IBoz";
            static string IBar<Derived>.GetString() => "IBoz";
            string IFoo<Base>.GetStringInstance() => "IBoz";
            string IBar<Derived>.GetStringInstance() => "IBoz";
        }

        class FooBar : IFoo<Base>, IBar<Derived> { }
        class FooBar2 : IFoo<Base>, IBar<Derived>, IFoo<Mid>, IBar<Mid> { }
        class FooBarBaz : FooBar, IBaz { }
        class FooBarBaz2 : IFoo<Base>, IBar<Derived>, IBaz { } // Implementation with all interfaces defined on the same type
        class FooBarBazBoz : FooBarBaz, IBoz { }
        class FooBarBazBoz2 : IFoo<Base>, IBar<Derived>, IBaz, IBoz { } // Implementation with all interfaces defined on the same type

        class Base { }
        class Mid : Base { }
        class Derived : Mid { }
    }
}
