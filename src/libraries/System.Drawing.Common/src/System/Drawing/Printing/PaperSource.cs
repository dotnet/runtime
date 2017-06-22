// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the paper tray from which the printer gets paper.
    ///    </para>
    /// </devdoc>
    public partial class PaperSource
    {
        private string _name;
        private PaperSourceKind _kind;

        /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource.PaperSource"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PaperSource'/> class with default properties.
        ///       This constructor is required for the serialization of the <see cref='System.Drawing.Printing.PaperSource'/> class.
        ///    </para>
        /// </devdoc>
        public PaperSource()
        {
            _kind = PaperSourceKind.Custom;
            _name = String.Empty;
        }

        internal PaperSource(PaperSourceKind kind, string name)
        {
            _kind = kind;
            _name = name;
        }

        /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource.Kind"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       a value indicating the type of paper source.
        ///       
        ///    </para>
        /// </devdoc>
        public PaperSourceKind Kind
        {
            get
            {
                if ((unchecked((int)_kind)) >= SafeNativeMethods.DMBIN_USER)
                    return PaperSourceKind.Custom;
                else
                    return _kind;
            }
        }

        /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource.RawKind"]/*' />
        /// <devdoc>
        ///    <para>
        /// Same as Kind, but values larger than DMBIN_USER do not map to PaperSourceKind.Custom.
        /// This property is needed for serialization of the PrinterSettings object.
        ///    </para>
        /// </devdoc>
        public int RawKind
        {
            get { return unchecked((int)_kind); }
            set { _kind = unchecked((PaperSourceKind)value); }
        }

        /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource.SourceName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the name of the paper source.
        ///       Setter is added for serialization of the PrinterSettings object.
        ///    </para>
        /// </devdoc>
        public string SourceName
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <include file='doc\PaperSource.uex' path='docs/doc[@for="PaperSource.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the PaperSource in
        ///       String form.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            return "[PaperSource " + SourceName
            + " Kind=" + Kind.ToString()
            + "]";
        }
    }
}
