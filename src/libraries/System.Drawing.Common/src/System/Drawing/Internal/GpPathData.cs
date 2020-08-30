// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GpPathData
    {
        public int Count;
        public PointF* Points;
        public byte* Types;
    }
}
