// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoDelta;

namespace Sample
{
    public class Test
    {
	static DeltaHelper replacer = DeltaHelper.Make ();

        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestMeaning()
        {
            return 128;
        }

	public static void Update()
	{
	    Assembly assm = typeof (Test).Assembly;
	    replacer.Update (assm);
	}
    }
}
