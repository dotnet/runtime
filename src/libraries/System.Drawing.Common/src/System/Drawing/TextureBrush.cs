// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

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

            SafeBrushHandle brush;
            int status = Gdip.GdipCreateTexture(new HandleRef(image, image.nativeImage),
                                                   (int)wrapMode,
                                                   out brush);
            Gdip.CheckStatus(status);

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

            SafeBrushHandle brush;
            int status = Gdip.GdipCreateTexture2(new HandleRef(image, image.nativeImage),
                                                    unchecked((int)wrapMode),
                                                    dstRect.X,
                                                    dstRect.Y,
                                                    dstRect.Width,
                                                    dstRect.Height,
                                                    out brush);
            Gdip.CheckStatus(status);

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

            SafeBrushHandle brush;
            int status = Gdip.GdipCreateTexture2I(new HandleRef(image, image.nativeImage),
                                                     unchecked((int)wrapMode),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, RectangleF dstRect) : this(image, dstRect, null) { }

        public TextureBrush(Image image, RectangleF dstRect, ImageAttributes? imageAttr)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            SafeBrushHandle brush;
            int status = Gdip.GdipCreateTextureIA(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        public TextureBrush(Image image, Rectangle dstRect) : this(image, dstRect, null) { }

        public TextureBrush(Image image, Rectangle dstRect, ImageAttributes? imageAttr)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            SafeBrushHandle brush;
            int status = Gdip.GdipCreateTextureIAI(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(brush);
        }

        internal TextureBrush(SafeBrushHandle nativeBrush) : base(nativeBrush)
        {
        }

        public override object Clone()
        {
            SafeBrushHandle clonedBrush;
            int status = Gdip.GdipCloneBrush(SafeBrushHandle, out clonedBrush);
            Gdip.CheckStatus(status);

            return new TextureBrush(clonedBrush);
        }

        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                int status = Gdip.GdipGetTextureTransform(SafeBrushHandle, matrix.SafeMatrixHandle);
                Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int status = Gdip.GdipSetTextureTransform(SafeBrushHandle, value.SafeMatrixHandle);
                Gdip.CheckStatus(status);
            }
        }

        public WrapMode WrapMode
        {
            get
            {
                int mode = 0;
                int status = Gdip.GdipGetTextureWrapMode(SafeBrushHandle, out mode);
                Gdip.CheckStatus(status);

                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(WrapMode));
                }

                int status = Gdip.GdipSetTextureWrapMode(SafeBrushHandle, unchecked((int)value));
                Gdip.CheckStatus(status);
            }
        }

        public Image Image
        {
            get
            {
                IntPtr image;
                int status = Gdip.GdipGetTextureImage(SafeBrushHandle, out image);
                Gdip.CheckStatus(status);

                return Image.CreateImageObject(image);
            }
        }

        public void ResetTransform()
        {
            int status = Gdip.GdipResetTextureTransform(SafeBrushHandle);
            Gdip.CheckStatus(status);
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
            if (matrix.SafeMatrixHandle.IsClosed)
            {
                return;
            }

            int status = Gdip.GdipMultiplyTextureTransform(SafeBrushHandle,
                                                              matrix.SafeMatrixHandle,
                                                              order);
            Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = Gdip.GdipTranslateTextureTransform(SafeBrushHandle,
                                                               dx,
                                                               dy,
                                                               order);
            Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = Gdip.GdipScaleTextureTransform(SafeBrushHandle,
                                                           sx,
                                                           sy,
                                                           order);
            Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = Gdip.GdipRotateTextureTransform(SafeBrushHandle,
                                                            angle,
                                                            order);
            Gdip.CheckStatus(status);
        }
    }
}
