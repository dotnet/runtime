// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
using TestLibrary;
namespace b46867
{
    using System;

    public class AA
    {
        static void Method2(double param3, long param4, __arglist)
        {
            param3 = (double)param4;
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsVarArgSupported))]
        public static void TestEntryPoint()
        {
            Method2(1.0d, 1, __arglist());
        }
    }
}
