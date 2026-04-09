// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b45259
{
    using System;

    public class AA
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            try
            {
                // do what you like here
            }
            catch (Exception)
            {
                float[] af = new float[7];
                af[0] = af[1];
            }
        }
    }

}
