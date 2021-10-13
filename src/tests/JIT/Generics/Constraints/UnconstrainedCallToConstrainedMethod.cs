// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

namespace UnconstrainedCallToConstrainedMethod
{
    interface IConstraint<T> { }
    interface IConstraintCurious<T> where T : IConstraintCurious<T> { }

    class ClassThatImplementsConstraintCurious : IConstraintCurious<ClassThatImplementsConstraintCurious> { }

    class ClassThatImplementsConstraint : IConstraint<object> { }

    struct StructThatImplementsConstraint : IConstraint<int>, IConstraint<object> { }

    struct StructThatImplementsConstraintCurious : IConstraintCurious<StructThatImplementsConstraintCurious> { }



    class TestClass
    {
        static int s_failures = 0;

        static void TestHelperMethodRequiresConstraint_TEMPORARY<T, U>(bool expectSuccess) 
        {
            s_failures++;
            Console.WriteLine($"FAILED Called TestHelperMethodRequiresConstraint_TEMPORARY<{typeof(T).FullName}, {typeof(U).FullName}>");
        }
        static void TestHelperMethodRequiresConstraintCurious_TEMPORARY<T>(bool expectSuccess)
        {
            s_failures++;
            Console.WriteLine($"FAILED Called TestHelperMethodRequiresConstraintCurious_TEMPORARY<{typeof(T).FullName}>");
        }

        [ConvertUnconstrainedCallsToThrowVerificationException]
        static void TestHelperMethodRequiresConstraint<T, U>(bool expectSuccess) where T : IConstraint<U>
        {
            if (expectSuccess)
                Console.WriteLine($"SUCCESS Called TesHelpertMethodRequiresConstraint<{typeof(T).FullName}, {typeof(U).FullName}>");
            else
            {
                s_failures++;
                Console.WriteLine($"FAILED Called TestHelperMethodRequiresConstraint<{typeof(T).FullName}, {typeof(U).FullName}>");
            }
        }
        [ConvertUnconstrainedCallsToThrowVerificationException]
        static void TestHelperMethodRequiresConstraintCurious<T>(bool expectSuccess) where T : IConstraintCurious<T>
        {
            if (expectSuccess)
                Console.WriteLine($"SUCCESS Called TestHelperMethodRequiresConstraintCurious<{typeof(T).FullName}>");
            else
            {
                s_failures++;
                Console.WriteLine($"FAILED Called TestHelperMethodRequiresConstraintCurious<{typeof(T).FullName}>");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConstraintTest<T, U>(bool expectSuccess)
        {
            try
            {
                TestHelperMethodRequiresConstraint<T, U>(expectSuccess);
            }
            catch (InvalidProgramException)
            {
                if (expectSuccess)
                {
                    s_failures++;
                    Console.WriteLine($"FAILED Threw Exception ConstraintTest<{typeof(T).FullName}, {typeof(U).FullName}>");
                }
                else
                {
                    Console.WriteLine($"SUCCESS Threw Exception ConstraintTest<{typeof(T).FullName}, {typeof(U).FullName}>");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConstraintCuriousTest<T>(bool expectSuccess)
        {
            try
            {
                TestHelperMethodRequiresConstraintCurious<T>(expectSuccess);
            }
            catch (InvalidProgramException)
            {
                if (expectSuccess)
                {
                    s_failures++;
                    Console.WriteLine($"FAILED Threw Exception ConstraintCuriousTest<{typeof(T).FullName}>");
                }
                else
                {
                    Console.WriteLine($"SUCCESS Threw Exception ConstraintCuriousTest<{typeof(T).FullName}>");
                }
            }
        }

        static int Main()
        {
            ConstraintTest<ClassThatImplementsConstraint, object>(expectSuccess: true);
            ConstraintTest<ClassThatImplementsConstraint, int>(expectSuccess: false);
            ConstraintTest<StructThatImplementsConstraint, int>(expectSuccess: true);
            ConstraintTest<StructThatImplementsConstraint, object>(expectSuccess: true);
            ConstraintTest<StructThatImplementsConstraint, short>(expectSuccess: false);

            ConstraintCuriousTest<ClassThatImplementsConstraint>(expectSuccess: false);
            ConstraintCuriousTest<ClassThatImplementsConstraintCurious>(expectSuccess: true);

            ConstraintCuriousTest<StructThatImplementsConstraint>(expectSuccess: false);
            ConstraintCuriousTest<StructThatImplementsConstraintCurious>(expectSuccess: true);

            if (s_failures != 0)
            {
                Console.WriteLine($"{s_failures} Failures");
                return 1;
            }
            return 100;
        }
    }
}
