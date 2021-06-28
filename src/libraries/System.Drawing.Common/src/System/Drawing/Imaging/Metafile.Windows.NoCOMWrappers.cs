// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    public sealed partial class Metafile : Image
    {
        private static IntPtr CreateGdipMetafileFromStream(GPStream stream)
        {
            Gdip.CheckStatus(Gdip.GdipCreateMetafileFromStream(stream, out IntPtr metafile));
            return metafile;
        }

        private static IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, EmfType type, string? description)
        {
            Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                stream,
                referenceHdc,
                type,
                IntPtr.Zero,
                MetafileFrameUnit.GdiCompatible,
                description,
                out IntPtr metafile));

            return metafile;
        }

        private static IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                stream,
                referenceHdc,
                type,
                ref frameRect,
                frameUnit,
                description,
                out IntPtr metafile));

            return metafile;
        }

        private static IntPtr CreateGdipMetafileFromStream(GPStream stream, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            IntPtr metafile = IntPtr.Zero;
            if (frameRect.IsEmpty)
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                    stream,
                    referenceHdc,
                    type,
                    IntPtr.Zero,
                    frameUnit,
                    description,
                    out metafile));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileStreamI(
                    stream,
                    referenceHdc,
                    type,
                    ref frameRect,
                    frameUnit,
                    description,
                    out metafile));
            }
            return metafile;
        }

        private static void GetGdipMetafileHeaderFromStream(GPStream stream, IntPtr memory)
        {
            Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromStream(stream, memory));
        }
    }
}
