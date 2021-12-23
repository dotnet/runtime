// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Region : MarshalByRefObject, IDisposable
    {
        private readonly SafeRegionHandle _nativeRegion;

        internal SafeRegionHandle SafeRegionHandle => _nativeRegion;

        public Region()
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegion(out _nativeRegion));
        }

        public Region(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionRect(ref rect, out _nativeRegion));
        }

        public Region(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionRectI(ref rect, out _nativeRegion));
        }

        public Region(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCreateRegionPath(new HandleRef(path, path._nativePath), out _nativeRegion));
        }

        public Region(RegionData rgnData)
        {
            if (rgnData == null)
                throw new ArgumentNullException(nameof(rgnData));

            Gdip.CheckStatus(Gdip.GdipCreateRegionRgnData(
                rgnData.Data,
                rgnData.Data.Length,
                out _nativeRegion));
        }

        internal Region(SafeRegionHandle nativeRegion) => _nativeRegion = nativeRegion;

        public static Region FromHrgn(IntPtr hrgn)
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegionHrgn(hrgn, out SafeRegionHandle region));
            return new Region(region);
        }

        public Region Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneRegion(SafeRegionHandle, out SafeRegionHandle region));
            return new Region(region);
        }

        public void Dispose()
        {
            _nativeRegion.Dispose();
        }

        public void MakeInfinite()
        {
            Gdip.CheckStatus(Gdip.GdipSetInfinite(SafeRegionHandle));
        }

        public void MakeEmpty()
        {
            Gdip.CheckStatus(Gdip.GdipSetEmpty(SafeRegionHandle));
        }

        public void Intersect(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(SafeRegionHandle, ref rect, CombineMode.Intersect));
        }

        public void Intersect(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(SafeRegionHandle, ref rect, CombineMode.Intersect));
        }

        public void Intersect(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(SafeRegionHandle, new HandleRef(path, path._nativePath), CombineMode.Intersect));
        }

        public void Intersect(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(SafeRegionHandle, region.SafeRegionHandle, CombineMode.Intersect));
        }

        public void Union(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(SafeRegionHandle, ref rect, CombineMode.Union));
        }

        public void Union(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(SafeRegionHandle, ref rect, CombineMode.Union));
        }

        public void Union(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(SafeRegionHandle, new HandleRef(path, path._nativePath), CombineMode.Union));
        }

        public void Union(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(SafeRegionHandle, region.SafeRegionHandle, CombineMode.Union));
        }

        public void Xor(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(SafeRegionHandle, ref rect, CombineMode.Xor));
        }

        public void Xor(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(SafeRegionHandle, ref rect, CombineMode.Xor));
        }

        public void Xor(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(SafeRegionHandle, new HandleRef(path, path._nativePath), CombineMode.Xor));
        }

        public void Xor(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(SafeRegionHandle, region.SafeRegionHandle, CombineMode.Xor));
        }

        public void Exclude(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(SafeRegionHandle, ref rect, CombineMode.Exclude));
        }

        public void Exclude(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(SafeRegionHandle, ref rect, CombineMode.Exclude));
        }

        public void Exclude(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(
                SafeRegionHandle,
                new HandleRef(path, path._nativePath),
                CombineMode.Exclude));
        }

        public void Exclude(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(
                SafeRegionHandle,
                region.SafeRegionHandle,
                CombineMode.Exclude));
        }

        public void Complement(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRect(SafeRegionHandle, ref rect, CombineMode.Complement));
        }

        public void Complement(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipCombineRegionRectI(SafeRegionHandle, ref rect, CombineMode.Complement));
        }

        public void Complement(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipCombineRegionPath(SafeRegionHandle, new HandleRef(path, path._nativePath), CombineMode.Complement));
        }

        public void Complement(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipCombineRegionRegion(SafeRegionHandle, region.SafeRegionHandle, CombineMode.Complement));
        }

        public void Translate(float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateRegion(SafeRegionHandle, dx, dy));
        }

        public void Translate(int dx, int dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateRegionI(SafeRegionHandle, dx, dy));
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            Gdip.CheckStatus(Gdip.GdipTransformRegion(
                SafeRegionHandle,
                matrix.SafeMatrixHandle));
        }

        public RectangleF GetBounds(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipGetRegionBounds(SafeRegionHandle, new HandleRef(g, g.NativeGraphics), out RectangleF bounds));
            return bounds;
        }

        public IntPtr GetHrgn(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipGetRegionHRgn(SafeRegionHandle, new HandleRef(g, g.NativeGraphics), out IntPtr hrgn));
            return hrgn;
        }

        public bool IsEmpty(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipIsEmptyRegion(SafeRegionHandle, new HandleRef(g, g.NativeGraphics), out int isEmpty));
            return isEmpty != 0;
        }

        public bool IsInfinite(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipIsInfiniteRegion(SafeRegionHandle, new HandleRef(g, g.NativeGraphics), out int isInfinite));
            return isInfinite != 0;
        }

        public bool Equals(Region region, Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipIsEqualRegion(SafeRegionHandle, region.SafeRegionHandle, new HandleRef(g, g.NativeGraphics), out int isEqual));
            return isEqual != 0;
        }

        public RegionData? GetRegionData()
        {
            Gdip.CheckStatus(Gdip.GdipGetRegionDataSize(SafeRegionHandle, out int regionSize));

            if (regionSize == 0)
                return null;

            byte[] regionData = new byte[regionSize];
            Gdip.CheckStatus(Gdip.GdipGetRegionData(SafeRegionHandle, regionData, regionSize, out regionSize));
            return new RegionData(regionData);
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);

        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics? g) => IsVisible(new PointF(x, y), g);

        public bool IsVisible(PointF point, Graphics? g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionPoint(
                SafeRegionHandle,
                point.X, point.Y,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y, float width, float height) => IsVisible(new RectangleF(x, y, width, height), null);

        public bool IsVisible(RectangleF rect) => IsVisible(rect, null);

        public bool IsVisible(float x, float y, float width, float height, Graphics? g) => IsVisible(new RectangleF(x, y, width, height), g);

        public bool IsVisible(RectangleF rect, Graphics? g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionRect(
                SafeRegionHandle,
                rect.X, rect.Y, rect.Width, rect.Height,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, Graphics? g) => IsVisible(new Point(x, y), g);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(Point point, Graphics? g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionPointI(
                SafeRegionHandle,
                point.X, point.Y,
                new HandleRef(g, g?.NativeGraphics ?? IntPtr.Zero),
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, int width, int height) => IsVisible(new Rectangle(x, y, width, height), null);

        public bool IsVisible(Rectangle rect) => IsVisible(rect, null);

        public bool IsVisible(int x, int y, int width, int height, Graphics? g) => IsVisible(new Rectangle(x, y, width, height), g);

        public bool IsVisible(Rectangle rect, Graphics? g)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRegionRectI(
                SafeRegionHandle,
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
                SafeRegionHandle,
                out int count,
                matrix.SafeMatrixHandle));

            RectangleF[] rectangles = new RectangleF[count];

            // Pinning an empty array gives null, libgdiplus doesn't like this.
            // As invoking isn't necessary, just return the empty array.
            if (count == 0)
                return rectangles;

            fixed (RectangleF* r = rectangles)
            {
                Gdip.CheckStatus(Gdip.GdipGetRegionScans
                    (SafeRegionHandle,
                    r,
                    out count,
                    matrix.SafeMatrixHandle));
            }

            return rectangles;
        }
    }
}
