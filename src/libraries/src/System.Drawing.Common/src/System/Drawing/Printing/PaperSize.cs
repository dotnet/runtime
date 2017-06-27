// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the size of a piece of paper.
    /// </summary>
    public partial class PaperSize
    {
        private PaperKind _kind;
        private string _name;

        // standard hundredths of an inch units
        private int _width;
        private int _height;
        private bool _createdByDefaultConstructor;

        /// <summary>
        /// Initializes a new instance of the <see cref='PaperSize'/> class with default properties.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Printing.PaperSize'/> class.
        /// </summary>
        public PaperSize(string name, int width, int height)
        {
            _kind = PaperKind.Custom;
            _name = name;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Gets or sets the height of the paper, in hundredths of an inch.
        /// </summary>
        public int Height
        {
            get
            {
                return _height;
            }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor)
                    throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _height = value;
            }
        }

        /// <summary>
        /// Gets the type of paper.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the type of paper.
        /// </summary>
        public string PaperName
        {
            get { return _name; }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor)
                    throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _name = value;
            }
        }

        /// <summary>
        /// Same as Kind, but values larger than or equal to DMPAPER_LAST do not map to PaperKind.Custom.
        /// </summary>
        public int RawKind
        {
            get { return unchecked((int)_kind); }
            set { _kind = unchecked((PaperKind)value); }
        }

        /// <summary>
        /// Gets or sets the width of the paper, in hundredths of an inch.
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }

            set
            {
                if (_kind != PaperKind.Custom && !_createdByDefaultConstructor)
                    throw new ArgumentException(SR.Format(SR.PSizeNotCustom));
                _width = value;
            }
        }

        /// <summary>
        /// Provides some interesting information about the PaperSize in String form.
        /// </summary>
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

