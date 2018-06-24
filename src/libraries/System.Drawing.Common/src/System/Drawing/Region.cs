// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Globalization;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Region : MarshalByRefObject, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        internal IntPtr NativeRegion { get; private set; }

        public Region()
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegion(out IntPtr region));
            SetNativeRegion(region);
        }

        public Region(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionRect(ref rect, out IntPtr region));
            SetNativeRegion(region);
        }

        public Region(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionRectI(ref rect, out IntPtr region));
            SetNativeRegion(region);
        }

        public Region(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCreateRegionPath(new HandleRef(path, path._nativePath), out IntPtr region));
            SetNativeRegion(region);
        }

        public Region(RegionData rgnData)
        {
            if (rgnData == null)
                throw new ArgumentNullException(nameof(rgnData));

            Gdip.CheckStatus(Gdip.GdipCreateRegionRgnData(
                rgnData.Data,
                rgnData.Data.Length,
                out IntPtr region));

            SetNativeRegion(region);
        }

        internal Region(IntPtr nativeRegion) => SetNativeRegion(nativeRegion);

        public static Region FromHrgn(IntPtr hrgn)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionHrgn(new HandleRef(null, hrgn), out IntPtr region));
            return new Region(region);
        }

        private void SetNativeRegion(IntPtr nativeRegion)
        {
            if (nativeRegion == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativeRegion));

            NativeRegion = nativeRegion;
        }

        public Region Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneRegion(new HandleRef(this, NativeRegion), out IntPtr region));
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
            if (NativeRegion != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    Gdip.GdipDeleteRegion(new HandleRef(this, NativeRegion));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                }
                finally
                {
                    NativeRegion = IntPtr.Zero;
                }
            }
        }

        ~Region() => Dispose(false);

        public void MakeInfinite()
        {
            Gdip.CheckStatus(Gdip.GdipSetInfinite(new HandleRef(this, NativeRegion)));
        }

        public void MakeEmpty()
        {
            Gdip.CheckStatus(Gdip.GdipSetEmpty(new HandleRef(this, NativeRegion)));
        }

        public void Intersect(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(new HandleRef(this, NativeRegion), ref rect, CombineMode.Intersect));
        }

        public void Intersect(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(new HandleRef(this, NativeRegion), ref rect, CombineMode.Intersect));
        }

        public void Intersect(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(new HandleRef(this, NativeRegion), new HandleRef(path, path._nativePath), CombineMode.Intersect));
        }

        public void Intersect(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(new HandleRef(this, NativeRegion), new HandleRef(region, region.NativeRegion), CombineMode.Intersect));
        }

        public void Union(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(new HandleRef(this, NativeRegion), ref rect, CombineMode.Union));
        }

        public void Union(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(new HandleRef(this, NativeRegion), ref rect, CombineMode.Union));
        }

        public void Union(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(new HandleRef(this, NativeRegion), new HandleRef(path, path._nativePath), CombineMode.Union));
        }

        public void Union(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(new HandleRef(this, NativeRegion), new HandleRef(region, region.NativeRegion), CombineMode.Union));
        }

        public void Xor(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(new HandleRef(this, NativeRegion), ref rect, CombineMode.Xor));
        }

        public void Xor(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(new HandleRef(this, NativeRegion), ref rect, CombineMode.Xor));
        }
        
        public void Xor(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(new HandleRef(this, NativeRegion), new HandleRef(path, path._nativePath), CombineMode.Xor));
        }

        public void Xor(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(new HandleRef(this, NativeRegion), new HandleRef(region, region.NativeRegion), CombineMode.Xor));
        }

        public void Exclude(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(new HandleRef(this, NativeRegion), ref rect, CombineMode.Exclude));
        }

        public void Exclude(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(new HandleRef(this, NativeRegion), ref rect, CombineMode.Exclude));
        }

        public void Exclude(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(
                new HandleRef(this, NativeRegion),
                new HandleRef(path, path._nativePath),
                CombineMode.Exclude));
        }

        public void Exclude(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(
                new HandleRef(this, NativeRegion),
                new HandleRef(region, region.NativeRegion),
                CombineMode.Exclude));
        }

        public void Complement(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(new HandleRef(this, NativeRegion), ref rect, CombineMode.Complement));
        }

        public void Complement(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(new HandleRef(this, NativeRegion), ref rect, CombineMode.Complement));
        }

        public void Complement(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(new HandleRef(this, NativeRegion), new HandleRef(path, path._nativePath), CombineMode.Complement));
        }

        public void Complement(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(new HandleRef(this, NativeRegion), new HandleRef(region, region.NativeRegion), CombineMode.Complement));
        }

        public void Translate(float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateRegion(new HandleRef(this, NativeRegion), dx, dy));
        }

        public void Translate(int dx, int dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateRegionI(new HandleRef(this, NativeRegion), dx, dy));
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            Gdip.CheckStatus(Gdip.GdipTransformRegion(
                new HandleRef(this, NativeRegion),
                new HandleRef(matrix, matrix.NativeMatrix)));
        }

        public RectangleF GetBounds(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipGetRegionBounds(new HandleRef(this, NativeRegion), new HandleRef(g, g.NativeGraphics), out RectangleF bounds));
            return bounds;
        }

        public IntPtr GetHrgn(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipGetRegionHRgn(new HandleRef(this, NativeRegion), new HandleRef(g, g.NativeGraphics), out IntPtr hrgn));
            return hrgn;
        }

        public bool IsEmpty(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipIsEmptyRegion(new HandleRef(this, NativeRegion), new HandleRef(g, g.NativeGraphics), out int isEmpty));
            return isEmpty != 0;
        }

        public bool IsInfinite(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipIsInfiniteRegion(new HandleRef(this, NativeRegion), new HandleRef(g, g.NativeGraphics), out int isInfinite));
            return isInfinite != 0;
        }

        public bool Equals(Region region, Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipIsEqualRegion(new HandleRef(this, NativeRegion), new HandleRef(region, region.NativeRegion), new HandleRef(g, g.NativeGraphics), out int isEqual));
            return isEqual != 0;
        }

        public RegionData GetRegionData()
        {
            Gdip.CheckStatus(Gdip.GdipGetRegionDataSize(new HandleRef(this, NativeRegion), out int regionSize));

            if (regionSize == 0)
                return null;

            byte[] regionData = new byte[regionSize];
            Gdip.CheckStatus(Gdip.GdipGetRegionData(new HandleRef(this, NativeRegion), regionData, regionSize, out regionSize));
            return new RegionData(regionData);
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);
        
        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics g) => IsVisible(new PointF(x, y), g);

        public bool IsVisible(PointF point, Graphics g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionPoint(
                new HandleRef(this, NativeRegion),
                point.X, point.Y,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y, float width, float height) => IsVisible(new RectangleF(x, y, width, height), null);

        public bool IsVisible(RectangleF rect) => IsVisible(rect, null);

        public bool IsVisible(float x, float y, float width, float height, Graphics g) => IsVisible(new RectangleF(x, y, width, height), g);

        public bool IsVisible(RectangleF rect, Graphics g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionRect(
                new HandleRef(this, NativeRegion),
                rect.X, rect.Y, rect.Width, rect.Height,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, Graphics g) => IsVisible(new Point(x, y), g);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(Point point, Graphics g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionPointI(
                new HandleRef(this, NativeRegion),
                point.X, point.Y,
                new HandleRef(g,  g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, int width, int height) => IsVisible(new Rectangle(x, y, width, height), null);

        public bool IsVisible(Rectangle rect) => IsVisible(rect, null);

        public bool IsVisible(int x, int y, int width, int height, Graphics g) => IsVisible(new Rectangle(x, y, width, height), g);

        public bool IsVisible(Rectangle rect, Graphics g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionRectI(
                new HandleRef(this, NativeRegion),
                rect.X, rect.Y, rect.Width, rect.Height,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }
 
        public unsafe RectangleF[] GetRegionScans(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            Gdip.CheckStatus(Gdip.GdipGetRegionScansCount(
                new HandleRef(this, NativeRegion),
                out int count,
                new HandleRef(matrix, matrix.NativeMatrix)));

            RectangleF[] rectangles = new RectangleF[count];

            // Pinning an empty array gives null, libgdiplus doesn't like this.
            // As invoking isn't necessary, just return the empty array.
            if (count == 0)
                return rectangles;

            fixed (RectangleF* r = rectangles)
            {
                Gdip.CheckStatus(Gdip.GdipGetRegionScans
                    (new HandleRef(this, NativeRegion),
                    r,
                    out count,
                    new HandleRef(matrix, matrix.NativeMatrix)));
            }

            return rectangles;
        }
    }
}
