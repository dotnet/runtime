// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.IO;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Bitmap
    {
        public Bitmap(Stream stream, bool useIcm)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            IntPtr bitmap = CreateGdipBitmapFromStream(new GPStream(stream), useIcm);

            ValidateImage(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, null, stream);
        }
    }
}
