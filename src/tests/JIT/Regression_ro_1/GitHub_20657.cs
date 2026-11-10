// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

// GitHub 20657: 

namespace GitHub_20657
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var v1 = new Vector4();
            var v2 = Oops(v1);
            if ((v2.X != 0.0) || (v2.Y != 0.0) || (v2.Z != 0.0) || (v2.W != 1.0))
            {
                return -1;
            }
            return 100;
        }

        private static readonly Vector4 Noo = new Vector4();

        public static Vector4 Oops(Vector4 v)
        {
            v.W = 1f;

            return v + Noo;
        }
    }
}
