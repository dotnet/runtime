// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    /// <summary>
    /// Represents a Win32 device context.  Provides operations for setting some of the properties of a device context.
    /// It's the managed wrapper for an HDC.
    ///
    /// This class is divided into two files separating the code that needs to be compiled into retail builds and
    /// debugging code.
    /// </summary>
    internal sealed partial class DeviceContext : MarshalByRefObject, IDisposable
    {
        /// <summary>
        /// This class is a wrapper to a Win32 device context, and the Hdc property is the way to get a
        /// handle to it.
        ///
        /// The hDc is released/deleted only when owned by the object, meaning it was created internally;
        /// in this case, the object is responsible for releasing/deleting it.
        /// In the case the object is created from an existing hdc, it is not released; this is consistent
        /// with the Win32 guideline that says if you call GetDC/CreateDC/CreatIC/CreateEnhMetafile, you are
        /// responsible for calling ReleaseDC/DeleteDC/DeleteEnhMetafile respectively.
        ///
        /// This class implements some of the operations commonly performed on the properties of a dc in WinForms,
        /// specially for interacting with GDI+, like clipping and coordinate transformation.
        /// Several properties are not persisted in the dc but instead they are set/reset during a more comprehensive
        /// operation like text rendering or painting; for instance text alignment is set and reset during DrawText (GDI),
        /// DrawString (GDI+).
        ///
        /// Other properties are persisted from operation to operation until they are reset, like clipping,
        /// one can make several calls to Graphics or WindowsGraphics object after setting the dc clip area and
        /// before resetting it; these kinds of properties are the ones implemented in this class.
        /// This kind of properties place an extra challenge in the scenario where a DeviceContext is obtained
        /// from a Graphics object that has been used with GDI+, because GDI+ saves the hdc internally, rendering the
        /// DeviceContext underlying hdc out of sync.  DeviceContext needs to support these kind of properties to
        /// be able to keep the GDI+ and GDI HDCs in sync.
        ///
        /// A few other persisting properties have been implemented in DeviceContext2, among them:
        /// 1. Window origin.
        /// 2. Bounding rectangle.
        /// 3. DC origin.
        /// 4. View port extent.
        /// 5. View port origin.
        /// 6. Window extent
        ///
        /// Other non-persisted properties just for information: Background/Foreground color, Palette, Color adjustment,
        /// Color space, ICM mode and profile, Current pen position, Binary raster op (not supported by GDI+),
        /// Background mode, Logical Pen, DC pen color, ARc direction, Miter limit, Logical brush, DC brush color,
        /// Brush origin, Polygon filling mode, Bitmap stretching mode, Logical font, Intercharacter spacing,
        /// Font mapper flags, Text alignment, Test justification, Layout, Path, Meta region.
        /// See book "Windows Graphics Programming - Feng Yuang", P315 - Device Context Attributes.
        /// </summary>

        private IntPtr _hDC;
        private readonly DeviceContextType _dcType;

        public event EventHandler? Disposing;

        private bool _disposed;

        private IntPtr _hInitialPen;
        private IntPtr _hInitialBrush;
        private IntPtr _hInitialBmp;
        private IntPtr _hInitialFont;

        private IntPtr _hCurrentPen;
        private IntPtr _hCurrentBrush;
        private IntPtr _hCurrentBmp;
        private IntPtr _hCurrentFont;

        private Stack? _contextStack;

#if GDI_FINALIZATION_WATCH
        private string AllocationSite = DbgUtil.StackTrace;
        private string DeAllocationSite = "";
#endif

        /// <summary>
        /// This object's hdc.  If this property is called, then the object will be used as an HDC wrapper, so the hdc
        /// is cached and calls to GetHdc/ReleaseHdc won't PInvoke into GDI. Call Dispose to properly release the hdc.
        /// </summary>
        public IntPtr Hdc => _hDC;

        // Due to a problem with calling DeleteObject() on currently selected GDI objects, we now track the initial set
        // of objects when a DeviceContext is created. Then, we also track which objects are currently selected in the
        // DeviceContext. When a currently selected object is disposed, it is first replaced in the DC and then deleted.
        private void CacheInitialState()
        {
            Debug.Assert(_hDC != IntPtr.Zero, "Cannot get initial state without a valid HDC");
            _hCurrentPen = _hInitialPen = Interop.Gdi32.GetCurrentObject(new HandleRef(this, _hDC), Interop.Gdi32.ObjectType.OBJ_PEN);
            _hCurrentBrush = _hInitialBrush = Interop.Gdi32.GetCurrentObject(new HandleRef(this, _hDC), Interop.Gdi32.ObjectType.OBJ_BRUSH);
            _hCurrentBmp = _hInitialBmp = Interop.Gdi32.GetCurrentObject(new HandleRef(this, _hDC), Interop.Gdi32.ObjectType.OBJ_BITMAP);
            _hCurrentFont = _hInitialFont = Interop.Gdi32.GetCurrentObject(new HandleRef(this, _hDC), Interop.Gdi32.ObjectType.OBJ_FONT);
        }

        /// <summary>
        /// Constructor to construct a DeviceContext object from an existing Win32 device context handle.
        /// </summary>
        private DeviceContext(IntPtr hDC, DeviceContextType dcType)
        {
            _hDC = hDC;
            _dcType = dcType;

            CacheInitialState();
            DeviceContexts.AddDeviceContext(this);

#if TRACK_HDC
            Debug.WriteLine(DbgUtil.StackTraceToStr($"DeviceContext(hDC=0x{(int)hDC:X8}, Type={dcType})"));
#endif
        }

        /// <summary>
        /// CreateDC creates a DeviceContext object wrapping an hdc created with the Win32 CreateDC function.
        /// </summary>
        public static DeviceContext CreateDC(string driverName, string deviceName, string? fileName, IntPtr devMode)
        {
            // Note: All input params can be null but not at the same time.  See MSDN for information.
            IntPtr hdc = Interop.Gdi32.CreateDCW(driverName, deviceName, fileName, devMode);
            return new DeviceContext(hdc, DeviceContextType.NamedDevice);
        }

        /// <summary>
        /// CreateIC creates a DeviceContext object wrapping an hdc created with the Win32 CreateIC function.
        /// </summary>
        public static DeviceContext CreateIC(string driverName, string deviceName, string? fileName, IntPtr devMode)
        {
            // Note: All input params can be null but not at the same time.  See MSDN for information.

            IntPtr hdc = Interop.Gdi32.CreateICW(driverName, deviceName, fileName, devMode);
            return new DeviceContext(hdc, DeviceContextType.Information);
        }

        /// <summary>
        /// Used for wrapping an existing hdc.  In this case, this object doesn't own the hdc so calls to
        /// GetHdc/ReleaseHdc don't PInvoke into GDI.
        /// </summary>
        public static DeviceContext FromHdc(IntPtr hdc)
        {
            Debug.Assert(hdc != IntPtr.Zero, "hdc == 0");
            return new DeviceContext(hdc, DeviceContextType.Unknown);
        }

        ~DeviceContext() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            Disposing?.Invoke(this, EventArgs.Empty);

            _disposed = true;

            switch (_dcType)
            {
                case DeviceContextType.Information:
                case DeviceContextType.NamedDevice:
                    Interop.Gdi32.DeleteDC(new HandleRef(this, _hDC));
                    _hDC = IntPtr.Zero;
                    break;
                case DeviceContextType.Memory:
                    Interop.Gdi32.DeleteDC(new HandleRef(this, _hDC));
                    _hDC = IntPtr.Zero;
                    break;
                // case DeviceContextType.Metafile: - not yet supported.
                case DeviceContextType.Unknown:
                default:
                    return;
                    // do nothing, the hdc is not owned by this object.
                    // in this case it is ok if disposed throught finalization.
            }

            DbgUtil.AssertFinalization(this, disposing);
        }

        /// <summary>
        /// Restores the device context to the specified state. The DC is restored by popping state information off a
        /// stack created by earlier calls to the SaveHdc function.
        /// The stack can contain the state information for several instances of the DC. If the state specified by the
        /// specified parameter is not at the top of the stack, RestoreDC deletes all state information between the top
        /// of the stack and the specified instance.
        /// Specifies the saved state to be restored. If this parameter is positive, nSavedDC represents a specific
        /// instance of the state to be restored. If this parameter is negative, nSavedDC represents an instance relative
        /// to the current state. For example, -1 restores the most recently saved state.
        /// See MSDN for more info.
        /// </summary>
        public void RestoreHdc()
        {
#if TRACK_HDC
            bool result =
#endif
            // Note: Don't use the Hdc property here, it would force handle creation.
            Interop.Gdi32.RestoreDC(new HandleRef(this, _hDC), -1);
#if TRACK_HDC
            // Note: Winforms may call this method during app exit at which point the DC may have been finalized already causing this assert to popup.
            Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("ret[0]=DC.RestoreHdc(hDc=0x{1:x8}, state={2})", result, unchecked((int) _hDC), restoreState) ));
#endif
            Debug.Assert(_contextStack != null, "Someone is calling RestoreHdc() before SaveHdc()");

            if (_contextStack != null)
            {
                GraphicsState g = (GraphicsState)_contextStack.Pop()!;

                _hCurrentBmp = g.hBitmap;
                _hCurrentBrush = g.hBrush;
                _hCurrentPen = g.hPen;
                _hCurrentFont = g.hFont;
            }

#if OPTIMIZED_MEASUREMENTDC
            // in this case, GDI will copy back the previously saved font into the DC.
            // we dont actually know what the font is in our measurement DC so
            // we need to clear it off.
            MeasurementDCInfo.ResetIfIsMeasurementDC(_hDC);
#endif
        }

        /// <summary>
        /// Saves the current state of the device context by copying data describing selected objects and graphic
        /// modes (such as the bitmap, brush, palette, font, pen, region, drawing mode, and mapping mode) to a
        /// context stack.
        /// The SaveDC function can be used any number of times to save any number of instances of the DC state.
        /// A saved state can be restored by using the RestoreHdc method.
        /// See MSDN for more details.
        /// </summary>
        public int SaveHdc()
        {
            HandleRef hdc = new HandleRef(this, _hDC);
            int state = Interop.Gdi32.SaveDC(hdc);

            if (_contextStack == null)
            {
                _contextStack = new Stack();
            }

            GraphicsState g = new GraphicsState();
            g.hBitmap = _hCurrentBmp;
            g.hBrush = _hCurrentBrush;
            g.hPen = _hCurrentPen;
            g.hFont = _hCurrentFont;

            _contextStack.Push(g);

#if TRACK_HDC
            Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("state[0]=DC.SaveHdc(hDc=0x{1:x8})", state, unchecked((int) _hDC)) ));
#endif

            return state;
        }

        /// <summary>
        /// Selects a region as the current clipping region for the device context.
        /// Remarks (From MSDN):
        /// - Only a copy of the selected region is used. The region itself can be selected for any number of other device contexts or it can be deleted.
        /// - The SelectClipRgn function assumes that the coordinates for a region are specified in device units.
        /// - To remove a device-context's clipping region, specify a NULL region handle.
        /// </summary>
        public void SetClip(WindowsRegion region)
        {
            HandleRef hdc = new HandleRef(this, _hDC);
            HandleRef hRegion = new HandleRef(region, region.HRegion);

            Interop.Gdi32.SelectClipRgn(hdc, hRegion);
        }

        ///<summary>
        /// Creates a new clipping region from the intersection of the current clipping region and the specified rectangle.
        ///</summary>
        public void IntersectClip(WindowsRegion wr)
        {
            //if the incoming windowsregion is infinite, there is no need to do any intersecting.
            if (wr.HRegion == IntPtr.Zero)
            {
                return;
            }

            WindowsRegion clip = new WindowsRegion(0, 0, 0, 0);
            try
            {
                int result = Interop.Gdi32.GetClipRgn(new HandleRef(this, _hDC), new HandleRef(clip, clip.HRegion));

                // If the function succeeds and there is a clipping region for the given device context, the return value is 1.
                if (result == 1)
                {
                    Debug.Assert(clip.HRegion != IntPtr.Zero);
                    wr.CombineRegion(clip, wr, Interop.Gdi32.CombineMode.RGN_AND);
                }

                SetClip(wr);
            }
            finally
            {
                clip.Dispose();
            }
        }

        /// <summary>
        ///  Modifies the viewport origin for a device context using the specified horizontal and vertical offsets in
        ///  logical units.
        /// </summary>
        public void TranslateTransform(int dx, int dy)
        {
            Point origin = default;
            Interop.Gdi32.OffsetViewportOrgEx(new HandleRef(this, _hDC), dx, dy, ref origin);
        }

        /// <summary>
        /// </summary>
        public override bool Equals(object? obj)
        {
            DeviceContext? other = obj as DeviceContext;

            if (other == this)
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            // Note: Use property instead of field so the HDC is initialized.  Also, this avoid serialization issues (the obj could be a proxy that does not have access to private fields).
            return other.Hdc == _hDC;
        }

        /// <summary>
        /// This allows collections to treat DeviceContext objects wrapping the same HDC as the same objects.
        /// </summary>
        public override int GetHashCode() => _hDC.GetHashCode();

        internal sealed class GraphicsState
        {
            internal IntPtr hBrush;
            internal IntPtr hFont;
            internal IntPtr hPen;
            internal IntPtr hBitmap;
        }
    }
}
