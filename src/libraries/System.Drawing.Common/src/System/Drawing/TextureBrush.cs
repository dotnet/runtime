// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;

    /**
     * Represent a Texture brush object
     */
    /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush"]/*' />
    /// <devdoc>
    ///    Encapsulates a <see cref='System.Drawing.Brush'/> that uses an fills the
    ///    interior of a shape with an image.
    /// </devdoc>
    public sealed class TextureBrush : Brush
    {
        /**
         * Create a new texture brush object
         *
         * @notes Should the rectangle parameter be Rectangle or RectF?
         *  We'll use Rectangle to specify pixel unit source image
         *  rectangle for now. Eventually, we'll need a mechanism
         *  to specify areas of an image in a resolution-independent way.
         *
         * @notes We'll make a copy of the bitmap object passed in.
         */

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/>
        ///    class with the specified image.
        /// </devdoc>
        public TextureBrush(Image bitmap)
            : this(bitmap, System.Drawing.Drawing2D.WrapMode.Tile)
        {
        }

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/>
        ///       class with the specified image and wrap mode.
        ///    </para>
        /// </devdoc>
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

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        // float version
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/>
        ///       class with the specified image, wrap mode, and bounding rectangle.
        ///    </para>
        /// </devdoc>
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

        // int version
        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/>
        ///       class with the specified image, wrap mode, and bounding rectangle.
        ///    </para>
        /// </devdoc>
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


        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image
        ///       and bounding rectangle.
        ///    </para>
        /// </devdoc>
        public TextureBrush(Image image, RectangleF dstRect)
        : this(image, dstRect, (ImageAttributes)null)
        { }

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified
        ///       image, bounding rectangle, and image attributes.
        ///    </para>
        /// </devdoc>
        public TextureBrush(Image image, RectangleF dstRect,
                            ImageAttributes imageAttr)
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

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified image
        ///       and bounding rectangle.
        ///    </para>
        /// </devdoc>
        public TextureBrush(Image image, Rectangle dstRect)
        : this(image, dstRect, (ImageAttributes)null)
        { }

        // When creating a texture brush from a metafile image, the dstRect
        // is used to specify the size that the metafile image should be
        // rendered at in the device units of the destination graphics.
        // It is NOT used to crop the metafile image, so only the width 
        // and height values matter for metafiles.
        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TextureBrush7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.TextureBrush'/> class with the specified
        ///       image, bounding rectangle, and image attributes.
        ///    </para>
        /// </devdoc>
        public TextureBrush(Image image, Rectangle dstRect,
                            ImageAttributes imageAttr)
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

        /// <devdoc>
        ///     Constructor to initialized this object to be owned by GDI+.
        /// </devdoc>
        internal TextureBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.TextureBrush'/>.
        /// </devdoc>
        public override Object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new TextureBrush(cloneBrush);
        }


        /**
         * Set/get brush transform
         */
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.Transform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.Matrix'/> that defines a local geometrical
        ///       transform for this <see cref='System.Drawing.TextureBrush'/>.
        ///    </para>
        /// </devdoc>
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

        /**
         * Set/get brush wrapping mode
         */
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.WrapMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.WrapMode'/> that indicates the wrap mode for this
        ///    <see cref='System.Drawing.TextureBrush'/>. 
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.Image"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the <see cref='System.Drawing.Image'/> associated with this <see cref='System.Drawing.TextureBrush'/>.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.ResetTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Resets the <see cref='System.Drawing.Drawing2D.LinearGradientBrush.Transform'/> property to
        ///       identity.
        ///    </para>
        /// </devdoc>
        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetTextureTransform(new HandleRef(this, NativeBrush));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.MultiplyTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        ///       transform of this <see cref='System.Drawing.TextureBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the specified <see cref='System.Drawing.Drawing2D.Matrix'/>.
        ///    </para>
        /// </devdoc>
        public void MultiplyTransform(Matrix matrix)
        { MultiplyTransform(matrix, MatrixOrder.Prepend); }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.MultiplyTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        ///       transform of this <see cref='System.Drawing.TextureBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> in the specified order.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TranslateTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Translates the local geometrical transform by the specified dimmensions. This
        ///       method prepends the translation to the transform.
        ///    </para>
        /// </devdoc>
        public void TranslateTransform(float dx, float dy)
        { TranslateTransform(dx, dy, MatrixOrder.Prepend); }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.TranslateTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Translates the local geometrical transform by the specified dimmensions in
        ///       the specified order.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.ScaleTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Scales the local geometric transform by the specified amounts. This method
        ///       prepends the scaling matrix to the transform.
        ///    </para>
        /// </devdoc>
        public void ScaleTransform(float sx, float sy)
        { ScaleTransform(sx, sy, MatrixOrder.Prepend); }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.ScaleTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Scales the local geometric transform by the specified amounts in the
        ///       specified order.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.RotateTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates the local geometric transform by the specified amount. This method
        ///       prepends the rotation to the transform.
        ///    </para>
        /// </devdoc>
        public void RotateTransform(float angle)
        { RotateTransform(angle, MatrixOrder.Prepend); }

        /// <include file='doc\TextureBrush.uex' path='docs/doc[@for="TextureBrush.RotateTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates the local geometric transform by the specified amount in the
        ///       specified order.
        ///    </para>
        /// </devdoc>
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

