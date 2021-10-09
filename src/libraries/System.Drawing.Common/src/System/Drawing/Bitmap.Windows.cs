// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.IO;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Bitmap
    {
        public unsafe Bitmap(Stream stream, bool useIcm)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));

            IntPtr bitmap = IntPtr.Zero;
            if (useIcm)
            {
                Gdip.CheckStatus(Gdip.GdipCreateBitmapFromStreamICM(streamWrapper.Ptr, &bitmap));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipCreateBitmapFromStream(streamWrapper.Ptr, &bitmap));
            }

            ValidateImage(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, null, stream);
        }
    }
}
