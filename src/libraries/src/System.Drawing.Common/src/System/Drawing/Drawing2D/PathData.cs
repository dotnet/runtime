// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public sealed class PathData
    {
        public PathData() { }

        public PointF[] Points { get; set; }

        public byte[] Types { get; set; }
    }
}
