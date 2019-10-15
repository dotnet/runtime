// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public sealed class Blend
    {
        public Blend()
        {
            Factors = new float[1];
            Positions = new float[1];
        }

        public Blend(int count)
        {
            Factors = new float[count];
            Positions = new float[count];
        }

        public float[] Factors { get; set; }

        public float[] Positions { get; set; }
    }
}
