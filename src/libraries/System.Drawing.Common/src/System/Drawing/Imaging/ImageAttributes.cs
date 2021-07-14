// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    // sdkinc\GDIplusImageAttributes.h

    // There are 5 possible sets of color adjustments:
    //          ColorAdjustDefault,
    //          ColorAdjustBitmap,
    //          ColorAdjustBrush,
    //          ColorAdjustPen,
    //          ColorAdjustText,

    // Bitmaps, Brushes, Pens, and Text will all use any color adjustments
    // that have been set into the default ImageAttributes until their own
    // color adjustments have been set.  So as soon as any "Set" method is
    // called for Bitmaps, Brushes, Pens, or Text, then they start from
    // scratch with only the color adjustments that have been set for them.
    // Calling Reset removes any individual color adjustments for a type
    // and makes it revert back to using all the default color adjustments
    // (if any).  The SetToIdentity method is a way to force a type to
    // have no color adjustments at all, regardless of what previous adjustments
    // have been set for the defaults or for that type.

    /// <summary>
    /// Contains information about how image colors are manipulated during rendering.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class ImageAttributes : ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        internal IntPtr nativeImageAttributes;

        internal void SetNativeImageAttributes(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(handle));

            nativeImageAttributes = handle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='ImageAttributes'/> class.
        /// </summary>
        public ImageAttributes()
        {
            IntPtr newImageAttributes = IntPtr.Zero;

            int status = Gdip.GdipCreateImageAttributes(out newImageAttributes);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            SetNativeImageAttributes(newImageAttributes);
        }

        internal ImageAttributes(IntPtr newNativeImageAttributes)
        {
            SetNativeImageAttributes(newNativeImageAttributes);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='ImageAttributes'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
#if FINALIZATION_WATCH
            if (!disposing && nativeImageAttributes != IntPtr.Zero)
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif
            if (nativeImageAttributes != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDisposeImageAttributes(new HandleRef(this, nativeImageAttributes));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, $"GDI+ returned an error status: {status.ToString(CultureInfo.InvariantCulture)}");
#endif
                }
                catch (Exception ex)
                {
                    if (ClientUtils.IsSecurityOrCriticalException(ex))
                    {
                        throw;
                    }

                    Debug.Fail("Exception thrown during Dispose: " + ex.ToString());
                }
                finally
                {
                    nativeImageAttributes = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='ImageAttributes'/>.
        /// </summary>
        ~ImageAttributes()
        {
            Dispose(false);
        }

        /// <summary>
        /// Creates an exact copy of this <see cref='ImageAttributes'/>.
        /// </summary>
        public object Clone()
        {
            IntPtr clone = IntPtr.Zero;

            int status = Gdip.GdipCloneImageAttributes(
                                    new HandleRef(this, nativeImageAttributes),
                                    out clone);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return new ImageAttributes(clone);
        }

        /// <summary>
        /// Sets the 5 X 5 color adjust matrix to the specified <see cref='Matrix'/>.
        /// </summary>
        public void SetColorMatrix(ColorMatrix newColorMatrix)
        {
            SetColorMatrix(newColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);
        }

        /// <summary>
        /// Sets the 5 X 5 color adjust matrix to the specified 'Matrix' with the specified 'ColorMatrixFlags'.
        /// </summary>
        public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag flags)
        {
            SetColorMatrix(newColorMatrix, flags, ColorAdjustType.Default);
        }

        /// <summary>
        /// Sets the 5 X 5 color adjust matrix to the specified 'Matrix' with the  specified 'ColorMatrixFlags'.
        /// </summary>
        public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag mode, ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesColorMatrix(
                        new HandleRef(this, nativeImageAttributes),
                        type,
                        true,
                        newColorMatrix,
                        null,
                        mode);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        /// <summary>
        /// Clears the color adjust matrix to all zeroes.
        /// </summary>
        public void ClearColorMatrix()
        {
            ClearColorMatrix(ColorAdjustType.Default);
        }

        /// <summary>
        /// Clears the color adjust matrix.
        /// </summary>
        public void ClearColorMatrix(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesColorMatrix(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                null,
                null,
                ColorMatrixFlag.Default);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        /// <summary>
        /// Sets a color adjust matrix for image colors and a separate gray scale adjust matrix for gray scale values.
        /// </summary>
        public void SetColorMatrices(ColorMatrix newColorMatrix, ColorMatrix? grayMatrix)
        {
            SetColorMatrices(newColorMatrix, grayMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);
        }

        public void SetColorMatrices(ColorMatrix newColorMatrix, ColorMatrix? grayMatrix, ColorMatrixFlag flags)
        {
            SetColorMatrices(newColorMatrix, grayMatrix, flags, ColorAdjustType.Default);
        }

        public void SetColorMatrices(ColorMatrix newColorMatrix, ColorMatrix? grayMatrix, ColorMatrixFlag mode,
                                     ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesColorMatrix(
                new HandleRef(this, nativeImageAttributes),
                type,
                true,
                newColorMatrix,
                grayMatrix,
                mode);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetThreshold(float threshold)
        {
            SetThreshold(threshold, ColorAdjustType.Default);
        }

        public void SetThreshold(float threshold, ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesThreshold(
                new HandleRef(this, nativeImageAttributes),
                type,
                true,
                threshold);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearThreshold()
        {
            ClearThreshold(ColorAdjustType.Default);
        }

        public void ClearThreshold(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesThreshold(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                0.0f);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetGamma(float gamma)
        {
            SetGamma(gamma, ColorAdjustType.Default);
        }

        public void SetGamma(float gamma, ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesGamma(
                new HandleRef(this, nativeImageAttributes),
                type,
                true,
                gamma);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearGamma()
        {
            ClearGamma(ColorAdjustType.Default);
        }

        public void ClearGamma(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesGamma(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                0.0f);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetNoOp()
        {
            SetNoOp(ColorAdjustType.Default);
        }

        public void SetNoOp(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesNoOp(
                new HandleRef(this, nativeImageAttributes),
                type,
                true);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearNoOp()
        {
            ClearNoOp(ColorAdjustType.Default);
        }

        public void ClearNoOp(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesNoOp(
                new HandleRef(this, nativeImageAttributes),
                type,
                false);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetColorKey(Color colorLow, Color colorHigh)
        {
            SetColorKey(colorLow, colorHigh, ColorAdjustType.Default);
        }

        public void SetColorKey(Color colorLow, Color colorHigh, ColorAdjustType type)
        {
            int lowInt = colorLow.ToArgb();
            int highInt = colorHigh.ToArgb();

            int status = Gdip.GdipSetImageAttributesColorKeys(
                                        new HandleRef(this, nativeImageAttributes),
                                        type,
                                        true,
                                        lowInt,
                                        highInt);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearColorKey()
        {
            ClearColorKey(ColorAdjustType.Default);
        }

        public void ClearColorKey(ColorAdjustType type)
        {
            int zero = 0;
            int status = Gdip.GdipSetImageAttributesColorKeys(
                                        new HandleRef(this, nativeImageAttributes),
                                        type,
                                        false,
                                        zero,
                                        zero);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetOutputChannel(ColorChannelFlag flags)
        {
            SetOutputChannel(flags, ColorAdjustType.Default);
        }

        public void SetOutputChannel(ColorChannelFlag flags, ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesOutputChannel(
                new HandleRef(this, nativeImageAttributes),
                type,
                true,
                flags);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearOutputChannel()
        {
            ClearOutputChannel(ColorAdjustType.Default);
        }

        public void ClearOutputChannel(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesOutputChannel(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                ColorChannelFlag.ColorChannelLast);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetOutputChannelColorProfile(string colorProfileFilename)
        {
            SetOutputChannelColorProfile(colorProfileFilename, ColorAdjustType.Default);
        }

        public void SetOutputChannelColorProfile(string colorProfileFilename,
                                                 ColorAdjustType type)
        {
            // Called in order to emulate exception behavior from .NET Framework related to invalid file paths.
            Path.GetFullPath(colorProfileFilename);

            int status = Gdip.GdipSetImageAttributesOutputChannelColorProfile(
                                        new HandleRef(this, nativeImageAttributes),
                                        type,
                                        true,
                                        colorProfileFilename);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearOutputChannelColorProfile()
        {
            ClearOutputChannel(ColorAdjustType.Default);
        }

        public void ClearOutputChannelColorProfile(ColorAdjustType type)
        {
            int status = Gdip.GdipSetImageAttributesOutputChannel(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                ColorChannelFlag.ColorChannelLast);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetRemapTable(ColorMap[] map)
        {
            SetRemapTable(map, ColorAdjustType.Default);
        }

        public void SetRemapTable(ColorMap[] map, ColorAdjustType type)
        {
            int index = 0;
            int mapSize = map.Length;
            int size = 4; // Marshal.SizeOf(index.GetType());
            IntPtr memory = Marshal.AllocHGlobal(checked(mapSize * size * 2));

            try
            {
                for (index = 0; index < mapSize; index++)
                {
                    Marshal.StructureToPtr(map[index].OldColor.ToArgb(), (IntPtr)((long)memory + index * size * 2), false);
                    Marshal.StructureToPtr(map[index].NewColor.ToArgb(), (IntPtr)((long)memory + index * size * 2 + size), false);
                }

                Gdip.CheckStatus(Gdip.GdipSetImageAttributesRemapTable(
                    new HandleRef(this, nativeImageAttributes),
                    type,
                    true,
                    mapSize,
                    memory));
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        public void ClearRemapTable()
        {
            ClearRemapTable(ColorAdjustType.Default);
        }

        public void ClearRemapTable(ColorAdjustType type)
        {
            Gdip.CheckStatus(Gdip.GdipSetImageAttributesRemapTable(
                new HandleRef(this, nativeImageAttributes),
                type,
                false,
                0,
                IntPtr.Zero));
        }

        public void SetBrushRemapTable(ColorMap[] map)
        {
            SetRemapTable(map, ColorAdjustType.Brush);
        }

        public void ClearBrushRemapTable()
        {
            ClearRemapTable(ColorAdjustType.Brush);
        }

        public void SetWrapMode(WrapMode mode)
        {
            SetWrapMode(mode, default(Color), false);
        }

        public void SetWrapMode(WrapMode mode, Color color)
        {
            SetWrapMode(mode, color, false);
        }

        public void SetWrapMode(WrapMode mode, Color color, bool clamp)
        {
            int status = Gdip.GdipSetImageAttributesWrapMode(
                            new HandleRef(this, nativeImageAttributes),
                            unchecked((int)mode),
                            color.ToArgb(),
                            clamp);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void GetAdjustedPalette(ColorPalette palette, ColorAdjustType type)
        {
            // does inplace adjustment
            IntPtr memory = palette.ConvertToMemory();
            try
            {
                Gdip.CheckStatus(Gdip.GdipGetImageAttributesAdjustedPalette(
                    new HandleRef(this, nativeImageAttributes),
                    memory,
                    type));
                palette.ConvertFromMemory(memory);
            }
            finally
            {
                if (memory != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(memory);
                }
            }
        }
    }
}
