// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    internal static class RectangleFEx
    {
        public static GPRECTF ToGPRECTF(this RectangleF rect)
        {
            return new GPRECTF(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}
