// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

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
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            ValidateBitmap(bitmap);

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
                status = SafeNativeMethods.Gdip.GdipCreateBitmapFromStreamICM(new GPStream(stream), out bitmap);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipCreateBitmapFromStream(new GPStream(stream), out bitmap);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);

            ValidateBitmap(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, null, stream);
        }

        private void ValidateBitmap(IntPtr bitmap)
        {
            int status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, bitmap));
            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, bitmap));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }
    }
}
