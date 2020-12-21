// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Drawing2D
{
    public sealed class RegionData
    {
        internal RegionData(byte[] data) => Data = data;

        public byte[] Data { get; set; }
    }
}
