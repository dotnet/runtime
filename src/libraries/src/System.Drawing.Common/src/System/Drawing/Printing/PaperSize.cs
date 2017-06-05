// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    using System.Globalization;

    /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies
    ///       the size of a piece of paper.
    ///    </para>
    /// </devdoc>
    [Serializable]
    public class PaperSize
    {
        private PaperKind _kind;
        private string _name;

        // standard hundredths of an inch units
        private int _width;
        private int _height;
        private bool _createdByDefaultConstructor;

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.PaperSize2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PaperSize'/> class with default properties.
        ///       This constructor is required for the serialization of the <see cref='System.Drawing.Printing.PaperSize'/> class.
        ///    </para>
        /// </devdoc>
        public PaperSize()
        {
            _kind = PaperKind.Custom;
            _name = String.Empty;
            _createdByDefaultConstructor = true;
        }

        internal PaperSize(PaperKind kind, string name, int width, int height)
        {
            _kind = kind;
            _name = name;
            _width = width;
            _height = height;
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.PaperSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PaperSize'/> class.
        ///    </para>
        /// </devdoc>
        public PaperSize(string name, int width, int height)
        {
            _kind = PaperKind.Custom;
            _name = name;
            _width = width;
            _height = height;
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.Height"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets
        ///       the height of the paper, in hundredths of an inch.</para>
        /// </devdoc>
        public int Height
        {
            get
            {
                return _height;
            }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor) throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _height = value;
            }
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.Kind"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the type of paper.
        ///       
        ///    </para>
        /// </devdoc>
        public PaperKind Kind
        {
            get
            {
                if (_kind <= (PaperKind)SafeNativeMethods.DMPAPER_LAST &&
                    !(_kind == (PaperKind)SafeNativeMethods.DMPAPER_RESERVED_48 || _kind == (PaperKind)SafeNativeMethods.DMPAPER_RESERVED_49))
                    return _kind;
                else
                    return PaperKind.Custom;
            }
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.PaperName"]/*' />
        /// <devdoc>
        ///    <para>Gets
        ///       or sets the name of the type of paper.</para>
        /// </devdoc>
        public string PaperName
        {
            get { return _name; }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor) throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _name = value;
            }
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.RawKind"]/*' />
        /// <devdoc>
        /// <para>
        /// Same as Kind, but values larger than or equal to DMPAPER_LAST do not map to PaperKind.Custom.
        /// This property is needed for serialization of the PrinterSettings object.
        /// </para>
        /// </devdoc>
        public int RawKind
        {
            get { return unchecked((int)_kind); }
            set { _kind = unchecked((PaperKind)value); }
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.Width"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets
        ///       the width of the paper, in hundredths of an inch.</para>
        /// </devdoc>
        public int Width
        {
            get
            {
                return _width;
            }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor) throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _width = value;
            }
        }

        /// <include file='doc\PaperSize.uex' path='docs/doc[@for="PaperSize.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the PaperSize in
        ///       String form.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            return "[PaperSize " + PaperName
            + " Kind=" + Kind.ToString()
            + " Height=" + Height.ToString(CultureInfo.InvariantCulture)
            + " Width=" + Width.ToString(CultureInfo.InvariantCulture)
            + "]";
        }
    }
}

