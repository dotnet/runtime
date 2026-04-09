// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b22290
{
    public class DD
    {
        public float[] Method1()
        {
            return new float[7];
        }
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            new DD().Method1();
        }
    }
}
