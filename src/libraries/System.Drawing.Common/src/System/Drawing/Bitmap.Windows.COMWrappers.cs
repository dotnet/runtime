// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Bitmap
    {
        private static unsafe IntPtr CreateGdipBitmapFromStream(GPStream stream, bool useIcm)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr bitmap = IntPtr.Zero;
                int status;
                if (useIcm)
                {
                    status = Gdip.GdipCreateBitmapFromStreamICM(streamPtr, &bitmap);
                }
                else
                {
                    status = Gdip.GdipCreateBitmapFromStream(streamPtr, &bitmap);
                }
                Gdip.CheckStatus(status);

                return bitmap;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }
    }
}
