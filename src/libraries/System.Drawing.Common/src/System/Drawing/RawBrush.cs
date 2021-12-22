// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing
{
    internal class RawBrush : Brush
    {
        internal RawBrush(IntPtr handle)
        {
            SetNativeBrush(handle);
        }

        public override object Clone() => FromHandle(Handle);
    }
}
