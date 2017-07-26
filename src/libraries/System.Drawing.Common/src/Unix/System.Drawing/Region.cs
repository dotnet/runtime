// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Region.cs
//
// Author:
//    Miguel de Icaza (miguel@ximian.com)
//      Jordi Mas i Hernandez (jordi@ximian.com)
//
// Copyright (C) 2003 Ximian, Inc. http://www.ximian.com
// Copyright (C) 2004,2006 Novell, Inc. http://www.novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public sealed class Region : MarshalByRefObject, IDisposable
    {
        private IntPtr nativeRegion = IntPtr.Zero;

        public Region()
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateRegion(out nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        internal Region(IntPtr native)
        {
            nativeRegion = native;
        }

        public Region(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCreateRegionPath(path.nativePath, out nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Region(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateRegionRectI(ref rect, out nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Region(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateRegionRect(ref rect, out nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Region(RegionData rgnData)
        {
            if (rgnData == null)
                throw new ArgumentNullException("rgnData");
            // a NullReferenceException can be throw for rgnData.Data.Length (if rgnData.Data is null) just like MS
            if (rgnData.Data.Length == 0)
                throw new ArgumentException("rgnData");
            Status status = SafeNativeMethods.Gdip.GdipCreateRegionRgnData(rgnData.Data, rgnData.Data.Length, out nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //                                                                                                     
        // Union
        //

        public void Union(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionPath(nativeRegion, path.nativePath, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void Union(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(nativeRegion, ref rect, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRect(nativeRegion, ref rect, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Union(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(nativeRegion, region.NativeObject, CombineMode.Union);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        //
        // Intersect
        //
        public void Intersect(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionPath(nativeRegion, path.nativePath, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(nativeRegion, ref rect, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRect(nativeRegion, ref rect, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Intersect(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(nativeRegion, region.NativeObject, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // Complement
        //
        public void Complement(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionPath(nativeRegion, path.nativePath, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(nativeRegion, ref rect, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRect(nativeRegion, ref rect, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Complement(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(nativeRegion, region.NativeObject, CombineMode.Complement);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // Exclude
        //
        public void Exclude(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionPath(nativeRegion, path.nativePath, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(nativeRegion, ref rect, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRect(nativeRegion, ref rect, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Exclude(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(nativeRegion, region.NativeObject, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // Xor
        //
        public void Xor(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionPath(nativeRegion, path.nativePath, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRectI(nativeRegion, ref rect, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRect(nativeRegion, ref rect, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Xor(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipCombineRegionRegion(nativeRegion, region.NativeObject, CombineMode.Xor);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // GetBounds
        //
        public RectangleF GetBounds(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException("g");

            RectangleF rect = new Rectangle();

            Status status = SafeNativeMethods.Gdip.GdipGetRegionBounds(nativeRegion, g.NativeObject, ref rect);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return rect;
        }

        //
        // Translate
        //
        public void Translate(int dx, int dy)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateRegionI(nativeRegion, dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Translate(float dx, float dy)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateRegion(nativeRegion, dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // IsVisible
        //
        public bool IsVisible(int x, int y, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPointI(nativeRegion, x, y, ptr, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRectI(nativeRegion, x, y,
                    width, height, IntPtr.Zero, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(int x, int y, int width, int height, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRectI(nativeRegion, x, y,
                    width, height, ptr, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(Point point)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPointI(nativeRegion, point.X, point.Y,
                            IntPtr.Zero, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(PointF point)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPoint(nativeRegion, point.X, point.Y,
                            IntPtr.Zero, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(Point point, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPointI(nativeRegion, point.X, point.Y,
                            ptr, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(PointF point, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPoint(nativeRegion, point.X, point.Y,
                            ptr, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(Rectangle rect)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRectI(nativeRegion, rect.X, rect.Y,
                    rect.Width, rect.Height, IntPtr.Zero, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(RectangleF rect)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRect(nativeRegion, rect.X, rect.Y,
                    rect.Width, rect.Height, IntPtr.Zero, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(Rectangle rect, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRectI(nativeRegion, rect.X, rect.Y,
                    rect.Width, rect.Height, ptr, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(RectangleF rect, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRect(nativeRegion, rect.X, rect.Y,
                    rect.Width, rect.Height, ptr, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(float x, float y)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPoint(nativeRegion, x, y, IntPtr.Zero, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(float x, float y, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionPoint(nativeRegion, x, y, ptr, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRect(nativeRegion, x, y, width, height, IntPtr.Zero, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsVisible(float x, float y, float width, float height, Graphics g)
        {
            IntPtr ptr = (g == null) ? IntPtr.Zero : g.NativeObject;
            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRegionRect(nativeRegion, x, y, width, height, ptr, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }


        //
        // Miscellaneous
        //

        public bool IsEmpty(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException("g");

            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsEmptyRegion(nativeRegion, g.NativeObject, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public bool IsInfinite(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException("g");

            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsInfiniteRegion(nativeRegion, g.NativeObject, out result);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public void MakeEmpty()
        {
            Status status = SafeNativeMethods.Gdip.GdipSetEmpty(nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void MakeInfinite()
        {
            Status status = SafeNativeMethods.Gdip.GdipSetInfinite(nativeRegion);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public bool Equals(Region region, Graphics g)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            if (g == null)
                throw new ArgumentNullException("g");

            bool result;

            Status status = SafeNativeMethods.Gdip.GdipIsEqualRegion(nativeRegion, region.NativeObject,
                           g.NativeObject, out result);

            SafeNativeMethods.Gdip.CheckStatus(status);

            return result;
        }

        public static Region FromHrgn(IntPtr hrgn)
        {
            if (hrgn == IntPtr.Zero)
                throw new ArgumentException("hrgn");

            IntPtr handle;
            Status status = SafeNativeMethods.Gdip.GdipCreateRegionHrgn(hrgn, out handle);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Region(handle);
        }


        public IntPtr GetHrgn(Graphics g)
        {
            // Our WindowsForms implementation uses null to avoid
            // creating a Graphics context when not needed
#if false
            // this is MS behaviour
            if (g == null)
                throw new ArgumentNullException ("g");
#else
            // this is an hack for MWF (libgdiplus would reject that)
            if (g == null)
                return nativeRegion;
#endif
            IntPtr handle = IntPtr.Zero;
            Status status = SafeNativeMethods.Gdip.GdipGetRegionHRgn(nativeRegion, g.NativeObject, ref handle);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return handle;
        }


        public RegionData GetRegionData()
        {
            int size, filled;

            Status status = SafeNativeMethods.Gdip.GdipGetRegionDataSize(nativeRegion, out size);
            SafeNativeMethods.Gdip.CheckStatus(status);

            byte[] buff = new byte[size];

            status = SafeNativeMethods.Gdip.GdipGetRegionData(nativeRegion, buff, size, out filled);
            SafeNativeMethods.Gdip.CheckStatus(status);

            RegionData rgndata = new RegionData(buff);

            return rgndata;
        }


        public RectangleF[] GetRegionScans(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            int cnt;

            Status status = SafeNativeMethods.Gdip.GdipGetRegionScansCount(nativeRegion, out cnt, matrix.NativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (cnt == 0)
                return new RectangleF[0];

            RectangleF[] rects = new RectangleF[cnt];
            int size = Marshal.SizeOf(rects[0]);

            IntPtr dest = Marshal.AllocHGlobal(size * cnt);
            try
            {
                status = SafeNativeMethods.Gdip.GdipGetRegionScans(nativeRegion, dest, out cnt, matrix.NativeObject);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
            finally
            {
                // note: Marshal.FreeHGlobal is called from GDIPlus.FromUnManagedMemoryToRectangles
                GDIPlus.FromUnManagedMemoryToRectangles(dest, rects);
            }
            return rects;
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            Status status = SafeNativeMethods.Gdip.GdipTransformRegion(nativeRegion, matrix.NativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Region Clone()
        {
            IntPtr cloned;

            Status status = SafeNativeMethods.Gdip.GdipCloneRegion(nativeRegion, out cloned);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Region(cloned);
        }

        public void Dispose()
        {
            DisposeHandle();
            System.GC.SuppressFinalize(this);
        }

        private void DisposeHandle()
        {
            if (nativeRegion != IntPtr.Zero)
            {
                SafeNativeMethods.Gdip.GdipDeleteRegion(nativeRegion);
                nativeRegion = IntPtr.Zero;
            }
        }

        ~Region()
        {
            DisposeHandle();
        }

        internal IntPtr NativeObject
        {
            get
            {
                return nativeRegion;
            }
            set
            {
                nativeRegion = value;
            }
        }
        // why is this a instance method ? and not static ?
        public void ReleaseHrgn(IntPtr regionHandle)
        {
            if (regionHandle == IntPtr.Zero)
                throw new ArgumentNullException("regionHandle");

            Status status = Status.Ok;
            if (GDIPlus.RunningOnUnix())
            {
                // for libgdiplus HRGN == GpRegion* 
                status = SafeNativeMethods.Gdip.GdipDeleteRegion(regionHandle);
            }
            else
            {
                // ... but on Windows HRGN are (old) GDI objects
                if (!GDIPlus.DeleteObject(regionHandle))
                    status = Status.InvalidParameter;
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}
