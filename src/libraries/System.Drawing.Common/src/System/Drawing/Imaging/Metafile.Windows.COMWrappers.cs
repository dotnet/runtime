// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    public sealed partial class Metafile : Image
    {
        private static unsafe IntPtr CreateGdipMetafileFromStream(GPStream stream)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr metafile = IntPtr.Zero;
                Gdip.CheckStatus(Gdip.GdipCreateMetafileFromStream(streamPtr, &metafile));
                return metafile;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }

        private static unsafe IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, EmfType type, string? description)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr metafile = IntPtr.Zero;

                Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                    streamPtr,
                    referenceHdc,
                    type,
                    IntPtr.Zero,
                    MetafileFrameUnit.GdiCompatible,
                    description,
                    &metafile));

                return metafile;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }

        private static unsafe IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr metafile = IntPtr.Zero;

                Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                    streamPtr,
                    referenceHdc,
                    type,
                    &frameRect,
                    frameUnit,
                    description,
                    &metafile));

                return metafile;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }

        private static unsafe IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                IntPtr metafile = IntPtr.Zero;
                if (frameRect.IsEmpty)
                {
                    Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                        streamPtr,
                        referenceHdc,
                        type,
                        IntPtr.Zero,
                        frameUnit,
                        description,
                        &metafile));
                }
                else
                {
                    Gdip.CheckStatus(Gdip.GdipRecordMetafileStreamI(
                        streamPtr,
                        referenceHdc,
                        type,
                        &frameRect,
                        frameUnit,
                        description,
                        &metafile));
                }
                return metafile;
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }

        private static void GetGdipMetafileHeaderFromStream(GPStream stream, IntPtr memory)
        {
            DrawingComWrappers.Instance.GetIStreamInterfaces(stream, out IntPtr streamWrapperPtr, out IntPtr streamPtr);
            try
            {
                Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromStream(streamPtr, memory));
            }
            finally
            {
                Marshal.Release(streamPtr);
                Marshal.Release(streamWrapperPtr);
            }
        }
    }
}
