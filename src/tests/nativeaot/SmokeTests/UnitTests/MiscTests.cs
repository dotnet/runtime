// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class MiscTests
{
    internal static int Run()
    {
        TestSurrogateStringLiterals.Run();
        return 100;
    }

    class TestSurrogateStringLiterals
    {
        public static void Run()
        {
            CheckSurrogateLiteral(GetFirstSurrogateLiteral(), '\uD800');
            CheckSurrogateLiteral(GetSecondSurrogateLiteral(), '\uD801');
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetFirstSurrogateLiteral() => "\uD800";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetSecondSurrogateLiteral() => "\uD801";

        private static void CheckSurrogateLiteral(string value, char expected)
        {
            if (value.Length != 1)
                throw new Exception(value.Length.ToString());

            if (value[0] != expected)
                throw new Exception(((int)value[0]).ToString("X4"));
        }
    }
}
