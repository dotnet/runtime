// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for GenTree::Compare missing gtFieldSeq check (PR #125357).
//
// Without the field sequence check, GenTree::Compare considers two GT_CNS_INT
// nodes with the same offset value but different field sequences to be identical.
// This can cause tail merge to incorrectly merge stores to different fields
// that happen to be at the same offset, propagating the wrong field sequence.
// After inlining, redundant branch opts (which uses the liberal VN) can fold
// the type check using the wrong field-based alias information, producing
// incorrect results.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_125357
{
    public class ClassOne
    {
        public int Field;
    }

    public class ClassTwo
    {
        public int Field;
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static object GetClassTwo() => new ClassTwo();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int TestFieldSeqMerge(object obj)
        {
            if (obj.GetType() == typeof(ClassOne))
            {
                Unsafe.As<ClassOne>(obj).Field = 42;
            }
            else
            {
                Unsafe.As<ClassTwo>(obj).Field = 42;
            }

            Unsafe.As<ClassTwo>(obj).Field = 999;
            return Unsafe.As<ClassOne>(obj).Field;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = TestFieldSeqMerge(GetClassTwo());
            if (result != 999)
            {
                return -1;
            }

            return 100;
        }
    }
}
