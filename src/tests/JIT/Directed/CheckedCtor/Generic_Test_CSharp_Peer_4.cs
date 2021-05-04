// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This test represents a case where csc.exe puts a base/peer ctor callsite outside of the
// first block of the derived ctor.
//
// Specifically covers: "Instance field initializers preceding the generic peer ctor callsite"
//

using System;
using System.Runtime.CompilerServices;

namespace Test
{
    static class App
    {
        static int Main()
        {
            new DerivedClass<int>(7);
            return 100;
        }
    }

    public class BaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseClass(int arg) { Console.Write("BaseClass::.ctor -- `{0}'\r\n", arg); return; }
    }

    public class DerivedClass<T> : BaseClass
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        private static readonly Random Generator = new Random(Seed);
        private static string GetString() { return "Text"; }
        public int Field1 = ((Generator.Next(5, 8) == 10) ? 10 : 20);
        public string Field2 = (GetString() ?? "NeededToFallBack");
        public Func<int> Field3 = () => Generator.Next(5, 8);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedClass(int selector) : this(selector, 0) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DerivedClass(int arg, int marker) : base(arg) { }
    }
}

