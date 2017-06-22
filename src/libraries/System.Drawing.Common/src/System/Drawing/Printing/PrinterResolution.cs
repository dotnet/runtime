// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    using System.ComponentModel;
    using System.Globalization;

    /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution"]/*' />
    /// <devdoc>
    ///    <para> Retrieves
    ///       the resolution supported by a printer.</para>
    /// </devdoc>
    public partial class PrinterResolution
    {
        private int _x;
        private int _y;
        private PrinterResolutionKind _kind;

        /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution.PrinterResolution"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PrinterResolution'/> class with default properties.
        ///       This constructor is required for the serialization of the <see cref='System.Drawing.Printing.PrinterResolution'/> class.
        ///    </para>
        /// </devdoc>
        public PrinterResolution()
        {
            _kind = PrinterResolutionKind.Custom;
        }

        internal PrinterResolution(PrinterResolutionKind kind, int x, int y)
        {
            _kind = kind;
            _x = x;
            _y = y;
        }

        /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution.Kind"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       a value indicating the kind of printer resolution.
        ///       Setter added to enable serialization of the PrinterSettings object.
        ///    </para>
        /// </devdoc>
        public PrinterResolutionKind Kind
        {
            get { return _kind; }
            set
            {
                //valid values are 0xfffffffc to 0x0
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), unchecked((int)PrinterResolutionKind.High), unchecked((int)PrinterResolutionKind.Custom)))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(PrinterResolutionKind));
                }

                _kind = value;
            }
        }

        /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution.X"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the printer resolution in the horizontal direction,
        ///       in dots per inch.
        ///       Setter added to enable serialization of the PrinterSettings object.
        ///    </para>
        /// </devdoc>
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution.Y"]/*' />
        /// <devdoc>
        ///    <para> Gets the printer resolution in the vertical direction,
        ///       in dots per inch.
        ///       Setter added to enable serialization of the PrinterSettings object.
        ///    </para>
        /// </devdoc>
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        /// <include file='doc\PrinterResolution.uex' path='docs/doc[@for="PrinterResolution.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the PrinterResolution in
        ///       String form.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            if (_kind != PrinterResolutionKind.Custom)
                return "[PrinterResolution " + Kind.ToString()
                + "]";
            else
                return "[PrinterResolution"
                + " X=" + X.ToString(CultureInfo.InvariantCulture)
                + " Y=" + Y.ToString(CultureInfo.InvariantCulture)
                + "]";
        }
    }
}
