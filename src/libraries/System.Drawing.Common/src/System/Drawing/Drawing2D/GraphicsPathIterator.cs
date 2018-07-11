// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsPathIterator : MarshalByRefObject, IDisposable
    {
        public GraphicsPathIterator(GraphicsPath path)
        {
            IntPtr nativeIter = IntPtr.Zero;
            int status = Gdip.GdipCreatePathIter(out nativeIter, new HandleRef(path, (path == null) ? IntPtr.Zero : path._nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            this.nativeIter = nativeIter;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (nativeIter != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    Gdip.GdipDeletePathIter(new HandleRef(this, nativeIter));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
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
                    nativeIter = IntPtr.Zero;
                }
            }
        }

        ~GraphicsPathIterator() => Dispose(false);

        public int NextSubpath(out int startIndex, out int endIndex, out bool isClosed)
        {
            int status = Gdip.GdipPathIterNextSubpath(new HandleRef(this, nativeIter), out int resultCount,
                        out int tempStart, out int tempEnd, out isClosed);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
            else
            {
                startIndex = tempStart;
                endIndex = tempEnd;
            }

            return resultCount;
        }

        public int NextSubpath(GraphicsPath path, out bool isClosed)
        {
            int status = Gdip.GdipPathIterNextSubpathPath(new HandleRef(this, nativeIter), out int resultCount,
                        new HandleRef(path, (path == null) ? IntPtr.Zero : path._nativePath), out isClosed);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return resultCount;
        }

        public int NextPathType(out byte pathType, out int startIndex, out int endIndex)
        {
            int status = Gdip.GdipPathIterNextPathType(new HandleRef(this, nativeIter), out int resultCount,
                        out pathType, out startIndex, out endIndex);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return resultCount;
        }

        public int NextMarker(out int startIndex, out int endIndex)
        {
            int status = Gdip.GdipPathIterNextMarker(new HandleRef(this, nativeIter), out int resultCount,
                        out startIndex, out endIndex);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return resultCount;
        }

        public int NextMarker(GraphicsPath path)
        {
            int status = Gdip.GdipPathIterNextMarkerPath(new HandleRef(this, nativeIter), out int resultCount,
                        new HandleRef(path, (path == null) ? IntPtr.Zero : path._nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return resultCount;
        }

        public int Count
        {
            get
            {
                int status = Gdip.GdipPathIterGetCount(new HandleRef(this, nativeIter), out int resultCount);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return resultCount;
            }
        }

        public int SubpathCount
        {
            get
            {
                int status = Gdip.GdipPathIterGetSubpathCount(new HandleRef(this, nativeIter), out int resultCount);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return resultCount;
            }
        }

        public bool HasCurve()
        {
            int status = Gdip.GdipPathIterHasCurve(new HandleRef(this, nativeIter), out bool hasCurve);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return hasCurve;
        }

        public void Rewind()
        {
            int status = Gdip.GdipPathIterRewind(new HandleRef(this, nativeIter));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public unsafe int Enumerate(ref PointF[] points, ref byte[] types)
        {
            if (points.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            if (points.Length == 0)
                return 0;

            fixed (PointF* p = points)
            fixed (byte* t = types)
            {
                int status = Gdip.GdipPathIterEnumerate(
                    new HandleRef(this, nativeIter),
                    out int resultCount,
                    p,
                    t,
                    points.Length);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }

                return resultCount;
            }
        }

        public unsafe int CopyData(ref PointF[] points, ref byte[] types, int startIndex, int endIndex)
        {
            if ((points.Length != types.Length) || (endIndex - startIndex + 1 > points.Length))
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (PointF* p = points)
            fixed (byte* t = types)
            {
                int status = Gdip.GdipPathIterCopyData(
                    new HandleRef(this, nativeIter),
                    out int resultCount,
                    p,
                    t,
                    startIndex,
                    endIndex);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }

                return resultCount;
            }
        }

        // handle to native path iterator object
        internal IntPtr nativeIter;
    }
}
