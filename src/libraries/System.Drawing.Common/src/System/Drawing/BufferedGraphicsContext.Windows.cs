// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Drawing
{
    /// <summary>
    /// The BufferedGraphicsContext class can be used to perform standard double buffer rendering techniques.
    /// </summary>
    public sealed class BufferedGraphicsContext : IDisposable
    {
        private Size _maximumBuffer;
        private Size _bufferSize;
        private Size _virtualSize;
        private Point _targetLoc;
        private IntPtr _compatDC;
        private IntPtr _dib;
        private IntPtr _oldBitmap;
        private Graphics _compatGraphics;
        private BufferedGraphics _buffer;
        private int _busy;
        private bool _invalidateWhenFree;

        private const int BufferFree = 0; // The graphics buffer is free to use.
        private const int BufferBusyPainting = 1; // The graphics buffer is busy being created/painting.
        private const int BufferBusyDisposing = 2; // The graphics buffer is busy disposing.

        private static TraceSwitch s_doubleBuffering;

#if DEBUG
        private string _stackAtBusy;
#endif

        /// <summary>
        /// Basic constructor.
        /// </summary>
        public BufferedGraphicsContext()
        {
            // By defualt, the size of our maxbuffer will be 3 x standard button size.
            _maximumBuffer.Width = 75 * 3;
            _maximumBuffer.Height = 32 * 3;

            _bufferSize = Size.Empty;
        }

        ~BufferedGraphicsContext() => Dispose(false);

        /// <summary>
        /// Internal trace switch for debugging
        /// </summary>
        internal static TraceSwitch DoubleBuffering
        {
            get
            {
                if (s_doubleBuffering == null)
                {
                    s_doubleBuffering = new TraceSwitch("DoubleBuffering", "Output information about double buffering");
                }

                return s_doubleBuffering;
            }
        }

        /// <summary>
        /// Allows you to set the maximum width and height of the buffer that will be retained in memory.
        /// You can allocate a buffer of any size, however any request for a buffer that would have a total
        /// memory footprint larger that the maximum size will be allocated temporarily and then discarded 
        /// with the BufferedGraphics is released.
        /// </summary>
        public Size MaximumBuffer
        {
            get => _maximumBuffer;
            set
            {
                if (value.Width <= 0 || value.Height <= 0)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidArgument, nameof(MaximumBuffer), value), nameof(value));
                }

                // If we've been asked to decrease the size of the maximum buffer,
                // then invalidate the older & larger buffer.
                if (value.Width * value.Height < _maximumBuffer.Width * _maximumBuffer.Height)
                {
                    Invalidate();
                }

                _maximumBuffer = value;
            }
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target Graphics object.
        /// </summary>
        public BufferedGraphics Allocate(Graphics targetGraphics, Rectangle targetRectangle)
        {
            if (ShouldUseTempManager(targetRectangle))
            {
                Debug.WriteLineIf(DoubleBuffering.TraceWarning, "Too big of buffer requested (" + targetRectangle.Width + " x " + targetRectangle.Height + ") ... allocating temp buffer manager");
                return AllocBufferInTempManager(targetGraphics, IntPtr.Zero, targetRectangle);
            }

            return AllocBuffer(targetGraphics, IntPtr.Zero, targetRectangle);
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target HDC object.
        /// </summary>
        public BufferedGraphics Allocate(IntPtr targetDC, Rectangle targetRectangle)
        {
            if (ShouldUseTempManager(targetRectangle))
            {
                Debug.WriteLineIf(DoubleBuffering.TraceWarning, "Too big of buffer requested (" + targetRectangle.Width + " x " + targetRectangle.Height + ") ... allocating temp buffer manager");
                return AllocBufferInTempManager(null, targetDC, targetRectangle);
            }

            return AllocBuffer(null, targetDC, targetRectangle);
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target HDC object.
        /// </summary>
        private BufferedGraphics AllocBuffer(Graphics targetGraphics, IntPtr targetDC, Rectangle targetRectangle)
        {
            int oldBusy = Interlocked.CompareExchange(ref _busy, BufferBusyPainting, BufferFree);

            // In the case were we have contention on the buffer - i.e. two threads 
            // trying to use the buffer at the same time, we just create a temp 
            // buffermanager and have the buffer dispose of it when it is done.
            //
            if (oldBusy != BufferFree)
            {
                Debug.WriteLineIf(DoubleBuffering.TraceWarning, "Attempt to have two buffers for a buffer manager... allocating temp buffer manager");
                return AllocBufferInTempManager(targetGraphics, targetDC, targetRectangle);
            }

#if DEBUG
            if (DoubleBuffering.TraceVerbose)
            {
                _stackAtBusy = new StackTrace().ToString();
            }
#endif

            Graphics surface;
            _targetLoc = new Point(targetRectangle.X, targetRectangle.Y);

            try
            {
                if (targetGraphics != null)
                {
                    IntPtr destDc = targetGraphics.GetHdc();
                    try
                    {
                        surface = CreateBuffer(destDc, -_targetLoc.X, -_targetLoc.Y, targetRectangle.Width, targetRectangle.Height);
                    }
                    finally
                    {
                        targetGraphics.ReleaseHdcInternal(destDc);
                    }
                }
                else
                {
                    surface = CreateBuffer(targetDC, -_targetLoc.X, -_targetLoc.Y, targetRectangle.Width, targetRectangle.Height);
                }

                _buffer = new BufferedGraphics(surface, this, targetGraphics, targetDC, _targetLoc, _virtualSize);
            }
            catch
            {
                // Free the buffer so it can be disposed.
                _busy = BufferFree;
                throw;
            }

            return _buffer;
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target HDC object.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
        private BufferedGraphics AllocBufferInTempManager(Graphics targetGraphics, IntPtr targetDC, Rectangle targetRectangle)
        {
            BufferedGraphicsContext tempContext = null;
            BufferedGraphics tempBuffer = null;

            try
            {
                tempContext = new BufferedGraphicsContext();
                tempBuffer = tempContext.AllocBuffer(targetGraphics, targetDC, targetRectangle);
                tempBuffer.DisposeContext = true;
            }
            finally
            {
                if (tempContext != null && (tempBuffer == null || (tempBuffer != null && !tempBuffer.DisposeContext)))
                {
                    tempContext.Dispose();
                }
            }

            return tempBuffer;
        }


        /// <summary>
        /// Fills in the fields of a BITMAPINFO so that we can create a bitmap
        /// that matches the format of the display.
        /// 
        /// This is done by creating a compatible bitmap and calling GetDIBits
        /// to return the color masks. This is done with two calls. The first
        /// call passes in biBitCount = 0 to GetDIBits which will fill in the
        /// base BITMAPINFOHEADER data. The second call to GetDIBits (passing
        /// in the BITMAPINFO filled in by the first call) will return the color
        /// table or bitmasks, as appropriate.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        private bool FillBitmapInfo(IntPtr hdc, IntPtr hpal, ref NativeMethods.BITMAPINFO_FLAT pbmi)
        {
            IntPtr hbm = IntPtr.Zero;
            bool bRet = false;
            try
            {
                // Create a dummy bitmap from which we can query color format info
                // about the device surface.
                hbm = SafeNativeMethods.CreateCompatibleBitmap(new HandleRef(null, hdc), 1, 1);

                if (hbm == IntPtr.Zero)
                {
                    throw new OutOfMemoryException(SR.Format(SR.GraphicsBufferQueryFail));
                }

                pbmi.bmiHeader_biSize = Marshal.SizeOf(typeof(NativeMethods.BITMAPINFOHEADER));
                pbmi.bmiColors = new byte[NativeMethods.BITMAPINFO_MAX_COLORSIZE * 4];
                
                // Call first time to fill in BITMAPINFO header.
                SafeNativeMethods.GetDIBits(new HandleRef(null, hdc),
                                                    new HandleRef(null, hbm),
                                                    0,
                                                    0,
                                                    IntPtr.Zero,
                                                    ref pbmi,
                                                    NativeMethods.DIB_RGB_COLORS);

                if (pbmi.bmiHeader_biBitCount <= 8)
                {
                    bRet = FillColorTable(hdc, hpal, ref pbmi);
                }
                else
                {
                    if (pbmi.bmiHeader_biCompression == NativeMethods.BI_BITFIELDS)
                    {
                        // Call a second time to get the color masks.
                        SafeNativeMethods.GetDIBits(new HandleRef(null, hdc),
                                                new HandleRef(null, hbm),
                                                0,
                                                pbmi.bmiHeader_biHeight,
                                                IntPtr.Zero,
                                                ref pbmi,
                                                NativeMethods.DIB_RGB_COLORS);
                    }
                    bRet = true;
                }
            }
            finally
            {
                if (hbm != IntPtr.Zero)
                {
                    SafeNativeMethods.DeleteObject(new HandleRef(null, hbm));
                    hbm = IntPtr.Zero;
                }
            }
            return bRet;
        }

        /// <summary>
        /// Initialize the color table of the BITMAPINFO pointed to by pbmi. Colors
        /// are set to the current system palette.
        ///
        /// Note: call only valid for displays of 8bpp or less.
        /// </summary>
        /// <returns>True is successful, false otherwise.</returns>
        private unsafe bool FillColorTable(IntPtr hdc, IntPtr hpal, ref NativeMethods.BITMAPINFO_FLAT pbmi)
        {
            byte[] aj = new byte[sizeof(NativeMethods.PALETTEENTRY) * 256];

            fixed (byte* pcolors = pbmi.bmiColors)
            {
                fixed (byte* ppal = aj)
                {
                    NativeMethods.RGBQUAD* prgb = (NativeMethods.RGBQUAD*)pcolors;
                    NativeMethods.PALETTEENTRY* lppe = (NativeMethods.PALETTEENTRY*)ppal;

                    int cColors = 1 << pbmi.bmiHeader_biBitCount;
                    if (cColors <= 256)
                    {
                        Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "8 bit or less...");

                        // Note: we don't support 4bpp displays.
                        uint palRet;
                        IntPtr palHalftone = IntPtr.Zero;
                        if (hpal == IntPtr.Zero)
                        {
                            Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "using halftone palette...");
                            palHalftone = Graphics.GetHalftonePalette();
                            palRet = SafeNativeMethods.GetPaletteEntries(new HandleRef(null, palHalftone), 0, cColors, aj);
                        }
                        else
                        {
                            Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "using custom palette...");
                            palRet = SafeNativeMethods.GetPaletteEntries(new HandleRef(null, hpal), 0, cColors, aj);
                        }

                        if (palRet != 0)
                        {
                            for (int i = 0; i < cColors; i++)
                            {
                                prgb[i].rgbRed = lppe[i].peRed;
                                prgb[i].rgbGreen = lppe[i].peGreen;
                                prgb[i].rgbBlue = lppe[i].peBlue;
                                prgb[i].rgbReserved = 0;
                            }

                            return true;
                        }
                        else
                        {
                            Debug.WriteLineIf(DoubleBuffering.TraceWarning, "FillColorTable: MyGetSystemPaletteEntries failed\n");
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a Graphics object representing a buffer.
        /// </summary>
        private Graphics CreateBuffer(IntPtr src, int offsetX, int offsetY, int width, int height)
        {
            // Create the compat DC.
            _busy = BufferBusyDisposing;
            DisposeDC();
            _busy = BufferBusyPainting;
            _compatDC = UnsafeNativeMethods.CreateCompatibleDC(new HandleRef(null, src));

            // Recreate the bitmap if necessary.
            if (width > _bufferSize.Width || height > _bufferSize.Height)
            {
                Debug.WriteLineIf(DoubleBuffering.TraceInfo, "allocating new bitmap: " + width + " x " + height);
                int optWidth = Math.Max(width, _bufferSize.Width);
                int optHeight = Math.Max(height, _bufferSize.Height);

                _busy = BufferBusyDisposing;
                DisposeBitmap();
                _busy = BufferBusyPainting;

                Debug.WriteLineIf(DoubleBuffering.TraceInfo, "    new size         : " + optWidth + " x " + optHeight);
                IntPtr pvbits = IntPtr.Zero;
                _dib = CreateCompatibleDIB(src, IntPtr.Zero, optWidth, optHeight, ref pvbits);
                _bufferSize = new Size(optWidth, optHeight);
            }

            // Select the bitmap.
            _oldBitmap = SafeNativeMethods.SelectObject(new HandleRef(this, _compatDC), new HandleRef(this, _dib));

            // Create compat graphics.
            Debug.WriteLineIf(DoubleBuffering.TraceInfo, "    Create compatGraphics");
            _compatGraphics = Graphics.FromHdcInternal(_compatDC);
            _compatGraphics.TranslateTransform(-_targetLoc.X, -_targetLoc.Y);
            _virtualSize = new Size(width, height);

            return _compatGraphics;
        }

        /// <summary>
        /// Create a DIB section with an optimal format w.r.t. the specified hdc.
        ///
        /// If DIB <= 8bpp, then the DIB color table is initialized based on the
        /// specified palette. If the palette handle is NULL, then the system
        /// palette is used.
        ///
        /// Note: The hdc must be a direct DC (not an info or memory DC).
        ///
        /// Note: On palettized displays, if the system palette changes the
        ///       UpdateDIBColorTable function should be called to maintain
        ///       the identity palette mapping between the DIB and the display.
        /// </summary>
        /// <returns>A valid bitmap handle if successful, IntPtr.Zero otherwise.</returns>
        [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke")]
        private IntPtr CreateCompatibleDIB(IntPtr hdc, IntPtr hpal, int ulWidth, int ulHeight, ref IntPtr ppvBits)
        {
            if (hdc == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hdc));
            }

            IntPtr hbmRet = IntPtr.Zero;
            var pbmi = new NativeMethods.BITMAPINFO_FLAT();

            // Validate hdc.
            int objType = UnsafeNativeMethods.GetObjectType(new HandleRef(null, hdc));

            switch (objType)
            {
                case NativeMethods.OBJ_DC:
                case NativeMethods.OBJ_METADC:
                case NativeMethods.OBJ_MEMDC:
                case NativeMethods.OBJ_ENHMETADC:
                    break;
                default:
                    throw new ArgumentException(SR.Format(SR.DCTypeInvalid));
            }

            if (FillBitmapInfo(hdc, hpal, ref pbmi))
            {
                // Change bitmap size to match specified dimensions.
                pbmi.bmiHeader_biWidth = ulWidth;
                pbmi.bmiHeader_biHeight = ulHeight;
                if (pbmi.bmiHeader_biCompression == NativeMethods.BI_RGB)
                {
                    pbmi.bmiHeader_biSizeImage = 0;
                }
                else
                {
                    if (pbmi.bmiHeader_biBitCount == 16)
                    {
                        pbmi.bmiHeader_biSizeImage = ulWidth * ulHeight * 2;
                    }
                    else if (pbmi.bmiHeader_biBitCount == 32)
                    {
                        pbmi.bmiHeader_biSizeImage = ulWidth * ulHeight * 4;
                    }
                    else
                    {
                        pbmi.bmiHeader_biSizeImage = 0;
                    }
                }
                pbmi.bmiHeader_biClrUsed = 0;
                pbmi.bmiHeader_biClrImportant = 0;

                // Create the DIB section. Let Win32 allocate the memory and return
                // a pointer to the bitmap surface.
                hbmRet = SafeNativeMethods.CreateDIBSection(new HandleRef(null, hdc), ref pbmi, NativeMethods.DIB_RGB_COLORS, ref ppvBits, IntPtr.Zero, 0);
                Win32Exception ex = null;
                if (hbmRet == IntPtr.Zero)
                {
                    ex = new Win32Exception(Marshal.GetLastWin32Error());
#if DEBUG
                    DumpBitmapInfo(ref pbmi);
#endif
                }

#if DEBUG
                if (DoubleBuffering.TraceVerbose)
                {
                    DumpBitmapInfo(ref pbmi);
                }
#endif
                if (ex != null)
                {
                    throw ex;
                }
            }

            return hbmRet;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the DC, but leaves the bitmap alone.
        /// </summary>
        private void DisposeDC()
        {
            if (_oldBitmap != IntPtr.Zero && _compatDC != IntPtr.Zero)
            {
                Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "restoring bitmap to DC");
                SafeNativeMethods.SelectObject(new HandleRef(this, _compatDC), new HandleRef(this, _oldBitmap));
                _oldBitmap = IntPtr.Zero;
            }

            if (_compatDC != IntPtr.Zero)
            {
                Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "delete compat DC");
                UnsafeNativeMethods.DeleteDC(new HandleRef(this, _compatDC));
                _compatDC = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Disposes the bitmap, will ASSERT if bitmap is being used (checks oldbitmap). if ASSERTed, call DisposeDC() first.
        /// </summary>
        private void DisposeBitmap()
        {
            if (_dib != IntPtr.Zero)
            {
                Debug.Assert(_oldBitmap == IntPtr.Zero);
                Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "delete dib");

                SafeNativeMethods.DeleteObject(new HandleRef(this, _dib));
                _dib = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Disposes of the Graphics buffer.
        /// </summary>
        private void Dispose(bool disposing)
        {
            Debug.WriteLineIf(DoubleBuffering.TraceInfo, "Dispose(" + disposing + ") {");
            Debug.Indent();
            int oldBusy = Interlocked.CompareExchange(ref _busy, BufferBusyDisposing, BufferFree);

            if (disposing)
            {
                if (oldBusy == BufferBusyPainting)
                {
#if DEBUG
                    Debug.WriteLineIf(DoubleBuffering.TraceInfo, "Stack at busy buffer: \n" + _stackAtBusy);
#endif

                    throw new InvalidOperationException(SR.Format(SR.GraphicsBufferCurrentlyBusy));
                }

                if (_compatGraphics != null)
                {
                    Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "Disposing compatGraphics");
                    _compatGraphics.Dispose();
                    _compatGraphics = null;
                }
            }

            DisposeDC();
            DisposeBitmap();

            if (_buffer != null)
            {
                Debug.WriteLineIf(DoubleBuffering.TraceVerbose, "Disposing buffer");
                _buffer.Dispose();
                _buffer = null;
            }

            _bufferSize = Size.Empty;
            _virtualSize = Size.Empty;
            Debug.Unindent();
            Debug.WriteLineIf(DoubleBuffering.TraceInfo, "}");

            _busy = BufferFree;
        }

#if DEBUG
        [ExcludeFromCodeCoverage]
        private void DumpBitmapInfo(ref NativeMethods.BITMAPINFO_FLAT pbmi)
        {
            Debug.WriteLine("biWidth --> " + pbmi.bmiHeader_biWidth);
            Debug.WriteLine("biHeight --> " + pbmi.bmiHeader_biHeight);
            Debug.WriteLine("biPlanes --> " + pbmi.bmiHeader_biPlanes);
            Debug.WriteLine("biBitCount --> " + pbmi.bmiHeader_biBitCount);
            Debug.WriteLine("");
        }
#endif

        /// <summary>
        /// Invalidates the cached graphics buffer.
        /// </summary>
        public void Invalidate()
        {
            int oldBusy = Interlocked.CompareExchange(ref _busy, BufferBusyDisposing, BufferFree);

            // If we're not busy with our buffer, lets clean it up now
            if (oldBusy == BufferFree)
            {
                Dispose();
                _busy = BufferFree;
            }
            else
            {
                // This will indicate to free the buffer as soon as it becomes non-busy.
                _invalidateWhenFree = true;
            }
        }

        /// <summary>
        /// Returns a Graphics object representing a buffer.
        /// </summary>
        internal void ReleaseBuffer(BufferedGraphics buffer)
        {
            _buffer = null;
            if (_invalidateWhenFree)
            {
                // Clears everything including the bitmap.
                _busy = BufferBusyDisposing;
                Dispose();
            }
            else
            {
                // Otherwise, just dispose the DC. A new one will be created next time.
                _busy = BufferBusyDisposing;

                // Only clears out the DC.
                DisposeDC();
            }

            _busy = BufferFree;
        }

        /// <summary>
        /// This routine allows us to control the point were we start using throw away
        /// managers for painting. Since the buffer manager stays around (by default)
        /// for the life of the app, we don't want to consume too much memory
        /// in the buffer. However, re-allocating the buffer for small things (like
        /// buttons, labels, etc) will hit us on runtime performance.
        /// </summary>
        private bool ShouldUseTempManager(Rectangle targetBounds)
        {
            return (targetBounds.Width * targetBounds.Height) > (MaximumBuffer.Width * MaximumBuffer.Height);
        }
    }
}
