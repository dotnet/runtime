// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System.ComponentModel;

    /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView"]/*' />
    /// <devdoc>
    ///    <para>Specifies how to do directory virtual list view search.</para>
    /// </devdoc>
    public class DirectoryVirtualListView
    {
        private int _beforeCount = 0;
        private int _afterCount = 0;
        private int _offset = 0;
        private string _target = "";
        private int _approximateTotal = 0;
        private int _targetPercentage = 0;
        private DirectoryVirtualListViewContext _context = null;

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryVirtualListView()
        {
        }

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectoryVirtualListView(int afterCount)
        {
            AfterCount = afterCount;
        }

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectoryVirtualListView(int beforeCount, int afterCount, int offset)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Offset = offset;
        }

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectoryVirtualListView(int beforeCount, int afterCount, string target)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Target = target;
        }

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectoryVirtualListView(int beforeCount, int afterCount, int offset, DirectoryVirtualListViewContext context)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Offset = offset;
            _context = context;
        }

        /// <include file='doc\DirectoryVirtualListView.uex' path='docs/doc[@for="DirectoryVirtualListView.DirectoryVirtualListView5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectoryVirtualListView(int beforeCount, int afterCount, string target, DirectoryVirtualListViewContext context)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Target = target;
            _context = context;
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.BeforeCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the number of entries before the target entry that the client is requesting from the server.</para>
        /// </devdoc>
        [
            DefaultValue(0),
            DSDescriptionAttribute(Res.DSBeforeCount)
        ]
        public int BeforeCount
        {
            get
            {
                return _beforeCount;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadBeforeCount));

                _beforeCount = value;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.AfterCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the number of entries after the target entry that the client is requesting from the server</para>
        /// </devdoc>
        [
            DefaultValue(0),
            DSDescriptionAttribute(Res.DSAfterCount)
        ]
        public int AfterCount
        {
            get
            {
                return _afterCount;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadAfterCount));

                _afterCount = value;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.Offset"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the estimated target entry's requested offset within the list.</para>
        /// </devdoc>
        [
            DefaultValue(0),
            DSDescriptionAttribute(Res.DSOffset)
        ]
        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadOffset));

                _offset = value;
                if (_approximateTotal != 0)
                    _targetPercentage = (int)((double)_offset / _approximateTotal * 100);
                else
                    _targetPercentage = 0;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.AfterCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the offset percentage</para>
        /// </devdoc>
        [
            DefaultValue(0),
            DSDescriptionAttribute(Res.DSTargetPercentage)
        ]
        public int TargetPercentage
        {
            get
            {
                return _targetPercentage;
            }
            set
            {
                if (value > 100 || value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadTargetPercentage));

                _targetPercentage = value;
                _offset = _approximateTotal * _targetPercentage / 100;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.AfterCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the desired target entry requested by the client.</para>
        /// </devdoc>
        [
            DefaultValue(""),
            DSDescriptionAttribute(Res.DSTarget),
            TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign)
        ]
        public string Target
        {
            get
            {
                return _target;
            }
            set
            {
                if (value == null)
                    value = "";

                _target = value;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.AfterCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the estimated total count.</para>
        /// </devdoc>
        [
            DefaultValue(0),
            DSDescriptionAttribute(Res.DSApproximateTotal)
        ]
        public int ApproximateTotal
        {
            get
            {
                return _approximateTotal;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadApproximateTotal));

                _approximateTotal = value;
            }
        }

        /// <include file='doc\DirectoryVirtualListView..uex' path='docs/doc[@for="DirectoryVirtualListView.AfterCount"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the virtual list view search response.</para>
        /// </devdoc>
        [
            DefaultValue(null),
            DSDescriptionAttribute(Res.DSDirectoryVirtualListViewContext)
        ]
        public DirectoryVirtualListViewContext DirectoryVirtualListViewContext
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }
    }
}
