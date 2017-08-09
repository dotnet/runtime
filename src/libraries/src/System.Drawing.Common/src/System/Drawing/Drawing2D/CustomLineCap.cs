// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{    
    public partial class CustomLineCap : MarshalByRefObject, ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        // Handle to native line cap object
        internal SafeCustomLineCapHandle nativeCap = null;

        private bool _disposed = false;

        // For subclass creation
        internal CustomLineCap() { }

        public CustomLineCap(GraphicsPath fillPath, GraphicsPath strokePath) : this(fillPath, strokePath, LineCap.Flat) { }

        public CustomLineCap(GraphicsPath fillPath, GraphicsPath strokePath, LineCap baseCap) : this(fillPath, strokePath, baseCap, 0) { }

        public CustomLineCap(GraphicsPath fillPath, GraphicsPath strokePath, LineCap baseCap, float baseInset)
        {
            IntPtr nativeLineCap;
            int status = SafeNativeMethods.Gdip.GdipCreateCustomLineCap(
                                new HandleRef(fillPath, (fillPath == null) ? IntPtr.Zero : fillPath.nativePath),
                                new HandleRef(strokePath, (strokePath == null) ? IntPtr.Zero : strokePath.nativePath),
                                baseCap, baseInset, out nativeLineCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeLineCap(nativeLineCap);
        }

        internal CustomLineCap(IntPtr nativeLineCap) => SetNativeLineCap(nativeLineCap);

        internal void SetNativeLineCap(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentNullException("handle");

            nativeCap = new SafeCustomLineCapHandle(handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

#if FINALIZATION_WATCH
            if (!disposing && nativeCap != null)
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif
            // propagate the explicit dispose call to the child
            if (disposing && nativeCap != null)
            {
                nativeCap.Dispose();
            }

            _disposed = true;
        }

        ~CustomLineCap() => Dispose(false);

        public virtual object Clone()
        {
            IntPtr clonedCap;
            int status = SafeNativeMethods.Gdip.GdipCloneCustomLineCap(new HandleRef(this, nativeCap), out clonedCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return CreateCustomLineCapObject(clonedCap);
        }

        public void SetStrokeCaps(LineCap startCap, LineCap endCap)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), startCap, endCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void GetStrokeCaps(out LineCap startCap, out LineCap endCap)
        {
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), out startCap, out endCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public LineJoin StrokeJoin
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), out LineJoin lineJoin);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return lineJoin;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public LineCap BaseCap
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapBaseCap(new HandleRef(this, nativeCap), out LineCap baseCap);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return baseCap;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapBaseCap(new HandleRef(this, nativeCap), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public float BaseInset
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapBaseInset(new HandleRef(this, nativeCap), out float inset);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return inset;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapBaseInset(new HandleRef(this, nativeCap), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public float WidthScale
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapWidthScale(new HandleRef(this, nativeCap), out float widthScale);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return widthScale;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapWidthScale(new HandleRef(this, nativeCap), value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }
    }
}
