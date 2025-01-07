// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

using BindingFlags = System.Reflection.BindingFlags;

class FeatureSwitches
{
    // These are substituted using the XML file
    // We're testing that the basic blocks that are known to be unreachable don't get compiled.
    static bool IsEnabled() => true;
    static int GetIntConstant() => 0;
    static bool s_isEnabled = IsEnabled();

    // These are substituted by embedded XML file and re-substituted by XML passed on command line
    static int GetCookie() => 0;

    public static int Run()
    {
        SanityTest.Run();
        SimpleTest.Run();
        TestInapplicableCatch.Run();
        TestEmptyFinally.Run();
        TestStaticField.Run();
        TestIntConstant.Run();
        TestResubstitution.Run();
        TestResourceStripping.Run();
        
        return 100;
    }

    class SanityTest
    {
        class PresentType { }

        class NotPresentType { }

        public static void Run()
        {
            EnsurePresent(typeof(PresentType));

            if (!IsTypePresent(typeof(SanityTest), nameof(PresentType)))
                throw new Exception();

            ThrowIfPresent(typeof(SanityTest), nameof(NotPresentType));
        }
    }

    class SimpleTest
    {
        class NotPresentType { }

        public static void Run()
        {
            // repeat 5 times to get past the stloc.0/1/2/3 and into stloc.s
            if (IsEnabled())
            {
                EnsurePresent(typeof(NotPresentType));
            }
            if (IsEnabled())
            {
                EnsurePresent(typeof(NotPresentType));
            }
            if (IsEnabled())
            {
                EnsurePresent(typeof(NotPresentType));
            }
            if (IsEnabled())
            {
                EnsurePresent(typeof(NotPresentType));
            }
            if (IsEnabled())
            {
                EnsurePresent(typeof(NotPresentType));
            }

            ThrowIfPresent(typeof(SimpleTest), nameof(NotPresentType));
        }
    }

    class TestInapplicableCatch
    {
        class NotPresentType { }

        public static void Run()
        {
            if (IsEnabled())
            {
                try
                {
                    throw null;
                }
                catch (NullReferenceException)
                {
                    EnsurePresent(typeof(NotPresentType));
                }
            }

            ThrowIfPresent(typeof(TestInapplicableCatch), nameof(NotPresentType));
        }
    }

    class TestEmptyFinally
    {
        class NotPresentType { }

        public static void Run()
        {
            try
            {

            }
            finally
            {
                if (IsEnabled())
                {
                    EnsurePresent(typeof(NotPresentType));
                }
            }

            ThrowIfPresent(typeof(TestEmptyFinally), nameof(NotPresentType));
        }
    }

    class TestStaticField
    {
        class NotPresentType { }

        public static void Run()
        {
            if (s_isEnabled)
            {
                EnsurePresent(typeof(NotPresentType));
            }

            ThrowIfPresent(typeof(TestStaticField), nameof(NotPresentType));
        }
    }

    class TestIntConstant
    {
        class NotPresentType { }

        public static void Run()
        {
            if (GetIntConstant() != 42)
            {
                EnsurePresent(typeof(NotPresentType));
            }

            ThrowIfPresent(typeof(TestIntConstant), nameof(NotPresentType));
        }
    }

    class TestResubstitution
    {
        public static void Run()
        {
            if (GetCookie() != 100)
                throw new Exception(GetCookie().ToString());
        }
    }

    class TestResourceStripping
    {
        public static void Run()
        {
            string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Console.WriteLine("Resource list");
            foreach (string n in names)
                Console.WriteLine(n);
            if (names.Length != 1 || names[0] != "KeptResource")
                throw new Exception();
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsTypePresent(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public) != null;

    private static void ThrowIfPresent(Type testType, string typeName)
    {
        if (IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }

    private static void EnsurePresent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (type.GetConstructors().Length != 1)
            throw new Exception();
    }
}
