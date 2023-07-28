// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Bug
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        internal virtual void runTest()
        {
            Random rand = new Random(Seed);
            Object o = ((UInt64)rand.Next((int)UInt64.MinValue, Int32.MaxValue));
        }

        [Fact]
        public static int TestEntryPoint()
        {
            new Bug().runTest();
            return 100;
        }
    }
}
