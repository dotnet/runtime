// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable 169

namespace StaticFieldLayout
{
    struct NoPointers
    {
        static int int1;
        static byte byte1;
        static char char1;
    }

    struct StillNoPointers
    {
        NoPointers noPointers1;
        static bool bool1;
    }

    class ClassNoPointers
    {
        static int int1;
        static byte byte1;
        static char char1;
    }

    struct HasPointers
    {
        bool bool1;
        static string string1;
        static ClassNoPointers class1;
        char char1;
    }

    class MixPointersAndNonPointers
    {
        static string string1;
        static int int1;
        static ClassNoPointers class1;
        static int int2;
        static string string2;
    }

    class EnsureInheritanceResetsStaticOffsets : MixPointersAndNonPointers
    {
        static int int3;
        static string string3;
    }

    class LiteralFieldsDontAffectLayout
    {
        const int IntConstant = 0;
        const string StringConstant = null;
        static int Int1;
        static string String1;
    }

    class RvaTestClass
    {
        static void RvaTest()
        {
            int[] foo = new int[] { 0, 1, 2, 3, 4, 45, 5, 5 };
        }

    }

    struct StaticSelfRef
    {
        static StaticSelfRef selfRef1;
    }
}
