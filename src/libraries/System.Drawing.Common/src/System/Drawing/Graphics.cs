// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates a GDI+ drawing surface.
    /// </summary>
    public sealed class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
#if FINALIZATION_WATCH
        static readonly TraceSwitch GraphicsFinalization = new TraceSwitch("GraphicsFinalization", "Tracks the creation and destruction of finalization");
        internal static string GetAllocationStack() {
            if (GraphicsFinalization.TraceVerbose) {
                return Environment.StackTrace;
            }
            else {
                return "Enabled 'GraphicsFinalization' switch to see stack of allocation";
            }
        }
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        /// <summary>
        /// The context state previous to the current Graphics context (the head of the stack).
        /// We don't keep a GraphicsContext for the current context since it is available at any time from GDI+ and
        /// we don't want to keep track of changes in it.
        /// </summary>
        private GraphicsContext? _previousContext;

        private static readonly object s_syncObject = new object();

        // Object reference used for printing; it could point to a PrintPreviewGraphics to obtain the VisibleClipBounds, or
        // a DeviceContext holding a printer DC.
        private object? _printingHelper;

        // GDI+'s preferred HPALETTE.
        private static IntPtr s_halftonePalette;

        // pointer back to the Image backing a specific graphic object
        private Image? _backingImage;

        /// <summary>
        /// Handle to native DC - obtained from the GDI+ graphics object. We need to cache it to implement
        /// IDeviceContext interface.
        /// </summary>
        private IntPtr _nativeHdc;

        public delegate bool DrawImageAbort(IntPtr callbackdata);

#if NET7_0_OR_GREATER
        [CustomMarshaller(typeof(DrawImageAbort), MarshalMode.ManagedToUnmanagedIn, typeof(KeepAliveMarshaller))]
        internal static class DrawImageAbortMarshaller
        {
            internal unsafe struct KeepAliveMarshaller
            {
                private delegate Interop.BOOL DrawImageAbortNative(IntPtr callbackdata);
                private DrawImageAbortNative? _managed;
                private delegate* unmanaged<IntPtr, Interop.BOOL> _nativeFunction;
                public void FromManaged(DrawImageAbort? managed)
                {
                    _managed = managed is null ? null : data => managed(data) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
                    _nativeFunction = _managed is null ? null : (delegate* unmanaged<IntPtr, Interop.BOOL>)Marshal.GetFunctionPointerForDelegate(_managed);
                }

                public delegate* unmanaged<IntPtr, Interop.BOOL> ToUnmanaged()
                {
                    return _nativeFunction;
                }

                public void OnInvoked()
                {
                    GC.KeepAlive(_managed);
                }

                public void Free() { }
            }
        }
#endif

        /// <summary>
        /// Callback for EnumerateMetafile methods.
        /// This method can then call Metafile.PlayRecord to play the record that was just enumerated.
        /// </summary>
        /// <param name="recordType">if >= MinRecordType, it's an EMF+ record</param>
        /// <param name="flags">always 0 for EMF records</param>
        /// <param name="dataSize">size of the data, or 0 if no data</param>
        /// <param name="data">pointer to the data, or NULL if no data (UINT32 aligned)</param>
        /// <param name="callbackData">pointer to callbackData, if any</param>
        /// <returns>False to abort enumerating, true to continue.</returns>
        public delegate bool EnumerateMetafileProc(
            EmfPlusRecordType recordType,
            int flags,
            int dataSize,
            IntPtr data,
            PlayRecordCallback? callbackData);

#if NET7_0_OR_GREATER
        [CustomMarshaller(typeof(EnumerateMetafileProc), MarshalMode.ManagedToUnmanagedIn, typeof(KeepAliveMarshaller))]
        internal static class EnumerateMetafileProcMarshaller
        {
            internal unsafe struct KeepAliveMarshaller
            {
                private delegate Interop.BOOL EnumerateMetafileProcNative(
                    EmfPlusRecordType recordType,
                    int flags,
                    int dataSize,
                    IntPtr data,
                    IntPtr callbackData);
                private EnumerateMetafileProcNative? _managed;
                private delegate* unmanaged<IntPtr, Interop.BOOL> _nativeFunction;
                public void FromManaged(EnumerateMetafileProc? managed)
                {
                    _managed = managed is null ? null : (recordType, flags, dataSize, data, callbackData) =>
                        managed(recordType, flags, dataSize, data, callbackData == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<PlayRecordCallback>(callbackData)) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
                    _nativeFunction = _managed is null ? null : (delegate* unmanaged<IntPtr, Interop.BOOL>)Marshal.GetFunctionPointerForDelegate(_managed);
                }

                public delegate* unmanaged<IntPtr, Interop.BOOL> ToUnmanaged()
                {
                    return _nativeFunction;
                }

                public void OnInvoked()
                {
                    GC.KeepAlive(_managed);
                }

                public void Free() {}
            }
        }
#endif

        /// <summary>
        /// Constructor to initialize this object from a native GDI+ Graphics pointer.
        /// </summary>
        private Graphics(IntPtr gdipNativeGraphics)
        {
            if (gdipNativeGraphics == IntPtr.Zero)
                throw new ArgumentNullException(nameof(gdipNativeGraphics));

            NativeGraphics = gdipNativeGraphics;
        }

        /// <summary>
        /// Creates a new instance of the <see cref='Graphics'/> class from the specified handle to a device context.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc)
        {
            if (hdc == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hdc));

            return FromHdcInternal(hdc);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdcInternal(IntPtr hdc)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC(hdc, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates a new instance of the Graphics class from the specified handle to a device context and handle to a device.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC2(hdc, hdevice, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates a new instance of the <see cref='Graphics'/> class from a window handle.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwnd(IntPtr hwnd) => FromHwndInternal(hwnd);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwndInternal(IntPtr hwnd)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHWND(hwnd, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates an instance of the <see cref='Graphics'/> class from an existing <see cref='Image'/>.
        /// </summary>
        public static Graphics FromImage(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            if ((image.PixelFormat & PixelFormat.Indexed) != 0)
                throw new ArgumentException(SR.GdiplusCannotCreateGraphicsFromIndexedPixelFormat, nameof(image));

            Gdip.CheckStatus(Gdip.GdipGetImageGraphicsContext(
                new HandleRef(image, image.nativeImage),
                out IntPtr nativeGraphics));

            return new Graphics(nativeGraphics) { _backingImage = image };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReleaseHdcInternal(IntPtr hdc)
        {
            Gdip.CheckStatus(!Gdip.Initialized ? Gdip.Ok :
                Gdip.GdipReleaseDC(new HandleRef(this, NativeGraphics), hdc));
            _nativeHdc = IntPtr.Zero;
        }

        /// <summary>
        /// Deletes this <see cref='Graphics'/>, and frees the memory allocated for it.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
#if DEBUG && FINALIZATION_WATCH
            if (!disposing && _nativeGraphics != IntPtr.Zero)
            {
                Debug.WriteLine("System.Drawing.Graphics: ***************************************************");
                Debug.WriteLine("System.Drawing.Graphics: Object Disposed through finalization:\n" + allocationSite);
            }
#endif
            while (_previousContext != null)
            {
                // Dispose entire stack.
                GraphicsContext? context = _previousContext.Previous;
                _previousContext.Dispose();
                _previousContext = context;
            }

            if (NativeGraphics != IntPtr.Zero)
            {
                try
                {
                    if (_nativeHdc != IntPtr.Zero) // avoid a handle leak.
                    {
                        ReleaseHdc();
                    }

                    if (PrintingHelper is DeviceContext printerDC)
                    {
                        printerDC.Dispose();
                        _printingHelper = null;
                    }

#if DEBUG
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDeleteGraphics(new HandleRef(this, NativeGraphics));

#if DEBUG
                    Debug.Assert(status == Gdip.Ok, $"GDI+ returned an error status: {status.ToString(CultureInfo.InvariantCulture)}");
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                }
                finally
                {
                    NativeGraphics = IntPtr.Zero;
                }
            }
        }

        ~Graphics() => Dispose(false);

        /// <summary>
        /// Handle to native GDI+ graphics object. This object is created on demand.
        /// </summary>
        internal IntPtr NativeGraphics { get; private set; }

        public Region Clip
        {
            get
            {
                var region = new Region();
                int status = Gdip.GdipGetClip(new HandleRef(this, NativeGraphics), new HandleRef(region, region.NativeRegion));
                Gdip.CheckStatus(status);

                return region;
            }
            set => SetClip(value, CombineMode.Replace);
        }

        public RectangleF ClipBounds
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetClipBounds(new HandleRef(this, NativeGraphics), out RectangleF rect));
                return rect;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref='Drawing2D.CompositingMode'/> associated with this <see cref='Graphics'/>.
        /// </summary>
        public CompositingMode CompositingMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetCompositingMode(new HandleRef(this, NativeGraphics), out CompositingMode mode));
                return mode;
            }
            set
            {
                if (value < CompositingMode.SourceOver || value > CompositingMode.SourceCopy)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CompositingMode));

                Gdip.CheckStatus(Gdip.GdipSetCompositingMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public CompositingQuality CompositingQuality
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetCompositingQuality(new HandleRef(this, NativeGraphics), out CompositingQuality cq));
                return cq;
            }
            set
            {
                if (value < CompositingQuality.Invalid || value > CompositingQuality.AssumeLinear)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CompositingQuality));

                Gdip.CheckStatus(Gdip.GdipSetCompositingQuality(new HandleRef(this, NativeGraphics), value));
            }
        }

        public float DpiX
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetDpiX(new HandleRef(this, NativeGraphics), out float dpi));
                return dpi;
            }
        }

        public float DpiY
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetDpiY(new HandleRef(this, NativeGraphics), out float dpi));
                return dpi;
            }
        }

        /// <summary>
        /// Gets or sets the interpolation mode associated with this Graphics.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetInterpolationMode(new HandleRef(this, NativeGraphics), out InterpolationMode mode));
                return mode;
            }
            set
            {
                if (value < InterpolationMode.Invalid || value > InterpolationMode.HighQualityBicubic)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(InterpolationMode));

                // GDI+ interprets the value of InterpolationMode and sets a value accordingly.
                // Libgdiplus does not, so do this manually here.
                switch (value)
                {
                    case InterpolationMode.Default:
                    case InterpolationMode.Low:
                        value = InterpolationMode.Bilinear;
                        break;
                    case InterpolationMode.High:
                        value = InterpolationMode.HighQualityBicubic;
                        break;
                    case InterpolationMode.Invalid:
                        throw new ArgumentException(SR.GdiplusInvalidParameter);
                    default:
                        break;
                }

                Gdip.CheckStatus(Gdip.GdipSetInterpolationMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public bool IsClipEmpty
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsClipEmpty(new HandleRef(this, NativeGraphics), out bool isEmpty));
                return isEmpty;
            }
        }

        public bool IsVisibleClipEmpty
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsVisibleClipEmpty(new HandleRef(this, NativeGraphics), out bool isEmpty));
                return isEmpty;
            }
        }

        public float PageScale
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPageScale(new HandleRef(this, NativeGraphics), out float scale));
                return scale;
            }
            set
            {
                // Libgdiplus doesn't perform argument validation, so do this here for compatibility.
                if (value <= 0 || value > 1000000032)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPageScale(new HandleRef(this, NativeGraphics), value));
            }
        }

        public GraphicsUnit PageUnit
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPageUnit(new HandleRef(this, NativeGraphics), out GraphicsUnit unit));
                return unit;
            }
            set
            {
                if (value < GraphicsUnit.World || value > GraphicsUnit.Millimeter)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(GraphicsUnit));

                // GDI+ doesn't allow GraphicsUnit.World as a valid value for PageUnit.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == GraphicsUnit.World)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPageUnit(new HandleRef(this, NativeGraphics), value));
            }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPixelOffsetMode(new HandleRef(this, NativeGraphics), out PixelOffsetMode mode));
                return mode;
            }
            set
            {
                if (value < PixelOffsetMode.Invalid || value > PixelOffsetMode.Half)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(PixelOffsetMode));

                // GDI+ doesn't allow PixelOffsetMode.Invalid as a valid value for PixelOffsetMode.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == PixelOffsetMode.Invalid)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPixelOffsetMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public Point RenderingOrigin
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetRenderingOrigin(new HandleRef(this, NativeGraphics), out int x, out int y));
                return new Point(x, y);
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetRenderingOrigin(new HandleRef(this, NativeGraphics), value.X, value.Y));
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetSmoothingMode(new HandleRef(this, NativeGraphics), out SmoothingMode mode));
                return mode;
            }
            set
            {
                if (value < SmoothingMode.Invalid || value > SmoothingMode.AntiAlias)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(SmoothingMode));

                // GDI+ interprets the value of SmoothingMode and sets a value accordingly.
                // Libgdiplus does not, so do this manually here.
                switch (value)
                {
                    case SmoothingMode.Default:
                    case SmoothingMode.HighSpeed:
                        value = SmoothingMode.None;
                        break;
                    case SmoothingMode.HighQuality:
                        value = SmoothingMode.AntiAlias;
                        break;
                    case SmoothingMode.Invalid:
                        throw new ArgumentException(SR.GdiplusInvalidParameter);
                    default:
                        break;
                }

                Gdip.CheckStatus(Gdip.GdipSetSmoothingMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public int TextContrast
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetTextContrast(new HandleRef(this, NativeGraphics), out int textContrast));
                return textContrast;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetTextContrast(new HandleRef(this, NativeGraphics), value));
            }
        }

        /// <summary>
        /// Gets or sets the rendering mode for text associated with this <see cref='Graphics'/>.
        /// </summary>
        public TextRenderingHint TextRenderingHint
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetTextRenderingHint(new HandleRef(this, NativeGraphics), out TextRenderingHint hint));
                return hint;
            }
            set
            {
                if (value < TextRenderingHint.SystemDefault || value > TextRenderingHint.ClearTypeGridFit)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(TextRenderingHint));

                Gdip.CheckStatus(Gdip.GdipSetTextRenderingHint(new HandleRef(this, NativeGraphics), value));
            }
        }

        /// <summary>
        /// Gets or sets the world transform for this <see cref='Graphics'/>.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                Gdip.CheckStatus(Gdip.GdipGetWorldTransform(
                    new HandleRef(this, NativeGraphics), new HandleRef(matrix, matrix.NativeMatrix)));

                return matrix;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetWorldTransform(
                    new HandleRef(this, NativeGraphics), new HandleRef(value, value.NativeMatrix)));
            }
        }

        /// <summary>
        /// Gets or sets the world transform elements for this <see cref="Graphics"/>.
        /// </summary>
        /// <remarks>
        /// This is a more performant alternative to <see cref="Transform"/> that does not need disposal.
        /// </remarks>
        public unsafe Matrix3x2 TransformElements
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix(out IntPtr nativeMatrix));

                try
                {
                    Gdip.CheckStatus(Gdip.GdipGetWorldTransform(
                        new HandleRef(this, NativeGraphics), new HandleRef(null, nativeMatrix)));

                    Matrix3x2 matrix = default;
                    Gdip.CheckStatus(Gdip.GdipGetMatrixElements(new HandleRef(null, nativeMatrix), (float*)&matrix));
                    return matrix;
                }
                finally
                {
                    if (nativeMatrix != IntPtr.Zero)
                    {
                        Gdip.GdipDeleteMatrix(new HandleRef(null, nativeMatrix));
                    }
                }
            }
            set
            {
                IntPtr nativeMatrix = Matrix.CreateNativeHandle(value);

                try
                {
                    Gdip.CheckStatus(Gdip.GdipSetWorldTransform(
                        new HandleRef(this, NativeGraphics), new HandleRef(null, nativeMatrix)));
                }
                finally
                {
                    if (nativeMatrix != IntPtr.Zero)
                    {
                        Gdip.GdipDeleteMatrix(new HandleRef(null, nativeMatrix));
                    }
                }
            }
        }

        public IntPtr GetHdc()
        {
            IntPtr hdc;
            Gdip.CheckStatus(Gdip.GdipGetDC(new HandleRef(this, NativeGraphics), out hdc));

            _nativeHdc = hdc; // need to cache the hdc to be able to release with a call to IDeviceContext.ReleaseHdc().
            return _nativeHdc;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void ReleaseHdc(IntPtr hdc) => ReleaseHdcInternal(hdc);

        public void ReleaseHdc() => ReleaseHdcInternal(_nativeHdc);

        /// <summary>
        /// Forces immediate execution of all operations currently on the stack.
        /// </summary>
        public void Flush() => Flush(FlushIntention.Flush);

        /// <summary>
        /// Forces execution of all operations currently on the stack.
        /// </summary>
        public void Flush(FlushIntention intention)
        {
            Gdip.CheckStatus(Gdip.GdipFlush(new HandleRef(this, NativeGraphics), intention));
        }

        public void SetClip(Graphics g) => SetClip(g, CombineMode.Replace);

        public void SetClip(Graphics g, CombineMode combineMode)
        {
            ArgumentNullException.ThrowIfNull(g);

            Gdip.CheckStatus(Gdip.GdipSetClipGraphics(
                new HandleRef(this, NativeGraphics),
                new HandleRef(g, g.NativeGraphics),
                combineMode));
        }

        public void SetClip(Rectangle rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(GraphicsPath path) => SetClip(path, CombineMode.Replace);

        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            ArgumentNullException.ThrowIfNull(path);

            Gdip.CheckStatus(Gdip.GdipSetClipPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(path, path._nativePath),
                combineMode));
        }

        public void SetClip(Region region, CombineMode combineMode)
        {
            ArgumentNullException.ThrowIfNull(region);

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                combineMode));
        }

        public void IntersectClip(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(Region region)
        {
            ArgumentNullException.ThrowIfNull(region);

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                CombineMode.Intersect));
        }

        public void ExcludeClip(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Exclude));
        }

        public void ExcludeClip(Region region)
        {
            ArgumentNullException.ThrowIfNull(region);

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                CombineMode.Exclude));
        }

        public void ResetClip()
        {
            Gdip.CheckStatus(Gdip.GdipResetClip(new HandleRef(this, NativeGraphics)));
        }

        public void TranslateClip(float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy));
        }

        public void TranslateClip(int dx, int dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy));
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y));

        public bool IsVisible(Point point)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePointI(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y));

        public bool IsVisible(PointF point)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePoint(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            return IsVisible(new Rectangle(x, y, width, height));
        }

        public bool IsVisible(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return IsVisible(new RectangleF(x, y, width, height));
        }

        public bool IsVisible(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out bool isVisible));

            return isVisible;
        }

        /// <summary>
        /// Resets the world transform to identity.
        /// </summary>
        public void ResetTransform()
        {
            Gdip.CheckStatus(Gdip.GdipResetWorldTransform(new HandleRef(this, NativeGraphics)));
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
            ArgumentNullException.ThrowIfNull(matrix);

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatibility with GDI+.
            if (matrix.NativeMatrix == IntPtr.Zero)
                return;

            Gdip.CheckStatus(Gdip.GdipMultiplyWorldTransform(
                new HandleRef(this, NativeGraphics), new HandleRef(matrix, matrix.NativeMatrix), order));
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateWorldTransform(new HandleRef(this, NativeGraphics), dx, dy, order));
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScaleWorldTransform(new HandleRef(this, NativeGraphics), sx, sy, order));
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotateWorldTransform(new HandleRef(this, NativeGraphics), angle, order));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawArc(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawArcI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four ordered pairs that represent points.
        /// </summary>
        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawBezier(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, Point pt1, Point pt2, Point pt3, Point pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        /// <summary>
        /// Draws the outline of a rectangle specified by <paramref name="rect"/>.
        /// </summary>
        /// <param name="pen">A Pen that determines the color, width, and style of the rectangle.</param>
        /// <param name="rect">A Rectangle structure that represents the rectangle to draw.</param>
        public void DrawRectangle(Pen pen, RectangleF rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of a rectangle specified by <paramref name="rect"/>.
        /// </summary>
        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawRectangle(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawRectangleI(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen, RectangleF[] rects)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(rects);

            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectangles(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen, Rectangle[] rects)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(rects);

            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectanglesI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of an ellipse specified by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygon(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygonI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the lines and curves defined by a <see cref='GraphicsPath'/>.
        /// </summary>
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(path);

            CheckErrorStatus(Gdip.GdipDrawPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                new HandleRef(path, path._nativePath)));
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments)
        {
            DrawCurve(pen, points, offset, numberOfSegments, 0.5f);
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points, int offset, int numberOfSegments, float tension)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, PointF[] points, float tension, FillMode fillmode)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, Point[] points, float tension, FillMode fillmode)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Fills the entire drawing surface with the specified color.
        /// </summary>
        public void Clear(Color color)
        {
            Gdip.CheckStatus(Gdip.GdipGraphicsClear(new HandleRef(this, NativeGraphics), color.ToArgb()));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillRectangle(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillRectangleI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush, RectangleF[] rects)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(rects);

            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectangles(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush, Rectangle[] rects)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(rects);

            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectanglesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, PointF[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush, PointF[] points, FillMode fillMode)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygon(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, Point[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush, Point[] points, FillMode fillMode)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygonI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        /// <param name="brush">A Brush that determines the characteristics of the fill.</param>
        /// <param name="rect">A Rectangle structure that represents the bounding rectangle that defines the ellipse from which the pie section comes.</param>
        /// <param name="startAngle">Angle in degrees measured clockwise from the x-axis to the first side of the pie section.</param>
        /// <param name="sweepAngle">Angle in degrees measured clockwise from the <paramref name="startAngle"/> parameter to the second side of the pie section.</param>
        public void FillPie(Brush brush, RectangleF rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            ArgumentNullException.ThrowIfNull(brush);

            CheckErrorStatus(Gdip.GdipFillPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a closed curve defined by an array of points.
        /// </summary>
        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode, float tension)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
        }

        /// <summary>
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode, float tension)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
        }

        /// <summary>
        /// Draws a string with the specified font.
        /// </summary>
        public void DrawString(string? s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), null);
        }

        public void DrawString(string? s, Font font, Brush brush, PointF point)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), null);
        }

        public void DrawString(string? s, Font font, Brush brush, float x, float y, StringFormat? format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), format);
        }

        public void DrawString(string? s, Font font, Brush brush, PointF point, StringFormat? format)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);
        }

        public void DrawString(string? s, Font font, Brush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }

        public void DrawString(string? s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat? format)
        {
            ArgumentNullException.ThrowIfNull(brush);
            if (string.IsNullOrEmpty(s))
                return;
            ArgumentNullException.ThrowIfNull(font);

            CheckErrorStatus(Gdip.GdipDrawString(
                new HandleRef(this, NativeGraphics),
                s,
                s.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRectangle,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero),
                new HandleRef(brush, brush.NativeBrush)));
        }

        public SizeF MeasureString(
            string? text,
            Font font,
            SizeF layoutArea,
            StringFormat? stringFormat,
            out int charactersFitted,
            out int linesFilled)
        {
            if (string.IsNullOrEmpty(text))
            {
                charactersFitted = 0;
                linesFilled = 0;
                return SizeF.Empty;
            }

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out charactersFitted,
                out linesFilled));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font, PointF origin, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(origin.X, origin.Y, 0, 0);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out _,
                out _));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font, SizeF layoutArea) => MeasureString(text, font, layoutArea, null);

        public SizeF MeasureString(string? text, Font font, SizeF layoutArea, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out _,
                out _));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font)
        {
            return MeasureString(text, font, new SizeF(0, 0));
        }

        public SizeF MeasureString(string? text, Font font, int width)
        {
            return MeasureString(text, font, new SizeF(width, 999999));
        }

        public SizeF MeasureString(string? text, Font font, int width, StringFormat? format)
        {
            return MeasureString(text, font, new SizeF(width, 999999), format);
        }

        public Region[] MeasureCharacterRanges(string? text, Font font, RectangleF layoutRect, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<Region>();
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            Gdip.CheckStatus(Gdip.GdipGetStringFormatMeasurableCharacterRangeCount(
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                out int count));

            IntPtr[] gpRegions = new IntPtr[count];
            Region[] regions = new Region[count];

            for (int f = 0; f < count; f++)
            {
                regions[f] = new Region();
                gpRegions[f] = regions[f].NativeRegion;
            }

            Gdip.CheckStatus(Gdip.GdipMeasureCharacterRanges(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRect,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                count,
                gpRegions));

            return regions;
        }

        /// <summary>
        /// Draws the specified image at the specified location.
        /// </summary>
        public void DrawImage(Image image, PointF point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, float x, float y)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImage(
                new HandleRef(this, NativeGraphics), new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, RectangleF rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, int x, int y)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image, int x, int y, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImageUnscaled(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaled(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y, int width, int height)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
        {
            ArgumentNullException.ThrowIfNull(image);

            int width = Math.Min(rect.Width, image.Width);
            int height = Math.Min(rect.Height, image.Height);

            // We could put centering logic here too for the case when the image
            // is smaller than the rect.
            DrawImage(image, rect, 0, 0, width, height, GraphicsUnit.Pixel);
        }

        // Affine or perspective blt
        //  destPoints.Length = 3: rect => parallelogram
        // destPoints[0] <=> top-left corner of the source rectangle
        //      destPoints[1] <=> top-right corner
        //       destPoints[2] <=> bottom-left corner
        //  destPoints.Length = 4: rect => quad
        // destPoints[3] <=> bottom-right corner
        //
        //  @notes Perspective blt only works for bitmap images.

        public unsafe void DrawImage(Image image, PointF[] destPoints)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(destPoints);

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public unsafe void DrawImage(Image image, Point[] destPoints)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(destPoints);

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image, float x, float y, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImagePointRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImagePointRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public unsafe void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(destPoints);

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    NativeMethods.NullHandleRef,
                    null,
                    NativeMethods.NullHandleRef);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes? imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        public void DrawImage(
            Image image,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        public unsafe void DrawImage(
            Image image,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback,
            int callbackData)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(destPoints);

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, null, null, 0);
        }

        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        public unsafe void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback,
            int callbackData)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(destPoints);

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRectI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, callback, IntPtr.Zero);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback,
            IntPtr callbackData)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, callback, IntPtr.Zero);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback,
            IntPtr callbackData)
        {
            ArgumentNullException.ThrowIfNull(image);

            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLines(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }


        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawLineI(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2));
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLinesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p,
                    points.Length));
            }
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize)
        {
            CopyFromScreen(sourceX, sourceY, destinationX, destinationY, blockRegionSize, CopyPixelOperation.SourceCopy);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size
        /// and specified raster operation.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize, copyPixelOperation);
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero, null);
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts)
        {
            ArgumentNullException.ThrowIfNull(pts);

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPoints(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] pts)
        {
            ArgumentNullException.ThrowIfNull(pts);

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPointsI(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
            }
        }

        /// <summary>
        /// GDI+ will return a 'generic error' when we attempt to draw an Emf
        /// image with width/height == 1. Here, we will hack around this by
        /// resetting the errorstatus. Note that we don't do simple arg checking
        /// for height || width == 1 here because transforms can be applied to
        /// the Graphics object making it difficult to identify this scenario.
        /// </summary>
        private static void IgnoreMetafileErrors(Image image, ref int errorStatus)
        {
            if (errorStatus != Gdip.Ok && image.RawFormat.Equals(ImageFormat.Emf))
                errorStatus = Gdip.Ok;
        }

        /// <summary>
        /// Creates a Region class only if the native region is not infinite.
        /// </summary>
        internal Region? GetRegionIfNotInfinite()
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegion(out IntPtr regionHandle));
            try
            {
                Gdip.GdipGetClip(new HandleRef(this, NativeGraphics), new HandleRef(null, regionHandle));
                Gdip.CheckStatus(Gdip.GdipIsInfiniteRegion(
                    new HandleRef(null, regionHandle),
                    new HandleRef(this, NativeGraphics),
                    out int isInfinite));

                if (isInfinite != 0)
                {
                    // Infinite
                    return null;
                }

                Region region = new Region(regionHandle);
                regionHandle = IntPtr.Zero;
                return region;
            }
            finally
            {
                if (regionHandle != IntPtr.Zero)
                {
                    Gdip.GdipDeleteRegion(new HandleRef(null, regionHandle));
                }
            }
        }

        /// <summary>
        /// Represents an object used in connection with the printing API, it is used to hold a reference to a
        /// PrintPreviewGraphics (fake graphics) or a printer DeviceContext (and maybe more in the future).
        /// </summary>
        internal object? PrintingHelper
        {
            get => _printingHelper;
            set
            {
                Debug.Assert(_printingHelper == null, "WARNING: Overwritting the printing helper reference!");
                _printingHelper = value;
            }
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size
        /// and specified raster operation.
        /// </summary>
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            switch (copyPixelOperation)
            {
                case CopyPixelOperation.Blackness:
                case CopyPixelOperation.NotSourceErase:
                case CopyPixelOperation.NotSourceCopy:
                case CopyPixelOperation.SourceErase:
                case CopyPixelOperation.DestinationInvert:
                case CopyPixelOperation.PatInvert:
                case CopyPixelOperation.SourceInvert:
                case CopyPixelOperation.SourceAnd:
                case CopyPixelOperation.MergePaint:
                case CopyPixelOperation.MergeCopy:
                case CopyPixelOperation.SourceCopy:
                case CopyPixelOperation.SourcePaint:
                case CopyPixelOperation.PatCopy:
                case CopyPixelOperation.PatPaint:
                case CopyPixelOperation.Whiteness:
                case CopyPixelOperation.CaptureBlt:
                case CopyPixelOperation.NoMirrorBitmap:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(copyPixelOperation), (int)copyPixelOperation, typeof(CopyPixelOperation));
            }

            int destWidth = blockRegionSize.Width;
            int destHeight = blockRegionSize.Height;

            IntPtr screenDC = Interop.User32.GetDC(IntPtr.Zero);
            try
            {
                IntPtr targetDC = GetHdc();
                int result = Interop.Gdi32.BitBlt(
                    targetDC,
                    destinationX,
                    destinationY,
                    destWidth,
                    destHeight,
                    screenDC,
                    sourceX,
                    sourceY,
                    (Interop.Gdi32.RasterOp)copyPixelOperation);

                //a zero result indicates a win32 exception has been thrown
                if (result == 0)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Interop.User32.ReleaseDC(IntPtr.Zero, screenDC);
                ReleaseHdc();
            }
        }

        public Color GetNearestColor(Color color)
        {
            int nearest = color.ToArgb();
            Gdip.CheckStatus(Gdip.GdipGetNearestColor(new HandleRef(this, NativeGraphics), ref nearest));
            return Color.FromArgb(nearest);
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            ArgumentNullException.ThrowIfNull(pen);

            CheckErrorStatus(Gdip.GdipDrawLine(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2));
        }

        /// <summary>
        /// Draws a series of cubic Bezier curves from an array of points.
        /// </summary>
        public unsafe void DrawBeziers(Pen pen, PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawBeziers(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a series of cubic Bezier curves from an array of points.
        /// </summary>
        public unsafe void DrawBeziers(Pen pen, Point[] points)
        {
            ArgumentNullException.ThrowIfNull(pen);
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawBeziersI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p,
                    points.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a path.
        /// </summary>
        public void FillPath(Brush brush, GraphicsPath path)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(path);

            CheckErrorStatus(Gdip.GdipFillPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                new HandleRef(path, path._nativePath)));
        }

        /// <summary>
        /// Fills the interior of a <see cref='Region'/>.
        /// </summary>
        public void FillRegion(Brush brush, Region region)
        {
            ArgumentNullException.ThrowIfNull(brush);
            ArgumentNullException.ThrowIfNull(region);

            CheckErrorStatus(Gdip.GdipFillRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                new HandleRef(region, region.NativeRegion)));
        }

        public void DrawIcon(Icon icon, int x, int y)
        {
            ArgumentNullException.ThrowIfNull(icon);

            if (_backingImage != null)
            {
                // We don't call the icon directly because we want to stay in GDI+ all the time
                // to avoid alpha channel interop issues between gdi and gdi+
                // so we do icon.ToBitmap() and then we call DrawImage. This is probably slower.
                DrawImage(icon.ToBitmap(), x, y);
            }
            else
            {
                icon.Draw(this, x, y);
            }
        }

        /// <summary>
        /// Draws this image to a graphics object. The drawing command originates on the graphics
        /// object, but a graphics object generally has no idea how to render a given image. So,
        /// it passes the call to the actual image. This version crops the image to the given
        /// dimensions and allows the user to specify a rectangle within the image to draw.
        /// </summary>
        public void DrawIcon(Icon icon, Rectangle targetRect)
        {
            ArgumentNullException.ThrowIfNull(icon);

            if (_backingImage != null)
            {
                // We don't call the icon directly because we want to stay in GDI+ all the time
                // to avoid alpha channel interop issues between gdi and gdi+
                // so we do icon.ToBitmap() and then we call DrawImage. This is probably slower.
                DrawImage(icon.ToBitmap(), targetRect);
            }
            else
            {
                icon.Draw(this, targetRect);
            }
        }

        /// <summary>
        /// Draws this image to a graphics object. The drawing command originates on the graphics
        /// object, but a graphics object generally has no idea how to render a given image. So,
        /// it passes the call to the actual image. This version stretches the image to the given
        /// dimensions and allows the user to specify a rectangle within the image to draw.
        /// </summary>
        public void DrawIconUnstretched(Icon icon, Rectangle targetRect)
        {
            ArgumentNullException.ThrowIfNull(icon);

            if (_backingImage != null)
            {
                DrawImageUnscaled(icon.ToBitmap(), targetRect);
            }
            else
            {
                icon.DrawUnstretched(this, targetRect);
            }
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            ArgumentNullException.ThrowIfNull(destPoints);

            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (PointF* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            ArgumentNullException.ThrowIfNull(destPoints);

            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (Point* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            ArgumentNullException.ThrowIfNull(destPoints);

            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (PointF* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    ref srcRect,
                    unit,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            ArgumentNullException.ThrowIfNull(destPoints);

            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (Point* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    ref srcRect,
                    unit,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        /// <summary>
        /// Combines current Graphics context with all previous contexts.
        /// When BeginContainer() is called, a copy of the current context is pushed into the GDI+ context stack, it keeps track of the
        /// absolute clipping and transform but reset the public properties so it looks like a brand new context.
        /// When Save() is called, a copy of the current context is also pushed in the GDI+ stack but the public clipping and transform
        /// properties are not reset (cumulative). Consecutive Save context are ignored with the exception of the top one which contains
        /// all previous information.
        /// The return value is an object array where the first element contains the cumulative clip region and the second the cumulative
        /// translate transform matrix.
        /// WARNING: This method is for internal FX support only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NETCOREAPP3_1_OR_GREATER
        [Obsolete(Obsoletions.GetContextInfoMessage, DiagnosticId = Obsoletions.GetContextInfoDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        [SupportedOSPlatform("windows")]
        public object GetContextInfo()
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: true, out Region? cumulativeClip);
            return new object[] { cumulativeClip ?? new Region(), new Matrix(cumulativeTransform) };
        }

        private void GetContextInfo(out Matrix3x2 cumulativeTransform, bool calculateClip, out Region? cumulativeClip)
        {
            cumulativeClip = calculateClip ? GetRegionIfNotInfinite() : null;   // Current context clip.
            cumulativeTransform = TransformElements;                            // Current context transform.
            Vector2 currentOffset = default;                                    // Offset of current context.
            Vector2 totalOffset = default;                                      // Absolute coordinate offset of top context.

            GraphicsContext? context = _previousContext;

            if (!cumulativeTransform.IsIdentity)
            {
                currentOffset = cumulativeTransform.Translation;
            }

            while (context is not null)
            {
                if (!context.TransformOffset.IsEmpty())
                {
                    cumulativeTransform.Translate(context.TransformOffset);
                }

                if (!currentOffset.IsEmpty())
                {
                    // The location of the GDI+ clip region is relative to the coordinate origin after any translate transform
                    // has been applied. We need to intersect regions using the same coordinate origin relative to the previous
                    // context.

                    // If we don't have a cumulative clip, we're infinite, and translation on infinite regions is a no-op.
                    cumulativeClip?.Translate(currentOffset.X, currentOffset.Y);
                    totalOffset.X += currentOffset.X;
                    totalOffset.Y += currentOffset.Y;
                }

                // Context only stores clips if they are not infinite. Intersecting a clip with an infinite clip is a no-op.
                if (calculateClip && context.Clip is not null)
                {
                    // Intersecting an infinite clip with another is just a copy of the second clip.
                    if (cumulativeClip is null)
                    {
                        cumulativeClip = context.Clip;
                    }
                    else
                    {
                        cumulativeClip.Intersect(context.Clip);
                    }
                }

                currentOffset = context.TransformOffset;

                // Ignore subsequent cumulative contexts.
                do
                {
                    context = context.Previous;

                    if (context == null || !context.Next!.IsCumulative)
                    {
                        break;
                    }
                } while (context.IsCumulative);
            }

            if (!totalOffset.IsEmpty())
            {
                // We need now to reset the total transform in the region so when calling Region.GetHRgn(Graphics)
                // the HRegion is properly offset by GDI+ based on the total offset of the graphics object.

                // If we don't have a cumulative clip, we're infinite, and translation on infinite regions is a no-op.
                cumulativeClip?.Translate(-totalOffset.X, -totalOffset.Y);
            }
        }

#if NETCOREAPP3_1_OR_GREATER
        /// <summary>
        ///  Gets the cumulative offset.
        /// </summary>
        /// <param name="offset">The cumulative offset.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset)
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: false, out _);
            Vector2 translation = cumulativeTransform.Translation;
            offset = new PointF(translation.X, translation.Y);
        }

        /// <summary>
        ///  Gets the cumulative offset and clip region.
        /// </summary>
        /// <param name="offset">The cumulative offset.</param>
        /// <param name="clip">The cumulative clip region or null if the clip region is infinite.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset, out Region? clip)
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: true, out clip);
            Vector2 translation = cumulativeTransform.Translation;
            offset = new PointF(translation.X, translation.Y);
        }
#endif

        public RectangleF VisibleClipBounds
        {
            get
            {
                if (PrintingHelper is PrintPreviewGraphics ppGraphics)
                    return ppGraphics.VisibleClipBounds;

                Gdip.CheckStatus(Gdip.GdipGetVisibleClipBounds(new HandleRef(this, NativeGraphics), out RectangleF rect));

                return rect;
            }
        }

        /// <summary>
        /// Saves the current context into the context stack.
        /// </summary>
        private void PushContext(GraphicsContext context)
        {
            Debug.Assert(context != null && context.State != 0, "GraphicsContext object is null or not valid.");

            if (_previousContext != null)
            {
                // Push context.
                context.Previous = _previousContext;
                _previousContext.Next = context;
            }
            _previousContext = context;
        }

        /// <summary>
        /// Pops all contexts from the specified one included. The specified context is becoming the current context.
        /// </summary>
        private void PopContext(int currentContextState)
        {
            Debug.Assert(_previousContext != null, "Trying to restore a context when the stack is empty");
            GraphicsContext? context = _previousContext;

            // Pop all contexts up the stack.
            while (context != null)
            {
                if (context.State == currentContextState)
                {
                    _previousContext = context.Previous;

                    // This will dipose all context object up the stack.
                    context.Dispose();
                    return;
                }
                context = context.Previous;
            }
            Debug.Fail("Warning: context state not found!");
        }

        public GraphicsState Save()
        {
            GraphicsContext context = new GraphicsContext(this);
            int status = Gdip.GdipSaveGraphics(new HandleRef(this, NativeGraphics), out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            context.IsCumulative = true;
            PushContext(context);

            return new GraphicsState(state);
        }

        public void Restore(GraphicsState gstate)
        {
            Gdip.CheckStatus(Gdip.GdipRestoreGraphics(new HandleRef(this, NativeGraphics), gstate.nativeState));
            PopContext(gstate.nativeState);
        }

        public GraphicsContainer BeginContainer(RectangleF dstrect, RectangleF srcrect, GraphicsUnit unit)
        {
            GraphicsContext context = new GraphicsContext(this);

            int status = Gdip.GdipBeginContainer(
                new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public GraphicsContainer BeginContainer()
        {
            GraphicsContext context = new GraphicsContext(this);
            int status = Gdip.GdipBeginContainer2(new HandleRef(this, NativeGraphics), out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public void EndContainer(GraphicsContainer container)
        {
            ArgumentNullException.ThrowIfNull(container);

            Gdip.CheckStatus(Gdip.GdipEndContainer(new HandleRef(this, NativeGraphics), container.nativeGraphicsContainer));
            PopContext(container.nativeGraphicsContainer);
        }

        public GraphicsContainer BeginContainer(Rectangle dstrect, Rectangle srcrect, GraphicsUnit unit)
        {
            GraphicsContext context = new GraphicsContext(this);

            int status = Gdip.GdipBeginContainerI(
                new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public void AddMetafileComment(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            Gdip.CheckStatus(Gdip.GdipComment(new HandleRef(this, NativeGraphics), data.Length, data));
        }

        public static IntPtr GetHalftonePalette()
        {
            if (s_halftonePalette == IntPtr.Zero)
            {
                lock (s_syncObject)
                {
                    if (s_halftonePalette == IntPtr.Zero)
                    {
                        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                        AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;

                        s_halftonePalette = Gdip.GdipCreateHalftonePalette();
                    }
                }
            }
            return s_halftonePalette;
        }

        // This is called from AppDomain.ProcessExit and AppDomain.DomainUnload.
        private static void OnDomainUnload(object? sender, EventArgs e)
        {
            if (s_halftonePalette != IntPtr.Zero)
            {
                Interop.Gdi32.DeleteObject(s_halftonePalette);
                s_halftonePalette = IntPtr.Zero;
            }
        }

        /// <summary>
        /// GDI+ will return a 'generic error' with specific win32 last error codes when
        /// a terminal server session has been closed, minimized, etc... We don't want
        /// to throw when this happens, so we'll guard against this by looking at the
        /// 'last win32 error code' and checking to see if it is either 1) access denied
        /// or 2) proc not found and then ignore it.
        ///
        /// The problem is that when you lock the machine, the secure desktop is enabled and
        /// rendering fails which is expected (since the app doesn't have permission to draw
        /// on the secure desktop). Not sure if there's anything you can do, short of catching
        /// the desktop switch message and absorbing all the exceptions that get thrown while
        /// it's the secure desktop.
        /// </summary>
        private static void CheckErrorStatus(int status)
        {
            if (status == Gdip.Ok)
                return;

            // Generic error from GDI+ can be GenericError or Win32Error.
            if (status == Gdip.GenericError || status == Gdip.Win32Error)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == SafeNativeMethods.ERROR_ACCESS_DENIED || error == SafeNativeMethods.ERROR_PROC_NOT_FOUND ||
                        // Here, we'll check to see if we are in a terminal services session...
                        (((Interop.User32.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) & 0x00000001) != 0) && (error == 0)))
                {
                    return;
                }
            }

            // Legitimate error, throw our status exception.
            throw Gdip.StatusException(status);
        }
    }
}
