// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// THIS PARTIAL CLASS CONTAINS THE BASE METHODS FOR CREATING AND DISPOSING A WINDOWSGRAPHICS AS WELL
// GETTING, DISPOSING AND WORKING WITH A DC.

using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace System.Drawing.Internal
{
    /// <summary>
    /// WindowsGraphics is a library for rendering text and drawing using GDI; it was created to address performance
    /// and compatibility issues found in GDI+ Graphics class.
    ///
    /// Note: WindowsGraphics is a stateful component, DC properties are persisted from method calls, as opposed to
    /// Graphics (GDI+) which performs atomic operations and always restores the hdc. The underlying hdc is always
    /// saved and restored on dispose so external HDCs won't be modified by WindowsGraphics. So we don't need to
    /// restore previous objects into the dc in method calls.
    ///</summary>
    internal sealed partial class WindowsGraphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        // Wrapper around the window dc this object refers to.
        // Note: this dc is only disposed when owned (created) by the WindowsGraphics.
        private DeviceContext _dc;
        private bool _disposeDc;
        private Graphics? _graphics; // cached when initialized FromGraphics to be able to call g.ReleaseHdc from Dispose.

#if GDI_FINALIZATION_WATCH
        private string AllocationSite = DbgUtil.StackTrace;
#endif

        public WindowsGraphics(DeviceContext dc)
        {
            Debug.Assert(dc != null, "null dc!");
            _dc = dc;
            _dc.SaveHdc();
        }

        public static WindowsGraphics FromHdc(IntPtr hDc)
        {
            Debug.Assert(hDc != IntPtr.Zero, "null hDc");
            DeviceContext dc = DeviceContext.FromHdc(hDc);

            // we create it, we dispose it.
            return new WindowsGraphics(dc)
            {
                _disposeDc = true
            };
        }

        public static WindowsGraphics FromGraphics(Graphics g, ApplyGraphicsProperties properties)
        {
            Debug.Assert(g != null, "null Graphics object.");

            WindowsRegion? wr = null;

            PointF offset = default;

            if (properties != ApplyGraphicsProperties.None)
            {
                Region? clip = null;

                if (properties.HasFlag(ApplyGraphicsProperties.Clipping))
                {
                    g.GetContextInfo(out offset, out clip);
                }
                else
                {
                    g.GetContextInfo(out offset);
                }

                if (clip is not null)
                {
                    // We have to create the WindowsRegion and dipose the Region object before locking the Graphics object,
                    // in case of an unlikely exception before releasing the WindowsRegion, the finalizer will do it for us.
                    // (no try-finally block since this method is used frequently - perf).

                    // If clipping has not been set (Region.IsInfinite) GetContextInfo will return a null Region.

                    wr = WindowsRegion.FromRegion(clip, g); // WindowsRegion will take ownership of the hRegion.
                    clip.Dispose(); // Disposing the Region object doesn't destroy the hRegion.
                }
            }

            WindowsGraphics wg = FromHdc(g.GetHdc()); // This locks the Graphics object.
            wg._graphics = g;

            // Apply transform and clip
            if (wr is not null)
            {
                using (wr)
                {
                    // If the Graphics object was created from a native DC the actual clipping region is the intersection
                    // beteween the original DC clip region and the GDI+ one - for display Graphics it is the same as
                    // Graphics.VisibleClipBounds.
                    wg.DeviceContext.IntersectClip(wr);
                }
            }

            if (offset != default)
            {
                // elements (XFORM) = [eM11, eM12, eM21, eM22, eDx, eDy], eDx/eDy specify the translation offset.
                wg.DeviceContext.TranslateTransform((int)offset.X, (int)offset.Y);
            }

            return wg;
        }

        ~WindowsGraphics() => Dispose(false);

        public DeviceContext DeviceContext => _dc;

        // Okay to suppress.
        // "WindowsGraphics object does not own the Graphics object.  For instance in a control's Paint event we pass
        // the GraphicsContainer object to TextRenderer, which uses WindowsGraphics; if the Graphics object is disposed
        // then further painting will be broken."
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
        {
            if (_dc != null)
            {
                DbgUtil.AssertFinalization(this, disposing);

                try
                {
                    // Restore original dc.
                    _dc.RestoreHdc();

                    if (_disposeDc)
                    {
                        _dc.Dispose(disposing);
                    }

                    if (_graphics != null)    // if created from a Graphics object...
                    {
                        _graphics.ReleaseHdcInternal(_dc.Hdc);
                        _graphics = null;
                    }
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                    Debug.Fail("Exception thrown during disposing: \r\n" + ex.ToString());
                }
                finally
                {
                    _dc = null!;
                }
            }
        }

        public IntPtr GetHdc() => _dc.Hdc;

        public void ReleaseHdc() => _dc.Dispose();
    }
}
