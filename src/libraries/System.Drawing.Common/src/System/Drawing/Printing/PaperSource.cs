// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the paper tray from which the printer gets paper.
    /// </summary>
    public partial class PaperSource
    {
        private string _name;
        private PaperSourceKind _kind;

        /// <summary>
        /// Initializes a new instance of the <see cref='PaperSource'/> class with default properties.
        /// </summary>
        public PaperSource()
        {
            _kind = PaperSourceKind.Custom;
            _name = string.Empty;
        }

        internal PaperSource(PaperSourceKind kind, string name)
        {
            _kind = kind;
            _name = name;
        }

        /// <summary>
        /// Gets a value indicating the type of paper source.
        /// </summary>
        public PaperSourceKind Kind
        {
            get
            {
                if ((unchecked((int)_kind)) >= SafeNativeMethods.DMBIN_USER)
                {
                    return PaperSourceKind.Custom;
                }

                return _kind;
            }
        }

        /// <summary>
        /// Same as Kind, but values larger than DMBIN_USER do not map to PaperSourceKind.Custom.
        /// </summary>
        public int RawKind
        {
            get => unchecked((int)_kind);
            set => _kind = unchecked((PaperSourceKind)value);
        }

        /// <summary>
        /// Gets the name of the paper source.
        /// </summary>
        public string SourceName
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// Provides some interesting information about the PaperSource in String form.
        /// </summary>
        public override string ToString() => $"[PaperSource {SourceName} Kind={Kind}]";
    }
}
