// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public abstract partial class Image
    {
        private static unsafe IntPtr LoadGdipImageFromStream(GPStream stream, bool useEmbeddedColorManagement)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr image = IntPtr.Zero;
                if (useEmbeddedColorManagement)
                {
                    Gdip.CheckStatus(Gdip.GdipLoadImageFromStreamICM(streamPtr, &image));
                }
                else
                {
                    Gdip.CheckStatus(Gdip.GdipLoadImageFromStream(streamPtr, &image));
                }
                return image;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }

        private unsafe int SaveGdipImageToStream(GPStream stream, Guid g, EncoderParameters? encoderParams, IntPtr encoderParamsMemory)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                return Gdip.GdipSaveImageToStream(
                    new HandleRef(this, nativeImage),
                    streamPtr,
                    &g,
                    new HandleRef(encoderParams, encoderParamsMemory));
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }
    }
}
