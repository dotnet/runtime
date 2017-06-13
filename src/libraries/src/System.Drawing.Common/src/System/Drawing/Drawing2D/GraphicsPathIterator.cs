// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Drawing.Internal;
    using System.Globalization;

    /**
     * Represent a Path Iterator object
     */
    /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Provides helper functions for the <see cref='System.Drawing.Drawing2D.GraphicsPath'/> class.
    ///    </para>
    /// </devdoc>
    public sealed class GraphicsPathIterator : MarshalByRefObject, IDisposable
    {
        /**
         * Create a new path iterator object
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.GraphicsPathIterator"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/> class with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        /// </devdoc>
        public GraphicsPathIterator(GraphicsPath path)
        {
            IntPtr nativeIter = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreatePathIter(out nativeIter, new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            this.nativeIter = nativeIter;
        }

        /**
         * Dispose of resources associated with the
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Dispose"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>.
        /// </devdoc>
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

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.GraphicsPathIterator'/>.
        /// </devdoc>
        ~GraphicsPathIterator()
        {
            Dispose(false);
        }

        /**
         * Next subpath in path
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath"]/*' />
        /// <devdoc>
        ///    Returns the number of subpaths in the
        /// <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. The start index and end index of the 
        ///    next subpath are contained in out parameters.
        /// </devdoc>
        public int NextSubpath(out int startIndex, out int endIndex, out bool isClosed)
        {
            int resultCount = 0;
            int tempStart = 0;
            int tempEnd = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpath(new HandleRef(this, nativeIter), out resultCount,
                                    out tempStart, out tempEnd, out isClosed);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
            else
            {
                startIndex = tempStart;
                endIndex = tempEnd;
            }

            return resultCount;
        }

        /**
         * Next subpath in path
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextSubpath1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int NextSubpath(GraphicsPath path, out bool isClosed)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextSubpathPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath), out isClosed);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        /**
         * Next type in subpath
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextPathType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int NextPathType(out byte pathType, out int startIndex, out int endIndex)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextPathType(new HandleRef(this, nativeIter), out resultCount,
                                    out pathType, out startIndex, out endIndex);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        /**
         * Next marker in subpath
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int NextMarker(out int startIndex, out int endIndex)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarker(new HandleRef(this, nativeIter), out resultCount,
                                    out startIndex, out endIndex);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        /**
         * Next marker in subpath
         */
        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.NextMarker1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int NextMarker(GraphicsPath path)
        {
            int resultCount = 0;
            int status = SafeNativeMethods.Gdip.GdipPathIterNextMarkerPath(new HandleRef(this, nativeIter), out resultCount,
                                    new HandleRef(path, (path == null) ? IntPtr.Zero : path.nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return resultCount;
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Count"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get
            {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetCount(new HandleRef(this, nativeIter), out resultCount);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return resultCount;
            }
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.SubpathCount"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int SubpathCount
        {
            get
            {
                int resultCount = 0;
                int status = SafeNativeMethods.Gdip.GdipPathIterGetSubpathCount(new HandleRef(this, nativeIter), out resultCount);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return resultCount;
            }
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.HasCurve"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool HasCurve()
        {
            bool hasCurve = false;

            int status = SafeNativeMethods.Gdip.GdipPathIterHasCurve(new HandleRef(this, nativeIter), out hasCurve);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return hasCurve;
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Rewind"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Rewind()
        {
            int status = SafeNativeMethods.Gdip.GdipPathIterRewind(new HandleRef(this, nativeIter));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.Enumerate"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Enumerate(ref PointF[] points, ref byte[] types)
        {
            if (points.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0;
            int size = (int)Marshal.SizeOf(typeof(GPPOINTF));
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
                    SafeNativeMethods.ZeroMemory((IntPtr)(checked((long)memoryPts + resultCount * size)), (UIntPtr)((count - resultCount) * size));
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

        /// <include file='doc\GraphicsPathIterator.uex' path='docs/doc[@for="GraphicsPathIterator.CopyData"]/*' />
        /// <devdoc>
        ///    <para>
        ///     points - pointF array to copy the retrieved point data
        ///     types - type array to copy the retrieved type data
        ///     startIndex - start index of the origianl data
        ///     endIndex - end index of the origianl data
        ///    </para>
        /// </devdoc>
        public int CopyData(ref PointF[] points, ref byte[] types, int startIndex, int endIndex)
        {
            if ((points.Length != types.Length) || (endIndex - startIndex + 1 > points.Length))
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int resultCount = 0;
            int size = (int)Marshal.SizeOf(typeof(GPPOINTF));
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
                    SafeNativeMethods.ZeroMemory((IntPtr)(checked((long)memoryPts + resultCount * size)), (UIntPtr)((count - resultCount) * size));
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

        /*
         * handle to native path iterator object
         */
        internal IntPtr nativeIter;
    }
}
