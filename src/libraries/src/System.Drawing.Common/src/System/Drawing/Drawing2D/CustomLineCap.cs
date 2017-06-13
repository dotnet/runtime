// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Runtime.InteropServices;

    /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap"]/*' />
    /// <devdoc>
    ///    Encapsulates a custom user-defined line
    ///    cap.
    /// </devdoc>
    public class CustomLineCap : MarshalByRefObject, ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif


        /*
         * Handle to native line cap object
         */
        internal SafeCustomLineCapHandle nativeCap = null;

        private bool _disposed = false;

        // For subclass creation
        internal CustomLineCap() { }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.CustomLineCap"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.CustomLineCap'/> class with the specified outline
        ///       and fill.
        ///    </para>
        /// </devdoc>
        public CustomLineCap(GraphicsPath fillPath,
                             GraphicsPath strokePath) :
            this(fillPath, strokePath, LineCap.Flat)
        { }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.CustomLineCap1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.CustomLineCap'/> class from the
        ///       specified existing <see cref='System.Drawing.Drawing2D.LineCap'/> with the specified outline and
        ///       fill.
        ///    </para>
        /// </devdoc>
        public CustomLineCap(GraphicsPath fillPath,
                             GraphicsPath strokePath,
                             LineCap baseCap) :
            this(fillPath, strokePath, baseCap, 0)
        { }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.CustomLineCap2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.CustomLineCap'/> class from the
        ///       specified existing <see cref='System.Drawing.Drawing2D.LineCap'/> with the specified outline, fill, and
        ///       inset.
        ///    </para>
        /// </devdoc>
        public CustomLineCap(GraphicsPath fillPath,
                             GraphicsPath strokePath,
                             LineCap baseCap,
                             float baseInset)
        {
            IntPtr nativeLineCap = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateCustomLineCap(
                                new HandleRef(fillPath, (fillPath == null) ? IntPtr.Zero : fillPath.nativePath),
                                new HandleRef(strokePath, (strokePath == null) ? IntPtr.Zero : strokePath.nativePath),
                                baseCap, baseInset, out nativeLineCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeLineCap(nativeLineCap);
        }

        internal CustomLineCap(IntPtr nativeLineCap)
        {
            SetNativeLineCap(nativeLineCap);
        }

        internal void SetNativeLineCap(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentNullException("handle");

            nativeCap = new SafeCustomLineCapHandle(handle);
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.Dispose"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.CustomLineCap'/>.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.Dispose2"]/*' />
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

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Drawing2D.CustomLineCap'/>.
        /// </devdoc>
        ~CustomLineCap()
        {
            Dispose(false);
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.Drawing2D.CustomLineCap'/>.
        /// </devdoc>
        public object Clone()
        {
            IntPtr cloneCap = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneCustomLineCap(new HandleRef(this, nativeCap), out cloneCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return CustomLineCap.CreateCustomLineCapObject(cloneCap);
        }

        internal static CustomLineCap CreateCustomLineCapObject(IntPtr cap)
        {
            CustomLineCapType capType = 0;

            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapType(new HandleRef(null, cap), out capType);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDeleteCustomLineCap(new HandleRef(null, cap));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            switch (capType)
            {
                case CustomLineCapType.Default:
                    return new CustomLineCap(cap);

                case CustomLineCapType.AdjustableArrowCap:
                    return new AdjustableArrowCap(cap);
            }

            SafeNativeMethods.Gdip.GdipDeleteCustomLineCap(new HandleRef(null, cap));
            throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.NotImplemented);
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.SetStrokeCaps"]/*' />
        /// <devdoc>
        ///    Sets the caps used to start and end lines.
        /// </devdoc>
        public void SetStrokeCaps(LineCap startCap, LineCap endCap)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), startCap, endCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.GetStrokeCaps"]/*' />
        /// <devdoc>
        ///    Gets the caps used to start and end lines.
        /// </devdoc>
        public void GetStrokeCaps(out LineCap startCap, out LineCap endCap)
        {
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapStrokeCaps(new HandleRef(this, nativeCap), out startCap, out endCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private void _SetStrokeJoin(LineJoin lineJoin)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), lineJoin);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private LineJoin _GetStrokeJoin()
        {
            LineJoin lineJoin;

            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapStrokeJoin(new HandleRef(this, nativeCap), out lineJoin);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return lineJoin;
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.StrokeJoin"]/*' />
        /// <devdoc>
        ///    Gets or sets the <see cref='System.Drawing.Drawing2D.LineJoin'/> used by this custom cap.
        /// </devdoc>
        public LineJoin StrokeJoin
        {
            get { return _GetStrokeJoin(); }
            set { _SetStrokeJoin(value); }
        }

        private void _SetBaseCap(LineCap baseCap)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapBaseCap(new HandleRef(this, nativeCap), baseCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private LineCap _GetBaseCap()
        {
            LineCap baseCap;
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapBaseCap(new HandleRef(this, nativeCap), out baseCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return baseCap;
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.BaseCap"]/*' />
        /// <devdoc>
        ///    Gets or sets the <see cref='System.Drawing.Drawing2D.LineCap'/> on which this <see cref='System.Drawing.Drawing2D.CustomLineCap'/> is based.
        /// </devdoc>
        public LineCap BaseCap
        {
            get { return _GetBaseCap(); }
            set { _SetBaseCap(value); }
        }

        private void _SetBaseInset(float inset)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapBaseInset(new HandleRef(this, nativeCap), inset);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private float _GetBaseInset()
        {
            float inset;
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapBaseInset(new HandleRef(this, nativeCap), out inset);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return inset;
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.BaseInset"]/*' />
        /// <devdoc>
        ///    Gets or sets the distance between the cap
        ///    and the line.
        /// </devdoc>
        public float BaseInset
        {
            get { return _GetBaseInset(); }
            set { _SetBaseInset(value); }
        }

        private void _SetWidthScale(float widthScale)
        {
            int status = SafeNativeMethods.Gdip.GdipSetCustomLineCapWidthScale(new HandleRef(this, nativeCap), widthScale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private float _GetWidthScale()
        {
            float widthScale;
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapWidthScale(new HandleRef(this, nativeCap), out widthScale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return widthScale;
        }

        /// <include file='doc\CustomLineCap.uex' path='docs/doc[@for="CustomLineCap.WidthScale"]/*' />
        /// <devdoc>
        ///    Gets or sets the amount by which to scale
        ///    the width of the cap.
        /// </devdoc>
        public float WidthScale
        {
            get { return _GetWidthScale(); }
            set { _SetWidthScale(value); }
        }
    }
}
