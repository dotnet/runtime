// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    [Flags]
    public enum ImageCodecFlags
    {
        Encoder = 0x00000001,
        Decoder = 0x00000002,
        SupportBitmap = 0x00000004,
        SupportVector = 0x00000008,
        SeekableEncode = 0x00000010,
        BlockingDecode = 0x00000020,
        Builtin = 0x00010000,
        System = 0x00020000,
        User = 0x00040000
    }
}
