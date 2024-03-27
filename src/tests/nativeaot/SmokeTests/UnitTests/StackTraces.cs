// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

class StackTraces
{
    internal static int Run()
    {
        TestStackTraceHidden.Run();

        return 100;
    }

    class TestStackTraceHidden
    {
        [DynamicDependency(nameof(HiddenMethodWithMetadata))]
        internal static void Run()
        {
            string s = null;
            StackTrace t = null;
            try
            {
                HiddenMethod(ref t);
            }
            catch (Exception ex)
            {
                s = ex.StackTrace;
            }

            Verify(s, false);
            Verify(t.ToString(), false);

            StringBuilder sb = new StringBuilder();
            foreach (StackFrame f in t.GetFrames())
                sb.AppendLine(f.ToString());

            Verify(sb.ToString(), true);

            if (GetSecretMethod(typeof(TestStackTraceHidden), nameof(HiddenMethodWithMetadata)) == null)
                throw new Exception();

            if (GetSecretMethod(typeof(TestStackTraceHidden), nameof(HiddenMethod)) != null)
                throw new Exception();

            static MethodInfo GetSecretMethod(Type t, string name) => t.GetMethod(name);

            static void Verify(string s, bool expected)
            {
                if (s.Contains(nameof(HiddenMethod)) != expected
                    || s.Contains(nameof(HiddenMethodWithMetadata)) != expected)
                    throw new Exception(s);
                if (!s.Contains(nameof(Collector)))
                    throw new Exception(s);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void HiddenMethod(ref StackTrace t) => HiddenMethodWithMetadata(ref t);

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void HiddenMethodWithMetadata(ref StackTrace t) => Collector(ref t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Collector(ref StackTrace t)
        {
            t = new StackTrace();
            throw new Exception();
        }
    }

}
