// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

class Program
{
    static int Main()
    {
        BodyFoldingTest.Run();
        DiagnosticMethodInfoTests.Run();
        Test108688Regression.Run();

        string stackTrace = Environment.StackTrace;

        Console.WriteLine(stackTrace);

#if STRIPPED
        const bool expected = false;
#else
        const bool expected = true;
#endif
        bool actual = stackTrace.Contains(nameof(Main)) && stackTrace.Contains(nameof(Program));
        return expected == actual ? 100 : 1;
    }

    class DiagnosticMethodInfoTests
    {
        public static void Run()
        {
#if STRIPPED
            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(new StackFrame());
            if (dmi != null)
                throw new Exception("Succeeded in creating DiagnosticMethodInfo despite no expectation");
#else
            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(new StackFrame());
            if (dmi == null)
                throw new Exception("No DiagnosticMethodInfo despite no expectation");
            if (dmi.Name != nameof(Run))
                throw new Exception($"Name is {dmi.Name} from {dmi.DeclaringTypeName}");

            StackTrace tr = NonGenericStackTraceClass.TestNonGeneric();

            Verify(tr.GetFrame(0), "Test", "GenericStackTraceClass`1+Nested");
            Verify(tr.GetFrame(1), "TestGeneric", "GenericStackTraceClass`1");
            Verify(tr.GetFrame(2), "TestNonGeneric", "GenericStackTraceClass`1");
            Verify(tr.GetFrame(3), "TestGeneric", "NonGenericStackTraceClass");
            Verify(tr.GetFrame(4), "TestNonGeneric", "NonGenericStackTraceClass");

            static void Verify(StackFrame fr, string expectedName, string expectedDeclaringName)
            {
                DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(fr);
                if (expectedName != dmi.Name)
                    throw new Exception($"{expectedName} != {dmi.Name}");
                if (!dmi.DeclaringTypeName.EndsWith(expectedDeclaringName))
                    throw new Exception($"!{dmi.DeclaringTypeName}.EndsWith({expectedDeclaringName})");
            }
#endif
        }

        class NonGenericStackTraceClass
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static StackTrace TestNonGeneric() => TestGeneric<int>();

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static StackTrace TestGeneric<T>() => GenericStackTraceClass<object>.TestNonGeneric();
        }

        class GenericStackTraceClass<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static StackTrace TestNonGeneric() => TestGeneric<object>();

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static StackTrace TestGeneric<U>() => Nested.Test();

            public class Nested
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static StackTrace Test() => new StackTrace();
            }
        }
    }

    class Test108688Regression
    {
        public static void Run()
        {
            {
                DelStruct s;
                Action del = s.InstanceMethod;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct s;
                Action del = s.InstanceGenericMethod<int>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct s;
                Action del = s.InstanceGenericMethod<object>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<int> s;
                Action del = s.InstanceMethod;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<int> s;
                Action del = s.InstanceGenericMethod<int>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<int> s;
                Action del = s.InstanceGenericMethod<object>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<object> s;
                Action del = s.InstanceMethod;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<object> s;
                Action del = s.InstanceGenericMethod<int>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }

            {
                DelStruct<object> s;
                Action del = s.InstanceGenericMethod<object>;
                var dmi = DiagnosticMethodInfo.Create(del);
#if STRIPPED
                if (dmi != null)
                    throw new Exception();
#else
                if (dmi.Name != nameof(DelStruct.InstanceGenericMethod))
                    throw new Exception();
                // Need to make sure it was stack trace metadata and not reflection metadata that provided this
                if (GetMethodSecretly(del.Target.GetType(), dmi.Name) != null)
                    throw new Exception();
#endif
            }
        }

        [UnconditionalSuppressMessage("", "IL2070")]
        private static MethodInfo GetMethodSecretly(Type t, string name)
            => t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        struct DelStruct
        {
            public void InstanceMethod() { }
            public void InstanceGenericMethod<T>() { }
        }

        struct DelStruct<T>
        {
            public void InstanceMethod() { }
            public void InstanceGenericMethod<U>() { }
        }
    }

}
