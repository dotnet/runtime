// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader"]/*' />
    /// <devdoc>
    ///    Contains attributes of an
    ///    associated <see cref='System.Drawing.Imaging.Metafile'/>.
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class MetafileHeader
    {
        // determine which to use by nullity
        internal MetafileHeaderWmf wmf;
        internal MetafileHeaderEmf emf;

        internal MetafileHeader()
        {
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.Type"]/*' />
        /// <devdoc>
        ///    Gets the type of the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public MetafileType Type
        {
            get
            {
                return IsWmf() ? wmf.type : emf.type;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.MetafileSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the size, in bytes, of the associated
        ///    <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        public int MetafileSize
        {
            get
            {
                return IsWmf() ? wmf.size : emf.size;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.Version"]/*' />
        /// <devdoc>
        ///    Gets the version number of the associated
        /// <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public int Version
        {
            get
            {
                return IsWmf() ? wmf.version : emf.version;
            }
        }

        /* FxCop rule 'AvoidBuildingNonCallableCode' - Left here in case it is needed in the future.
         private EmfPlusFlags EmfPlusFlags
         {
             get
             {
                 return IsWmf() ? wmf.emfPlusFlags : emf.emfPlusFlags;
             }
         }
         */

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.DpiX"]/*' />
        /// <devdoc>
        ///    Gets the horizontal resolution, in
        ///    dots-per-inch, of the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public float DpiX
        {
            get
            {
                return IsWmf() ? wmf.dpiX : emf.dpiX;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.DpiY"]/*' />
        /// <devdoc>
        ///    Gets the vertical resolution, in
        ///    dots-per-inch, of the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public float DpiY
        {
            get
            {
                return IsWmf() ? wmf.dpiY : emf.dpiY;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.Bounds"]/*' />
        /// <devdoc>
        ///    Gets a <see cref='System.Drawing.Rectangle'/> that bounds the associated
        /// <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public Rectangle Bounds
        {
            get
            {
                return IsWmf() ?
                    new Rectangle(wmf.X, wmf.Y, wmf.Width, wmf.Height) :
                    new Rectangle(emf.X, emf.Y, emf.Width, emf.Height);
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsWmf"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Windows metafile
        ///    format.
        /// </devdoc>
        public bool IsWmf()
        {
            if ((wmf == null) && (emf == null))
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            if ((wmf != null) &&
                ((wmf.type == MetafileType.Wmf) ||
                 (wmf.type == MetafileType.WmfPlaceable)))
                return true;
            else
                return false;
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsWmfPlaceable"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a value indicating whether the
        ///       associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Windows Placeable metafile
        ///       format.
        ///    </para>
        /// </devdoc>
        public bool IsWmfPlaceable()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((wmf != null) && (wmf.type == MetafileType.WmfPlaceable));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsEmf"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Windows enhanced metafile
        ///    format.
        /// </devdoc>
        public bool IsEmf()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.Emf));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsEmfOrEmfPlus"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Windows enhanced metafile
        ///    format or the Windows enhanced metafile plus.
        /// </devdoc>
        public bool IsEmfOrEmfPlus()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((emf != null) && (emf.type >= MetafileType.Emf));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsEmfPlus"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Windows enhanced metafile
        ///    plus format.
        /// </devdoc>
        public bool IsEmfPlus()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((emf != null) && (emf.type >= MetafileType.EmfPlusOnly));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsEmfPlusDual"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/> is in the Dual enhanced
        ///    metafile format. This format supports both the enhanced and the enhanced
        ///    plus format.
        /// </devdoc>
        public bool IsEmfPlusDual()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.EmfPlusDual));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsEmfPlusOnly"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a value indicating whether the associated <see cref='System.Drawing.Imaging.Metafile'/> supports only the Windows
        ///       enhanced metafile plus format.
        ///    </para>
        /// </devdoc>
        public bool IsEmfPlusOnly()
        {
            if (wmf == null && emf == null)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.EmfPlusOnly));
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.IsDisplay"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a value indicating whether the associated <see cref='System.Drawing.Imaging.Metafile'/> is device-dependent.
        ///    </para>
        /// </devdoc>
        public bool IsDisplay()
        {
            return IsEmfPlus() &&
               (((unchecked((int)emf.emfPlusFlags)) & ((int)EmfPlusFlags.Display)) != 0);
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.WmfHeader"]/*' />
        /// <devdoc>
        ///    Gets the WMF header file for the associated
        /// <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public MetaHeader WmfHeader
        {
            get
            {
                if (wmf == null)
                    throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

                return wmf.WmfHeader;
            }
        }


        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.EmfHeader"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the WMF header file for the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        /* FxCop rule 'AvoidBuildingNonCallableCode' - Left here in case it is needed in the future.
         internal SafeNativeMethods.ENHMETAHEADER EmfHeader
         {
             get
             {
                 if (emf == null)
                     throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

                 return emf.EmfHeader;
              }
          }
        */

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.EmfPlusHeaderSize"]/*' />
        /// <devdoc>
        ///    Gets the size, in bytes, of the
        ///    enhanced metafile plus header file.
        /// </devdoc>
        public int EmfPlusHeaderSize
        {
            get
            {
                if (wmf == null && emf == null)
                    throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

                return IsWmf() ? wmf.EmfPlusHeaderSize : emf.EmfPlusHeaderSize;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.LogicalDpiX"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the logical horizontal resolution, in
        ///       dots-per-inch, of the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        public int LogicalDpiX
        {
            get
            {
                if (wmf == null && emf == null)
                    throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

                return IsWmf() ? wmf.LogicalDpiX : emf.LogicalDpiX;
            }
        }

        /// <include file='doc\MetafileHeader.uex' path='docs/doc[@for="MetafileHeader.LogicalDpiY"]/*' />
        /// <devdoc>
        ///    Gets the logical vertical resolution, in
        ///    dots-per-inch, of the associated <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public int LogicalDpiY
        {
            get
            {
                if (wmf == null && emf == null)
                    throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

                return IsWmf() ? wmf.LogicalDpiY : emf.LogicalDpiX;
            }
        }
    }
}
