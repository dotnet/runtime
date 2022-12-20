// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public class CustomLineCap : MarshalByRefObject, ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        // Handle to native line cap object
        internal SafeCustomLineCapHandle nativeCap = null!;

        private bool _disposed;

        // For subclass creation
        internal CustomLineCap() { }

        public CustomLineCap(GraphicsPath? fillPath, GraphicsPath? strokePath) : this(fillPath, strokePath, LineCap.Flat) { }

        public CustomLineCap(GraphicsPath? fillPath, GraphicsPath? strokePath, LineCap baseCap) : this(fillPath, strokePath, baseCap, 0) { }

        public CustomLineCap(GraphicsPath? fillPath, GraphicsPath? strokePath, LineCap baseCap, float baseInset)
        {
            IntPtr nativeLineCap;
            int status = Gdip.GdipCreateCustomLineCap(
                                new HandleRef(fillPath, (fillPath == null) ? IntPtr.Zero : fillPath._nativePath),
                                new HandleRef(strokePath, (strokePath == null) ? IntPtr.Zero : strokePath._nativePath),
                                baseCap, baseInset, out nativeLineCap);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            SetNativeLineCap(nativeLineCap);
        }

        internal CustomLineCap(IntPtr nativeLineCap) => SetNativeLineCap(nativeLineCap);

        internal static CustomLineCap CreateCustomLineCapObject(IntPtr cap)
        {
            int status = Gdip.GdipGetCustomLineCapType(cap, out CustomLineCapType capType);
            if (status != Gdip.Ok)
            {
                Gdip.GdipDeleteCustomLineCap(cap);
                throw Gdip.StatusException(status);
            }

            switch (capType)
            {
                case CustomLineCapType.Default:
                    return new CustomLineCap(cap);

                case CustomLineCapType.AdjustableArrowCap:
                    return new AdjustableArrowCap(cap);
            }

            Gdip.GdipDeleteCustomLineCap(cap);
            throw Gdip.StatusException(Gdip.NotImplemented);
        }

        internal void SetNativeLineCap(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(handle));

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

        public object Clone()
        {
            return CoreClone();
        }

        internal virtual object CoreClone()
        {
            IntPtr clonedCap;
            int status = Gdip.GdipCloneCustomLineCap(new HandleRef(this, nativeCap), out clonedCap);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return CreateCustomLineCapObject(clonedCap);
        }

        public void SetStrokeCaps(LineCap startCap, LineCap endCap)
        {
            int status = Gdip.GdipSetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), startCap, endCap);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void GetStrokeCaps(out LineCap startCap, out LineCap endCap)
        {
            int status = Gdip.GdipGetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), out startCap, out endCap);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public LineJoin StrokeJoin
        {
            get
            {
                int status = Gdip.GdipGetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), out LineJoin lineJoin);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return lineJoin;
            }
            set
            {
                int status = Gdip.GdipSetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        public LineCap BaseCap
        {
            get
            {
                int status = Gdip.GdipGetCustomLineCapBaseCap(new HandleRef(this, nativeCap), out LineCap baseCap);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return baseCap;
            }
            set
            {
                int status = Gdip.GdipSetCustomLineCapBaseCap(new HandleRef(this, nativeCap), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        public float BaseInset
        {
            get
            {
                int status = Gdip.GdipGetCustomLineCapBaseInset(new HandleRef(this, nativeCap), out float inset);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return inset;
            }
            set
            {
                int status = Gdip.GdipSetCustomLineCapBaseInset(new HandleRef(this, nativeCap), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        public float WidthScale
        {
            get
            {
                int status = Gdip.GdipGetCustomLineCapWidthScale(new HandleRef(this, nativeCap), out float widthScale);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return widthScale;
            }
            set
            {
                int status = Gdip.GdipSetCustomLineCapWidthScale(new HandleRef(this, nativeCap), value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }
    }
}
