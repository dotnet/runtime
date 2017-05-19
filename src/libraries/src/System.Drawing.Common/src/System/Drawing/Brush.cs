// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;

    /**
     * Represent a Brush object
     */
    /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush"]/*' />
    /// <devdoc>
    ///     <para>
    ///         Classes derrived from this abstract base class define objects used to fill the 
    ///         interiors of graphical shapes such as rectangles, ellipses, pies, polygons, and paths.
    ///     </para>
    /// </devdoc>
    public abstract class Brush : MarshalByRefObject, ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif
        // handle to native GDI+ brush object to be used on demand.
        /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush.nativeBrush;"]/*' />
        private IntPtr _nativeBrush;

        /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush.Clone"]/*' />
        /// <devdoc>
        ///    When overriden in a derived class, creates
        ///    an exact copy of this <see cref='System.Drawing.Brush'/>.
        /// </devdoc>
        public abstract object Clone();


        /// <devdoc>
        ///     Sets the native GDI+ brush reference.
        ///     Note: This method is intended to be used by derived classes only! (internal protected doesn't work as in C++).
        /// </devdoc>
        protected internal void SetNativeBrush(IntPtr brush)
        {
            SetNativeBrushInternal(brush);
        }

        internal void SetNativeBrushInternal(IntPtr brush)
        {
            Debug.Assert(brush != IntPtr.Zero, "WARNING: Assigning null to the GDI+ native brush object.");
            Debug.Assert(_nativeBrush == IntPtr.Zero, "WARNING: Initialized GDI+ native brush object being assigned a new value.");

            _nativeBrush = brush;
        }


        /// <devdoc>
        ///    Gets the GDI+ native object reference. Triggers GDI+ obect initialization.
        /// </devdoc>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        internal IntPtr NativeBrush
        {
            get
            {
                //Need to comment this line out to allow for checking this.NativePen == IntPtr.Zero.
                //Debug.Assert(this.nativeBrush != IntPtr.Zero, "this.nativeBrush == null." );
                return _nativeBrush;
            }
        }

        /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush.Dispose"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Deletes this <see cref='System.Drawing.Brush'/>.
        ///    </para>
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush.Dispose1"]/*' />
        protected virtual void Dispose(bool disposing)
        {
#if FINALIZATION_WATCH
            if (!disposing && nativeBrush != IntPtr.Zero )
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif

            if (_nativeBrush != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteBrush(new HandleRef(this, _nativeBrush));
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
                    _nativeBrush = IntPtr.Zero;
                }
            }
        }

        /**
         * Object cleanup
         */
        /// <include file='doc\Brush.uex' path='docs/doc[@for="Brush.Finalize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Releases memory allocated for this <see cref='System.Drawing.Brush'/>.
        ///    </para>
        /// </devdoc>
        ~Brush()
        {
            Dispose(false);
        }
    }
}
