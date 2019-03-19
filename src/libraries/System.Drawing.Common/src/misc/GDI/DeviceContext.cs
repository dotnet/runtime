// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed partial class DeviceContext : MarshalByRefObject, IDeviceContext, IDisposable
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
        private DeviceContextType _dcType;

        public event EventHandler Disposing;

        private bool _disposed;

        // We cache the hWnd when creating the dc from one, to provide support forIDeviceContext.GetHdc/ReleaseHdc.  
        // This hWnd could be null, in such case it is referring to the screen.
        private IntPtr _hWnd = (IntPtr)(-1); // Unlikely to be a valid hWnd.  

        private IntPtr _hInitialPen;
        private IntPtr _hInitialBrush;
        private IntPtr _hInitialBmp;
        private IntPtr _hInitialFont;

        private IntPtr _hCurrentPen;
        private IntPtr _hCurrentBrush;
        private IntPtr _hCurrentBmp;
        private IntPtr _hCurrentFont;

        private Stack _contextStack;

#if GDI_FINALIZATION_WATCH
        private string AllocationSite = DbgUtil.StackTrace;
        private string DeAllocationSite = "";
#endif

        /// <summary>
        /// This object's hdc.  If this property is called, then the object will be used as an HDC wrapper, so the hdc
        /// is cached and calls to GetHdc/ReleaseHdc won't PInvoke into GDI. Call Dispose to properly release the hdc.
        /// </summary>
        public IntPtr Hdc
        {
            get
            {
                if (_hDC == IntPtr.Zero)
                {
                    if (_dcType == DeviceContextType.Display)
                    {
                        Debug.Assert(!_disposed, "Accessing a disposed DC, forcing recreation of HDC - this will generate a Handle leak!");

                        // Note: ReleaseDC must be called from the same thread. This applies only to HDC obtained
                        // from calling GetDC. This means Display DeviceContext objects should never be finalized.
                        _hDC = ((IDeviceContext)this).GetHdc();  // _hDC will be released on call to Dispose.
                        CacheInitialState();
                    }
#if GDI_FINALIZATION_WATCH
                    else
                    {
                        try { Debug.WriteLine($"Allocation stack:\r\n{AllocationSite}\r\nDeallocation stack:\r\n{DeAllocationSite}"); } catch  {}
                    }
#endif
                }

                Debug.Assert(_hDC != IntPtr.Zero, "Attempt to use deleted HDC - DC type: " + _dcType);

                return _hDC;
            }
        }

        // Due to a problem with calling DeleteObject() on currently selected GDI objects, we now track the initial set
        // of objects when a DeviceContext is created. Then, we also track which objects are currently selected in the
        // DeviceContext. When a currently selected object is disposed, it is first replaced in the DC and then deleted.
        private void CacheInitialState()
        {
            Debug.Assert(_hDC != IntPtr.Zero, "Cannot get initial state without a valid HDC");
            _hCurrentPen = _hInitialPen = IntUnsafeNativeMethods.GetCurrentObject(new HandleRef(this, _hDC), IntNativeMethods.OBJ_PEN);
            _hCurrentBrush = _hInitialBrush = IntUnsafeNativeMethods.GetCurrentObject(new HandleRef(this, _hDC), IntNativeMethods.OBJ_BRUSH);
            _hCurrentBmp = _hInitialBmp = IntUnsafeNativeMethods.GetCurrentObject(new HandleRef(this, _hDC), IntNativeMethods.OBJ_BITMAP);
            _hCurrentFont = _hInitialFont = IntUnsafeNativeMethods.GetCurrentObject(new HandleRef(this, _hDC), IntNativeMethods.OBJ_FONT);
        }


        /// <summary>
        /// Constructor to construct a DeviceContext object from an window handle.
        /// </summary>
        private DeviceContext(IntPtr hWnd)
        {
            _hWnd = hWnd;
            _dcType = DeviceContextType.Display;

            DeviceContexts.AddDeviceContext(this);

            // the hDc will be created on demand.

#if TRACK_HDC
            Debug.WriteLine( DbgUtil.StackTraceToStr(string.Format( "DeviceContext( hWnd=0x{0:x8} )", unchecked((int) hWnd))));
#endif
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

            if (dcType == DeviceContextType.Display)
            {
                _hWnd = IntUnsafeNativeMethods.WindowFromDC(new HandleRef(this, _hDC));
            }
#if TRACK_HDC
            Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("DeviceContext( hDC=0x{0:X8}, Type={1} )", unchecked((int) hDC), dcType) ));
#endif
        }



        /// <summary>
        /// CreateDC creates a DeviceContext object wrapping an hdc created with the Win32 CreateDC function.
        /// </summary>
        public static DeviceContext CreateDC(string driverName, string deviceName, string fileName, HandleRef devMode)
        {
            // Note: All input params can be null but not at the same time.  See MSDN for information.

            IntPtr hdc = IntUnsafeNativeMethods.CreateDC(driverName, deviceName, fileName, devMode);
            return new DeviceContext(hdc, DeviceContextType.NamedDevice);
        }

        /// <summary>
        /// CreateIC creates a DeviceContext object wrapping an hdc created with the Win32 CreateIC function.
        /// </summary>
        public static DeviceContext CreateIC(string driverName, string deviceName, string fileName, HandleRef devMode)
        {
            // Note: All input params can be null but not at the same time.  See MSDN for information.

            IntPtr hdc = IntUnsafeNativeMethods.CreateIC(driverName, deviceName, fileName, devMode);
            return new DeviceContext(hdc, DeviceContextType.Information);
        }

        /// <summary>
        /// Creates a DeviceContext object wrapping a memory DC compatible with the specified device.
        /// </summary>
        public static DeviceContext FromCompatibleDC(IntPtr hdc)
        {
            // If hdc is null, the function creates a memory DC compatible with the application's current screen.
            // Win2K+: (See CreateCompatibleDC in the MSDN).
            // In this case the thread that calls CreateCompatibleDC owns the HDC that is created. When this thread is destroyed, 
            // the HDC is no longer valid.

            IntPtr compatibleDc = IntUnsafeNativeMethods.CreateCompatibleDC(new HandleRef(null, hdc));
            return new DeviceContext(compatibleDc, DeviceContextType.Memory);
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

        /// <summary>
        /// When hwnd is null, we are getting the screen DC.
        /// </summary>
        public static DeviceContext FromHwnd(IntPtr hwnd) => new DeviceContext(hwnd);

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
                case DeviceContextType.Display:
                    Debug.Assert(disposing, "WARNING: Finalizing a Display DeviceContext.\r\nReleaseDC may fail when not called from the same thread GetDC was called from.");

                    ((IDeviceContext)this).ReleaseHdc();
                    break;

                case DeviceContextType.Information:
                case DeviceContextType.NamedDevice:

                    // CreateDC and CreateIC add an HDC handle to the HandleCollector; to remove it properly we need 
                    // to call DeleteHDC.
#if TRACK_HDC
                    Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("DC.DeleteHDC(hdc=0x{0:x8})", unchecked((int) _hDC))));
#endif

                    IntUnsafeNativeMethods.DeleteHDC(new HandleRef(this, _hDC));

                    _hDC = IntPtr.Zero;
                    break;

                case DeviceContextType.Memory:

                    // CreatCompatibleDC adds a GDI handle to HandleCollector, to remove it properly we need to call 
                    // DeleteDC.
#if TRACK_HDC
                    Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("DC.DeleteDC(hdc=0x{0:x8})", unchecked((int) _hDC))));
#endif
                    IntUnsafeNativeMethods.DeleteDC(new HandleRef(this, _hDC));

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
        /// Explicit interface method implementation to hide them a bit for usability reasons so the object is seen as
        /// a wrapper around an hdc that is always available, and for performance reasons since it caches the hdc if
        /// used in this way.
        /// </summary>
        IntPtr IDeviceContext.GetHdc()
        {
            if (_hDC == IntPtr.Zero)
            {
                Debug.Assert(_dcType == DeviceContextType.Display, "Calling GetDC from a non display/window device.");

                // Note: for common DCs, GetDC assigns default attributes to the DC each time it is retrieved. 
                // For example, the default font is System.
                _hDC = IntUnsafeNativeMethods.GetDC(new HandleRef(this, _hWnd));
#if TRACK_HDC
                Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("hdc[0x{0:x8}]=DC.GetHdc(hWnd=0x{1:x8})", unchecked((int) _hDC), unchecked((int) _hWnd))));
#endif            
            }

            return _hDC;
        }


        ///<summary>
        /// If the object was created from a DC, this object doesn't 'own' the dc so we just ignore this call.
        ///</summary>
        void IDeviceContext.ReleaseHdc()
        {
            if (_hDC != IntPtr.Zero && _dcType == DeviceContextType.Display)
            {
#if TRACK_HDC
                int retVal = 
#endif
                IntUnsafeNativeMethods.ReleaseDC(new HandleRef(this, _hWnd), new HandleRef(this, _hDC));
                // Note: retVal == 0 means it was not released but doesn't necessarily means an error; class or private DCs are never released.
#if TRACK_HDC
                Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("[ret={0}]=DC.ReleaseDC(hDc=0x{1:x8}, hWnd=0x{2:x8})", retVal, unchecked((int) _hDC), unchecked((int) _hWnd))));
#endif                 
                _hDC = IntPtr.Zero;
            }
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
            IntUnsafeNativeMethods.RestoreDC(new HandleRef(this, _hDC), -1);
#if TRACK_HDC
            // Note: Winforms may call this method during app exit at which point the DC may have been finalized already causing this assert to popup.
            Debug.WriteLine( DbgUtil.StackTraceToStr( string.Format("ret[0]=DC.RestoreHdc(hDc=0x{1:x8}, state={2})", result, unchecked((int) _hDC), restoreState) ));
#endif 
            Debug.Assert(_contextStack != null, "Someone is calling RestoreHdc() before SaveHdc()");

            if (_contextStack != null)
            {
                GraphicsState g = (GraphicsState)_contextStack.Pop();

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
            int state = IntUnsafeNativeMethods.SaveDC(hdc);

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

            IntUnsafeNativeMethods.SelectClipRgn(hdc, hRegion);
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
                int result = IntUnsafeNativeMethods.GetClipRgn(new HandleRef(this, _hDC), new HandleRef(clip, clip.HRegion));

                // If the function succeeds and there is a clipping region for the given device context, the return value is 1. 
                if (result == 1)
                {
                    Debug.Assert(clip.HRegion != IntPtr.Zero);
                    wr.CombineRegion(clip, wr, RegionCombineMode.AND); //1 = AND (or Intersect)
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
            IntNativeMethods.POINT orgn = new IntNativeMethods.POINT();
            IntUnsafeNativeMethods.OffsetViewportOrgEx(new HandleRef(this, _hDC), dx, dy, orgn);
        }

        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            DeviceContext other = obj as DeviceContext;

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

        internal class GraphicsState
        {
            internal IntPtr hBrush;
            internal IntPtr hFont;
            internal IntPtr hPen;
            internal IntPtr hBitmap;
        }
    }
}
