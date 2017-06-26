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
    /// <summary>
    /// Encapsulates a <see cref='System.Drawing.Brush'/> that fills the interior of a shape with an image.
    /// </summary>
    public sealed class TextureBrush : Brush
    {
        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image.
        /// </summary>
        public TextureBrush(Image bitmap)
            : this(bitmap, System.Drawing.Drawing2D.WrapMode.Tile)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image and wrap mode.
        /// </summary>
        public TextureBrush(Image image, WrapMode wrapMode)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            //validate the WrapMode enum
            //valid values are 0x0 to 0x4
            if (!ClientUtils.IsEnumValid(wrapMode, unchecked((int)wrapMode), (int)WrapMode.Tile, (int)WrapMode.Clamp))
            {
                throw new InvalidEnumArgumentException("wrapMode", unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateTexture(new HandleRef(image, image.nativeImage),
                                                   (int)wrapMode,
                                                   out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(brush);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image,
        /// wrap mode, and bounding rectangle.
        /// </summary>
        public TextureBrush(Image image, WrapMode wrapMode, RectangleF dstRect)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            //validate the WrapMode enum
            //valid values are 0x0 to 0x4
            if (!ClientUtils.IsEnumValid(wrapMode, unchecked((int)wrapMode), (int)WrapMode.Tile, (int)WrapMode.Clamp))
            {
                throw new InvalidEnumArgumentException("wrapMode", unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateTexture2(new HandleRef(image, image.nativeImage),
                                                    unchecked((int)wrapMode),
                                                    dstRect.X,
                                                    dstRect.Y,
                                                    dstRect.Width,
                                                    dstRect.Height,
                                                    out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(brush);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image,
        /// wrap mode, and bounding rectangle.
        /// </summary>
        public TextureBrush(Image image, WrapMode wrapMode, Rectangle dstRect)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            //validate the WrapMode enum
            //valid values are 0x0 to 0x4
            if (!ClientUtils.IsEnumValid(wrapMode, unchecked((int)wrapMode), (int)WrapMode.Tile, (int)WrapMode.Clamp))
            {
                throw new InvalidEnumArgumentException("wrapMode", unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateTexture2I(new HandleRef(image, image.nativeImage),
                                                     unchecked((int)wrapMode),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            SetNativeBrushInternal(brush);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image
        /// and bounding rectangle.
        /// </summary>
        public TextureBrush(Image image, RectangleF dstRect)
            : this(image, dstRect, (ImageAttributes)null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image,
        /// bounding rectangle, and image attributes.
        /// </summary>
        public TextureBrush(Image image, RectangleF dstRect, ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateTextureIA(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            SetNativeBrushInternal(brush);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image
        /// and bounding rectangle.
        /// </summary>
        public TextureBrush(Image image, Rectangle dstRect)
            : this(image, dstRect, (ImageAttributes)null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified
        /// image, bounding rectangle, and image attributes.
        /// </summary>
        public TextureBrush(Image image, Rectangle dstRect, ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateTextureIAI(new HandleRef(image, image.nativeImage),
                                                     new HandleRef(imageAttr, (imageAttr == null) ?
                                                       IntPtr.Zero : imageAttr.nativeImageAttributes),
                                                     dstRect.X,
                                                     dstRect.Y,
                                                     dstRect.Width,
                                                     dstRect.Height,
                                                     out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            SetNativeBrushInternal(brush);
        }

        /// <summary>
        /// Constructor to initialize this object to be owned by GDI+.
        /// </summary>
        internal TextureBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        /// <summary>
        /// Creates an exact copy of this <see cref='System.Drawing.TextureBrush'/>.
        /// </summary>
        public override Object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new TextureBrush(cloneBrush);
        }


        private void _SetTransform(Matrix matrix)
        {
            int status = SafeNativeMethods.Gdip.GdipSetTextureTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private Matrix _GetTransform()
        {
            Matrix matrix = new Matrix();

            int status = SafeNativeMethods.Gdip.GdipGetTextureTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            return matrix;
        }

        /// <summary>
        /// Gets or sets a <see cref='System.Drawing.Drawing2D.Matrix'/> that defines a local geometrical
        /// transform for this <see cref='System.Drawing.TextureBrush'/>.
        /// </summary>
        public Matrix Transform
        {
            get { return _GetTransform(); }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _SetTransform(value);
            }
        }

        private void _SetWrapMode(WrapMode wrapMode)
        {
            int status = SafeNativeMethods.Gdip.GdipSetTextureWrapMode(new HandleRef(this, NativeBrush), unchecked((int)wrapMode));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        private WrapMode _GetWrapMode()
        {
            int mode = 0;

            int status = SafeNativeMethods.Gdip.GdipGetTextureWrapMode(new HandleRef(this, NativeBrush), out mode);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            return (WrapMode)mode;
        }

        /// <summary>
        /// Gets or sets a <see cref='System.Drawing.Drawing2D.WrapMode'/> that indicates the wrap mode for this
        /// <see cref='System.Drawing.TextureBrush'/>. 
        /// </summary>
        public WrapMode WrapMode
        {
            get
            {
                return _GetWrapMode();
            }
            set
            {
                //validate the WrapMode enum
                //valid values are 0x0 to 0x4
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)WrapMode.Tile, (int)WrapMode.Clamp))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(WrapMode));
                }

                _SetWrapMode(value);
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Drawing.Image'/> associated with this <see cref='System.Drawing.TextureBrush'/>.
        /// </summary>
        public Image Image
        {
            get
            {
                IntPtr image;

                int status = SafeNativeMethods.Gdip.GdipGetTextureImage(new HandleRef(this, NativeBrush), out image);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                return Image.CreateImageObject(image);
            }
        }

        /// <summary>
        /// Resets the <see cref='System.Drawing.Drawing2D.LinearGradientBrush.Transform'/> property to identity.
        /// </summary>
        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetTextureTransform(new HandleRef(this, NativeBrush));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        /// transform of this <see cref='System.Drawing.TextureBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/>
        /// by prepending the specified <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </summary>
        public void MultiplyTransform(Matrix matrix)
        { MultiplyTransform(matrix, MatrixOrder.Prepend); }

        /// <summary>
        /// Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        /// transform of this <see cref='System.Drawing.TextureBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/>
        /// in the specified order.
        /// </summary>
        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyTextureTransform(new HandleRef(this, NativeBrush),
                                                              new HandleRef(matrix, matrix.nativeMatrix),
                                                              order);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Translates the local geometrical transform by the specified dimmensions. This
        /// method prepends the translation to the transform.
        /// </summary>
        public void TranslateTransform(float dx, float dy)
        { TranslateTransform(dx, dy, MatrixOrder.Prepend); }

        /// <summary>
        /// Translates the local geometrical transform by the specified dimmensions in the specified order.
        /// </summary>
        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateTextureTransform(new HandleRef(this, NativeBrush),
                                                               dx,
                                                               dy,
                                                               order);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Scales the local geometric transform by the specified amounts. This method
        /// prepends the scaling matrix to the transform.
        /// </summary>
        public void ScaleTransform(float sx, float sy)
        { ScaleTransform(sx, sy, MatrixOrder.Prepend); }

        /// <summary>
        /// Scales the local geometric transform by the specified amounts in the specified order.
        /// </summary>
        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleTextureTransform(new HandleRef(this, NativeBrush),
                                                           sx,
                                                           sy,
                                                           order);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Rotates the local geometric transform by the specified amount. This method prepends the rotation to the transform.
        /// </summary>
        public void RotateTransform(float angle)
        { RotateTransform(angle, MatrixOrder.Prepend); }

        /// <summary>
        /// Rotates the local geometric transform by the specified amount in the specified order.
        /// </summary>
        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateTextureTransform(new HandleRef(this, NativeBrush),
                                                            angle,
                                                            order);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }
    }
}

