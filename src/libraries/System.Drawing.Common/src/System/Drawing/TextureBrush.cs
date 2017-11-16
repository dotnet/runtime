// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace System.Drawing
{
    public sealed class TextureBrush : Brush
    {
        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.

        public TextureBrush(Image bitmap) : this(bitmap, WrapMode.Tile)
        {
        }

        public TextureBrush(Image image, WrapMode wrapMode)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
            {
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateTexture(new HandleRef(image, image.nativeImage),
                                                   (int)wrapMode,
                                                   out brush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, WrapMode wrapMode, RectangleF dstRect)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            
            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
            {
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateTexture2(new HandleRef(image, image.nativeImage),
                                                    unchecked((int)wrapMode),
                                                    dstRect.X,
                                                    dstRect.Y,
                                                    dstRect.Width,
                                                    dstRect.Height,
                                                    out brush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, WrapMode wrapMode, Rectangle dstRect)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
            {
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateTexture2I(new HandleRef(image, image.nativeImage),
                                                     unchecked((int)wrapMode),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, RectangleF dstRect) : this(image, dstRect, null) { }

        public TextureBrush(Image image, RectangleF dstRect, ImageAttributes imageAttr)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateTextureIA(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, Rectangle dstRect) : this(image, dstRect, null) { }

        public TextureBrush(Image image, Rectangle dstRect, ImageAttributes imageAttr)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateTextureIAI(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        internal TextureBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new TextureBrush(cloneBrush);
        }

        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                int status = SafeNativeMethods.Gdip.GdipGetTextureTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int status = SafeNativeMethods.Gdip.GdipSetTextureTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public WrapMode WrapMode
        {
            get
            {
                int mode = 0;
                int status = SafeNativeMethods.Gdip.GdipGetTextureWrapMode(new HandleRef(this, NativeBrush), out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(WrapMode));
                }
    
                int status = SafeNativeMethods.Gdip.GdipSetTextureWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Image Image
        {
            get
            {
                IntPtr image;
                int status = SafeNativeMethods.Gdip.GdipGetTextureImage(new HandleRef(this, NativeBrush), out image);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return Image.CreateImageObject(image);
            }
        }

        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetTextureTransform(new HandleRef(this, NativeBrush));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatability with GDI+.
            if (matrix.nativeMatrix == IntPtr.Zero)
            {
                return;
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyTextureTransform(new HandleRef(this, NativeBrush),
                                                              new HandleRef(matrix, matrix.nativeMatrix),
                                                              order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateTextureTransform(new HandleRef(this, NativeBrush),
                                                               dx,
                                                               dy,
                                                               order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleTextureTransform(new HandleRef(this, NativeBrush),
                                                           sx,
                                                           sy,
                                                           order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateTextureTransform(new HandleRef(this, NativeBrush),
                                                            angle,
                                                            order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}
