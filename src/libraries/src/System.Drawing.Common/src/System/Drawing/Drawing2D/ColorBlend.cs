// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public sealed class ColorBlend
    {
        public ColorBlend()
        {
            Colors = new Color[1];
            Positions = new float[1];
        }

        public ColorBlend(int count)
        {
            Colors = new Color[count];
            Positions = new float[count];
        }

        public Color[] Colors { get; set; }

        public float[] Positions { get; set; }
    }
}
