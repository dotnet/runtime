// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class MiscTests
{
    internal static int Run()
    {
        TestSurrogateStringLiterals.Run();
        TestNonAsciiIdentifiers.Run();
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

    // Regression test: identifiers made only of non-ASCII characters used to be sanitized into runs of
    // underscores. A type with an 8-character name and a 9-character method then produced the same symbol
    // ("<type>__<method>") as a type with a 9-character name and an 8-character method, and the linker
    // (or the object writer) rejected the duplicate symbols. The exception handling clauses below make sure
    // per-method symbols such as the EH info are emitted for both methods.
    class TestNonAsciiIdentifiers
    {
        public static void Run()
        {
            if (new ラベルを登録する().実行して検分する() != "a")
                throw new Exception();
            if (new 配置対象を選び出す().実行して検分す() != "c")
                throw new Exception();
        }

        sealed class ラベルを登録する
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string 実行して検分する()
            {
                try { return "a"; }
                catch (Exception) { return "b"; }
            }
        }

        sealed class 配置対象を選び出す
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string 実行して検分す()
            {
                try { return "c"; }
                catch (Exception) { return "d"; }
            }
        }
    }
}
