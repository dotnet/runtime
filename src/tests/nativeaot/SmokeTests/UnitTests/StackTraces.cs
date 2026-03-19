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
        TestLineNumbers.Run();

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

    class TestLineNumbers
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void MethodWithoutMetadata()
        {
#line 10 "MyFile1.cs"
            var sf1 = new StackFrame(needFileInfo: true);
            var sf2 = new StackFrame(needFileInfo: true);
#line 1 "MyFile2.cs"
            var sf3 = new StackFrame(needFileInfo: true);
#line default
            if (!sf1.GetFileName().EndsWith("MyFile1.cs", StringComparison.Ordinal) || sf1.GetFileLineNumber() != 10)
                throw new Exception(sf1.ToString());
            if (!sf2.GetFileName().EndsWith("MyFile1.cs", StringComparison.Ordinal) || sf2.GetFileLineNumber() != 11)
                throw new Exception(sf2.ToString());
            if (!sf3.GetFileName().EndsWith("MyFile2.cs", StringComparison.Ordinal) || sf3.GetFileLineNumber() != 1)
                throw new Exception(sf3.ToString());
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void MethodWithMetadata()
        {
#line 10 "MyFile1.cs"
            var sf1 = new StackFrame(needFileInfo: true);
            var sf2 = new StackFrame(needFileInfo: true);
#line 1 "MyFile2.cs"
            var sf3 = new StackFrame(needFileInfo: true);
#line default
            if (!sf1.GetFileName().EndsWith("MyFile1.cs", StringComparison.Ordinal) || sf1.GetFileLineNumber() != 10)
                throw new Exception(sf1.ToString());
            if (!sf2.GetFileName().EndsWith("MyFile1.cs", StringComparison.Ordinal) || sf2.GetFileLineNumber() != 11)
                throw new Exception(sf2.ToString());
            if (!sf3.GetFileName().EndsWith("MyFile2.cs", StringComparison.Ordinal) || sf3.GetFileLineNumber() != 1)
                throw new Exception(sf3.ToString());
        }

        public static void Run()
        {
            typeof(TestLineNumbers).GetMethod(nameof(MethodWithMetadata), BindingFlags.Static | BindingFlags.NonPublic);

            MethodWithoutMetadata();
            if (GetMethodSecretly(typeof(TestLineNumbers), nameof(MethodWithoutMetadata)) != null)
                throw new Exception();

            GetMethodSecretly(typeof(TestLineNumbers), nameof(MethodWithMetadata)).Invoke(null, []);

            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "That's the point")]
            static MethodInfo GetMethodSecretly(Type type, string name) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        }
    }
}
