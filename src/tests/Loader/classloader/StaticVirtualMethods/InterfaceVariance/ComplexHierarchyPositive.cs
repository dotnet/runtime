// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

// This regression test tracks the issue where variant static interface dispatch crashes the runtime, and behaves incorrectly

namespace VariantStaticInterfaceDispatchRegressionTest
{
    class Test
    {
        static int Main()
        {
            Console.WriteLine("Instance test cases");

            Console.WriteLine("---FooBar");
            Console.WriteLine(GetTheFooStringInstance<FooBar, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBar, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBar, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBar, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBar, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBar, Derived>());

            Console.WriteLine("---FooBar2");
            Console.WriteLine(GetTheFooStringInstance<FooBar2, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBar2, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBar2, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBar2, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBar2, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBar2, Derived>());

            Console.WriteLine("---FooBarBaz");
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBarBaz, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBaz, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBaz, Derived>());

            Console.WriteLine("---FooBarBazBoz");
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz, Derived>());

            Console.WriteLine("---FooBarBaz2");
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz2, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz2, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBaz2, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBarBaz2, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBaz2, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBaz2, Derived>());

            Console.WriteLine("---FooBarBazBoz2");
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz2, Base>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz2, Mid>());
            Console.WriteLine(GetTheFooStringInstance<FooBarBazBoz2, Derived>());

            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz2, Base>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz2, Mid>());
            Console.WriteLine(GetTheBarStringInstance<FooBarBazBoz2, Derived>());

            Console.WriteLine("Static test cases");

            Console.WriteLine("---FooBar");
            Console.WriteLine(GetTheFooString<FooBar, Base>());
            Console.WriteLine(GetTheFooString<FooBar, Mid>());
            Console.WriteLine(GetTheFooString<FooBar, Derived>());

            Console.WriteLine(GetTheBarString<FooBar, Base>());
            Console.WriteLine(GetTheBarString<FooBar, Mid>());
            Console.WriteLine(GetTheBarString<FooBar, Derived>());

            Console.WriteLine("---FooBar2");
            Console.WriteLine(GetTheFooString<FooBar2, Base>());
            Console.WriteLine(GetTheFooString<FooBar2, Mid>());
            Console.WriteLine(GetTheFooString<FooBar2, Derived>());

            Console.WriteLine(GetTheBarString<FooBar2, Base>());
            Console.WriteLine(GetTheBarString<FooBar2, Mid>());
            Console.WriteLine(GetTheBarString<FooBar2, Derived>());

            Console.WriteLine("---FooBarBaz");
            Console.WriteLine(GetTheFooString<FooBarBaz, Base>());
            Console.WriteLine(GetTheFooString<FooBarBaz, Mid>());
            Console.WriteLine(GetTheFooString<FooBarBaz, Derived>());

            Console.WriteLine(GetTheBarString<FooBarBaz, Base>());
            Console.WriteLine(GetTheBarString<FooBarBaz, Mid>());
            Console.WriteLine(GetTheBarString<FooBarBaz, Derived>());

            Console.WriteLine("---FooBarBazBoz");
            Console.WriteLine(GetTheFooString<FooBarBazBoz, Base>());
            Console.WriteLine(GetTheFooString<FooBarBazBoz, Mid>());
            Console.WriteLine(GetTheFooString<FooBarBazBoz, Derived>());

            Console.WriteLine(GetTheBarString<FooBarBazBoz, Base>());
            Console.WriteLine(GetTheBarString<FooBarBazBoz, Mid>());
            Console.WriteLine(GetTheBarString<FooBarBazBoz, Derived>());

            Console.WriteLine("---FooBarBaz2");
            Console.WriteLine(GetTheFooString<FooBarBaz2, Base>());
            Console.WriteLine(GetTheFooString<FooBarBaz2, Mid>());
            Console.WriteLine(GetTheFooString<FooBarBaz2, Derived>());

            Console.WriteLine(GetTheBarString<FooBarBaz2, Base>());
            Console.WriteLine(GetTheBarString<FooBarBaz2, Mid>());
            Console.WriteLine(GetTheBarString<FooBarBaz2, Derived>());

            Console.WriteLine("---FooBarBazBoz2");
            Console.WriteLine(GetTheFooString<FooBarBazBoz2, Base>());
            Console.WriteLine(GetTheFooString<FooBarBazBoz2, Mid>());
            Console.WriteLine(GetTheFooString<FooBarBazBoz2, Derived>());

            Console.WriteLine(GetTheBarString<FooBarBazBoz2, Base>());
            Console.WriteLine(GetTheBarString<FooBarBazBoz2, Mid>());
            Console.WriteLine(GetTheBarString<FooBarBazBoz2, Derived>());

            return 100;
        }

        static string GetTheFooString<T, U>() where T : IFoo<U> { try { return T.GetString(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheBarString<T, U>() where T : IBar<U> { try { return T.GetString(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheFooStringInstance<T, U>() where T : IFoo<U>, new() { try { return (new T()).GetStringInstance(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }
        static string GetTheBarStringInstance<T, U>() where T : IBar<U>, new() { try { return (new T()).GetStringInstance(); } catch (AmbiguousImplementationException) { return "AmbiguousImplementationException"; } }

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
