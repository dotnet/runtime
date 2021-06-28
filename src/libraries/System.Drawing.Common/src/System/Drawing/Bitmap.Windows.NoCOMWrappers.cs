// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Bitmap
    {
        private static unsafe IntPtr CreateGdipBitmapFromStream(GPStream stream, bool useIcm)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status;
            if (useIcm)
            {
                status = Gdip.GdipCreateBitmapFromStreamICM(stream, out bitmap);
            }
            else
            {
                status = Gdip.GdipCreateBitmapFromStream(stream, out bitmap);
            }
            Gdip.CheckStatus(status);

            return bitmap;
        }
    }
}
