// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Internal;
using System.IO;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Bitmap
    {
        public Bitmap(Type type, string resource)
        {
            Stream stream = type.Module.Assembly.GetManifestResourceStream(type, resource);
            if (stream == null)
            {
                throw new ArgumentException(SR.Format(SR.ResourceNotFound, type, resource));
            }

            IntPtr bitmap = IntPtr.Zero;
            int status = Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out bitmap);
            Gdip.CheckStatus(status);

            ValidateImage(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, null, stream);
        }

        public Bitmap(Stream stream, bool useIcm)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            IntPtr bitmap = IntPtr.Zero;
            int status;

            if (useIcm)
            {
                status = Gdip.GdipCreateBitmapFromStreamICM(new GPStream(stream), out bitmap);
            }
            else
            {
                status = Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out bitmap);
            }
            Gdip.CheckStatus(status);

            ValidateImage(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, null, stream);
        }
    }
}
