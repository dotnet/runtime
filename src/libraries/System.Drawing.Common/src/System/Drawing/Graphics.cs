// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates a GDI+ drawing surface.
    /// </summary>
    public sealed partial class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        /// <summary>
        /// Gets or sets the world transform for this <see cref='Graphics'/>.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                int status = SafeNativeMethods.Gdip.GdipGetWorldTransform(new HandleRef(this, NativeGraphics),
                                                           new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetWorldTransform(new HandleRef(this, NativeGraphics),
                                                           new HandleRef(value, value.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        /// <summary>
        /// Resets the world transform to identity.
        /// </summary>
        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetWorldTransform(new HandleRef(this, NativeGraphics));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Multiplies the <see cref='Matrix'/> that represents the world transform and <paramref name="matrix"/>.
        /// </summary>
        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        /// <summary>
        /// Multiplies the <see cref='Matrix'/> that represents the world transform and <paramref name="matrix"/>.
        /// </summary>
        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyWorldTransform(new HandleRef(this, NativeGraphics),
                                                            new HandleRef(matrix, matrix.nativeMatrix),
                                                            order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateWorldTransform(new HandleRef(this, NativeGraphics), dx, dy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleWorldTransform(new HandleRef(this, NativeGraphics), sx, sy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateWorldTransform(new HandleRef(this, NativeGraphics), angle, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}
