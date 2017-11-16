// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Internal;
using System.Globalization;

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsPathIterator : MarshalByRefObject, IDisposable
    {
        public GraphicsPathIterator(GraphicsPath path)
        {
            IntPtr nativeIter = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreatePathIter(out nativeIter, new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

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
                    SafeNativeMethods.Gdip.GdipDeletePathIter(new HandleRef(this, nativeIter));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
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
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpath(new HandleRef(this, nativeIter), out int resultCount,
                        out int tempStart, out int tempEnd, out isClosed);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
            else
            {
                startIndex = tempStart;
                endIndex = tempEnd;
            }

            return resultCount;
        }

        public int NextSubpath(GraphicsPath path, out bool isClosed)
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpathPath(new HandleRef(this, nativeIter), out int resultCount,
                        new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath), out isClosed);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        public int NextPathType(out byte pathType, out int startIndex, out int endIndex)
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterNextPathType(new HandleRef(this, nativeIter), out int resultCount,
                        out pathType, out startIndex, out endIndex);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        public int NextMarker(out int startIndex, out int endIndex)
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarker(new HandleRef(this, nativeIter), out int resultCount,
                        out startIndex, out endIndex);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        public int NextMarker(GraphicsPath path)
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarkerPath(new HandleRef(this, nativeIter), out int resultCount,
                        new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        public int Count
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipPathIterGetCount(new HandleRef(this, nativeIter), out int resultCount);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return resultCount;
            }
        }

        public int SubpathCount
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipPathIterGetSubpathCount(new HandleRef(this, nativeIter), out int resultCount);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return resultCount;
            }
        }

        public bool HasCurve()
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterHasCurve(new HandleRef(this, nativeIter), out bool hasCurve);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return hasCurve;
        }

        public void Rewind()
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterRewind(new HandleRef(this, nativeIter));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public unsafe int Enumerate(ref PointF[] points, ref byte[] types)
        {
            if (points.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0;
            int size = Marshal.SizeOf(typeof(GPPOINTF));
            int count = points.Length;
            byte[] typesLocal = new byte[count];

            IntPtr memoryPts = Marshal.AllocHGlobal(checked(count * size));
            try
            {
                int status = SafeNativeMethods.Gdip.GdipPathIterEnumerate(new HandleRef(this, nativeIter), out resultCount,
                                memoryPts, typesLocal, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                if (resultCount < count)
                {
                    SafeNativeMethods.ZeroMemory((byte*)(checked((long)memoryPts + resultCount * size)), (ulong)((count - resultCount) * size));
                }

                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, count);
                typesLocal.CopyTo(types, 0);
            }
            finally
            {
                Marshal.FreeHGlobal(memoryPts);
            }

            return resultCount;
        }

        public unsafe int CopyData(ref PointF[] points, ref byte[] types, int startIndex, int endIndex)
        {
            if ((points.Length != types.Length) || (endIndex - startIndex + 1 > points.Length))
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0;
            int size = Marshal.SizeOf(typeof(GPPOINTF));
            int count = points.Length;
            byte[] typesLocal = new byte[count];

            IntPtr memoryPts = Marshal.AllocHGlobal(checked(count * size));
            try
            {
                int status = SafeNativeMethods.Gdip.GdipPathIterCopyData(new HandleRef(this, nativeIter), out resultCount,
                                memoryPts, typesLocal, startIndex, endIndex);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                if (resultCount < count)
                {
                    SafeNativeMethods.ZeroMemory((byte*)(checked((long)memoryPts + resultCount * size)), (ulong)((count - resultCount) * size));
                }

                points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPts, count);
                typesLocal.CopyTo(types, 0);
            }
            finally
            {
                Marshal.FreeHGlobal(memoryPts);
            }

            return resultCount;
        }

        // handle to native path iterator object
        internal IntPtr nativeIter;
    }
}
