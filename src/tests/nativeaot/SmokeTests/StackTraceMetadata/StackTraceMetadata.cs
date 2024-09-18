// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

class Program
{
    static int Main()
    {
        BodyFoldingTest.Run();
        DiagnosticMethodInfoTests.Run();

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
}
