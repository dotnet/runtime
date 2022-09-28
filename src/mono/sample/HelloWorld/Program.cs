// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace HelloWorld
{
    struct S1 {
        internal double d1, d2;
    }

    ref struct R1 {
        internal ref double d1; // 8
        internal ref object o1; // += sizeof(MonoObject*)
        internal object o2;  // skip
        internal ref S1 s1; //+= instance_size(S1)
        internal S1 s2; // skip
        public R1(ref double d1, ref object o1, ref S1 s1) {
            this.d1 = ref d1;
            this.o1 = ref o1;
            this.s1 = ref s1;
        }
        internal void Run()
        {
            Console.WriteLine("oi");
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Run();
        }
        static void Run()
        {
            Invoke(new string[] { "TEST" });
        }
        static void Invoke(object[] parameters)
        {
            CheckArguments(parameters, "abcd"u8);
        }
        static void CheckArguments(ReadOnlySpan<object> parameters, ReadOnlySpan<byte> parameters2)
        {
            double d1thays = 123.0;
            object o1thays = new String("thays");
            var s1thays = new S1();
            s1thays.d1 = 10;
            s1thays.d2 = 20;
            R1 myR1 = new R1(ref d1thays, ref o1thays, ref s1thays);
            myR1.o2 = new String("thays2");
            myR1.s2 = new S1();
            myR1.s2.d1 = 30;
            myR1.s2.d2 = 40;
            System.Diagnostics.Debugger.Break();
        }
    }
}
