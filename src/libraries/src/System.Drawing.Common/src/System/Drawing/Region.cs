// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public sealed partial class Region : MarshalByRefObject, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        public Region()
        {
            IntPtr region = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateRegion(out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeRegion(region);
        }

        public Region(RectangleF rect)
        {
            IntPtr region = IntPtr.Zero;
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCreateRegionRect(ref gprectf, out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeRegion(region);
        }

        public Region(Rectangle rect)
        {
            IntPtr region = IntPtr.Zero;
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCreateRegionRectI(ref gprect, out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeRegion(region);
        }

        public Region(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            IntPtr region = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateRegionPath(new HandleRef(path, path.nativePath), out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeRegion(region);
        }

        public Region(RegionData rgnData)
        {
            if (rgnData == null)
            {
                throw new ArgumentNullException(nameof(rgnData));
            }

            IntPtr region = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateRegionRgnData(rgnData.Data,
                                                         rgnData.Data.Length,
                                                         out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeRegion(region);
        }

        internal Region(IntPtr nativeRegion) => SetNativeRegion(nativeRegion);

        public static Region FromHrgn(IntPtr hrgn)
        {
            IntPtr region = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateRegionHrgn(new HandleRef(null, hrgn), out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Region(region);
        }

        private void SetNativeRegion(IntPtr nativeRegion)
        {
            if (nativeRegion == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(nativeRegion));
            }

            this._nativeRegion = nativeRegion;
        }

        public Region Clone()
        {
            IntPtr region = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneRegion(new HandleRef(this, _nativeRegion), out region);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Region(region);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
#if FINALIZATION_WATCH
            if (!disposing && nativeRegion != IntPtr.Zero)
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif
            if (_nativeRegion != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteRegion(new HandleRef(this, _nativeRegion));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                }
                finally
                {
                    _nativeRegion = IntPtr.Zero;
                }
            }
        }

        ~Region() => Dispose(false);

        public void MakeInfinite()
        {
            int status = SafeNativeMethods.Gdip.GdipSetInfinite(new HandleRef(this, _nativeRegion));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void MakeEmpty()
        {
            int status = SafeNativeMethods.Gdip.GdipSetEmpty(new HandleRef(this, _nativeRegion));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(RectangleF rect)
        {
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRect(new HandleRef(this, _nativeRegion), ref gprectf, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(Rectangle rect)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(new HandleRef(this, _nativeRegion), ref gprect, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionPath(new HandleRef(this, _nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion), CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(RectangleF rect)
        {
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRect(new HandleRef(this, _nativeRegion), ref gprectf, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(Rectangle rect)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(new HandleRef(this, _nativeRegion), ref gprect, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionPath(new HandleRef(this, _nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion), CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(RectangleF rect)
        {
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRect(new HandleRef(this, _nativeRegion), ref gprectf, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(Rectangle rect)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(new HandleRef(this, _nativeRegion), ref gprect, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
        
        public void Xor(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionPath(new HandleRef(this, _nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion), CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(RectangleF rect)
        {
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRect(new HandleRef(this, _nativeRegion), ref gprectf, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(Rectangle rect)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(new HandleRef(this, _nativeRegion), ref gprect, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionPath(new HandleRef(this, _nativeRegion), new HandleRef(path, path.nativePath),
                                                       CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion),
                                                         CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(RectangleF rect)
        {
            var gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRect(new HandleRef(this, _nativeRegion), ref gprectf, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(Rectangle rect)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(new HandleRef(this, _nativeRegion), ref gprect, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionPath(new HandleRef(this, _nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion), CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Translate(float dx, float dy)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateRegion(new HandleRef(this, _nativeRegion), dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Translate(int dx, int dy)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateRegionI(new HandleRef(this, _nativeRegion), dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            int status = SafeNativeMethods.Gdip.GdipTransformRegion(new HandleRef(this, _nativeRegion),
                                                     new HandleRef(matrix, matrix.nativeMatrix));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public RectangleF GetBounds(Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            var gprectf = new GPRECTF();
            int status = SafeNativeMethods.Gdip.GdipGetRegionBounds(new HandleRef(this, _nativeRegion), new HandleRef(g, g.NativeGraphics), ref gprectf);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return gprectf.ToRectangleF();
        }

        public IntPtr GetHrgn(Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            IntPtr hrgn = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipGetRegionHRgn(new HandleRef(this, _nativeRegion), new HandleRef(g, g.NativeGraphics), out hrgn);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return hrgn;
        }

        public bool IsEmpty(Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            int isEmpty;
            int status = SafeNativeMethods.Gdip.GdipIsEmptyRegion(new HandleRef(this, _nativeRegion), new HandleRef(g, g.NativeGraphics), out isEmpty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isEmpty != 0;
        }

        public bool IsInfinite(Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            int isInfinite;
            int status = SafeNativeMethods.Gdip.GdipIsInfiniteRegion(new HandleRef(this, _nativeRegion), new HandleRef(g, g.NativeGraphics), out isInfinite);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isInfinite != 0;
        }

        public bool Equals(Region region, Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int isEqual;
            int status = SafeNativeMethods.Gdip.GdipIsEqualRegion(new HandleRef(this, _nativeRegion), new HandleRef(region, region._nativeRegion), new HandleRef(g, g.NativeGraphics), out isEqual);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isEqual != 0;
        }

        public RegionData GetRegionData()
        {
            int regionSize = 0;
            int status = SafeNativeMethods.Gdip.GdipGetRegionDataSize(new HandleRef(this, _nativeRegion), out regionSize);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (regionSize == 0)
            {
                return null;
            }

            byte[] regionData = new byte[regionSize];
            status = SafeNativeMethods.Gdip.GdipGetRegionData(new HandleRef(this, _nativeRegion), regionData, regionSize, out regionSize);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new RegionData(regionData);
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);
        
        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics g) => IsVisible(new PointF(x, y), g);

        public bool IsVisible(PointF point, Graphics g)
        {
            int isVisible;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPoint(new HandleRef(this, _nativeRegion), point.X, point.Y,
                                                          new HandleRef(g, (g == null) ? IntPtr.Zero : g.NativeGraphics),
                                                          out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y, float width, float height) => IsVisible(new RectangleF(x, y, width, height), null);

        public bool IsVisible(RectangleF rect) => IsVisible(rect, null);

        public bool IsVisible(float x, float y, float width, float height, Graphics g) => IsVisible(new RectangleF(x, y, width, height), g);

        public bool IsVisible(RectangleF rect, Graphics g)
        {
            int isVisible = 0;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRect(new HandleRef(this, _nativeRegion), rect.X, rect.Y,
                                                         rect.Width, rect.Height,
                                                         new HandleRef(g, (g == null) ? IntPtr.Zero : g.NativeGraphics),
                                                         out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, Graphics g) => IsVisible(new Point(x, y), g);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(Point point, Graphics g)
        {
            int isVisible = 0;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPointI(new HandleRef(this, _nativeRegion), point.X, point.Y,
                                                           new HandleRef(g, (g == null) ? IntPtr.Zero : g.NativeGraphics),
                                                           out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, int width, int height) => IsVisible(new Rectangle(x, y, width, height), null);

        public bool IsVisible(Rectangle rect) => IsVisible(rect, null);

        public bool IsVisible(int x, int y, int width, int height, Graphics g) => IsVisible(new Rectangle(x, y, width, height), g);

        public bool IsVisible(Rectangle rect, Graphics g)
        {
            int isVisible = 0;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRectI(new HandleRef(this, _nativeRegion), rect.X, rect.Y,
                                                          rect.Width, rect.Height,
                                                          new HandleRef(g, (g == null) ? IntPtr.Zero : g.NativeGraphics),
                                                          out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }
 
        public RectangleF[] GetRegionScans(Matrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            int count = 0;
            int status = SafeNativeMethods.Gdip.GdipGetRegionScansCount(new HandleRef(this, _nativeRegion),
                                                         out count,
                                                         new HandleRef(matrix, matrix.nativeMatrix));
            SafeNativeMethods.Gdip.CheckStatus(status);

            int rectsize = (int)Marshal.SizeOf(typeof(GPRECTF));
            IntPtr memoryRects = Marshal.AllocHGlobal(checked(rectsize * count));

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetRegionScans(new HandleRef(this, _nativeRegion),
                    memoryRects,
                    out count,
                    new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                var gprectf = new GPRECTF();

                var rectangles = new RectangleF[count];
                for (int index = 0; index < count; index++)
                {
                    gprectf = (GPRECTF)Marshal.PtrToStructure((IntPtr)(checked((long)memoryRects + rectsize * index)), typeof(GPRECTF));
                    rectangles[index] = gprectf.ToRectangleF();
                }

                return rectangles;
            }
            finally
            {
                Marshal.FreeHGlobal(memoryRects);
            }
        }

        internal IntPtr _nativeRegion;
    }
}
