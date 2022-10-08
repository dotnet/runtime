// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Contains attributes of an associated <see cref='Metafile'/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class MetafileHeader
    {
        // determine which to use by nullity
        internal MetafileHeaderWmf? wmf;
        internal MetafileHeaderEmf? emf;

        internal MetafileHeader()
        {
        }

        /// <summary>
        /// Gets the type of the associated <see cref='Metafile'/>.
        /// </summary>
        public MetafileType Type
        {
            get
            {
                return IsWmf() ? wmf!.type : emf!.type;
            }
        }

        /// <summary>
        /// Gets the size, in bytes, of the associated <see cref='Metafile'/>.
        /// </summary>
        public int MetafileSize
        {
            get
            {
                return IsWmf() ? wmf!.size : emf!.size;
            }
        }

        /// <summary>
        /// Gets the version number of the associated <see cref='Metafile'/>.
        /// </summary>
        public int Version
        {
            get
            {
                return IsWmf() ? wmf!.version : emf!.version;
            }
        }

        /// <summary>
        /// Gets the horizontal resolution, in dots-per-inch, of the associated <see cref='Metafile'/>.
        /// </summary>
        public float DpiX
        {
            get
            {
                return IsWmf() ? wmf!.dpiX : emf!.dpiX;
            }
        }

        /// <summary>
        /// Gets the vertical resolution, in dots-per-inch, of the associated <see cref='Metafile'/>.
        /// </summary>
        public float DpiY
        {
            get
            {
                return IsWmf() ? wmf!.dpiY : emf!.dpiY;
            }
        }

        /// <summary>
        /// Gets a <see cref='Rectangle'/> that bounds the associated <see cref='Metafile'/>.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                return IsWmf() ?
                    new Rectangle(wmf!.X, wmf.Y, wmf.Width, wmf.Height) :
                    new Rectangle(emf!.X, emf.Y, emf.Width, emf.Height);
            }
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Windows metafile format.
        /// </summary>
        public bool IsWmf()
        {
            if ((wmf == null) && (emf == null))
                throw Gdip.StatusException(Gdip.InvalidParameter);

            if ((wmf != null) &&
                ((wmf.type == MetafileType.Wmf) ||
                 (wmf.type == MetafileType.WmfPlaceable)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Windows Placeable metafile format.
        /// </summary>
        public bool IsWmfPlaceable()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((wmf != null) && (wmf.type == MetafileType.WmfPlaceable));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Windows enhanced metafile format.
        /// </summary>
        public bool IsEmf()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.Emf));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Windows enhanced
        /// metafile format or the Windows enhanced metafile plus.
        /// </summary>
        public bool IsEmfOrEmfPlus()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((emf != null) && (emf.type >= MetafileType.Emf));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Windows enhanced
        /// metafile plus format.
        /// </summary>
        public bool IsEmfPlus()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((emf != null) && (emf.type >= MetafileType.EmfPlusOnly));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is in the Dual enhanced metafile
        /// format. This format supports both the enhanced and the enhanced plus format.
        /// </summary>
        public bool IsEmfPlusDual()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.EmfPlusDual));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> supports only the Windows
        /// enhanced metafile plus format.
        /// </summary>
        public bool IsEmfPlusOnly()
        {
            if (wmf == null && emf == null)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            return ((emf != null) && (emf.type == MetafileType.EmfPlusOnly));
        }

        /// <summary>
        /// Returns a value indicating whether the associated <see cref='Metafile'/> is device-dependent.
        /// </summary>
        public bool IsDisplay()
        {
            return IsEmfPlus() &&
               (((unchecked((int)emf!.emfPlusFlags)) & ((int)EmfPlusFlags.Display)) != 0);
        }

        /// <summary>
        /// Gets the WMF header file for the associated <see cref='Metafile'/>.
        /// </summary>
        public MetaHeader WmfHeader
        {
            get
            {
                if (wmf == null)
                    throw Gdip.StatusException(Gdip.InvalidParameter);

                return wmf.WmfHeader;
            }
        }

        /// <summary>
        /// Gets the size, in bytes, of the enhanced metafile plus header file.
        /// </summary>
        public int EmfPlusHeaderSize
        {
            get
            {
                if (wmf == null && emf == null)
                    throw Gdip.StatusException(Gdip.InvalidParameter);

                return IsWmf() ? wmf!.EmfPlusHeaderSize : emf!.EmfPlusHeaderSize;
            }
        }

        /// <summary>
        /// Gets the logical horizontal resolution, in dots-per-inch, of the associated <see cref='Metafile'/>.
        /// </summary>
        public int LogicalDpiX
        {
            get
            {
                if (wmf == null && emf == null)
                    throw Gdip.StatusException(Gdip.InvalidParameter);

                return IsWmf() ? wmf!.LogicalDpiX : emf!.LogicalDpiX;
            }
        }

        /// <summary>
        /// Gets the logical vertical resolution, in dots-per-inch, of the associated <see cref='Metafile'/>.
        /// </summary>
        public int LogicalDpiY
        {
            get
            {
                if (wmf == null && emf == null)
                    throw Gdip.StatusException(Gdip.InvalidParameter);

                return IsWmf() ? wmf!.LogicalDpiY : emf!.LogicalDpiX;
            }
        }
    }
}
