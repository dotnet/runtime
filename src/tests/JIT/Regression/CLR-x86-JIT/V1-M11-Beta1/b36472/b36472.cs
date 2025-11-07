// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
using TestLibrary;
namespace b36472
{
    using System;

    public class BB
    {
        static object Method1(__arglist)
        {
            return (int)0;
        }
        object[] Method2(ref object[] param1, ref int[] param2, BB param3,
                                BB param4, BB param5, ref float[] param6)
        {
            return null;
        }

	    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsVarArgSupported))]
        public static int TestEntryPoint()
        {
            return (int)BB.Method1(__arglist()) + 100;
        }
    }
}
