// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System.ComponentModel;

    /// <include file='doc\SortOption.uex' path='docs/doc[@for="SortOption"]/*' />
    /// <devdoc>
    ///    <para>Specifies how to sort a query.</para>
    /// </devdoc>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SortOption
    {
        private string _propertyName;
        private SortDirection _sortDirection;

        /// <include file='doc\SortOption.uex' path='docs/doc[@for="SortOption.SortOption"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SortOption()
        {
        }

        /// <include file='doc\SortOption.uex' path='docs/doc[@for="SortOption.SortOption1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SortOption(string propertyName, SortDirection direction)
        {
            this.PropertyName = propertyName;
            if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5_3)
            {
                this.Direction = direction;
            }
            else
            {
                this.Direction = _sortDirection;
            }
        }

        /// <include file='doc\SortOption.uex' path='docs/doc[@for="SortOption.PropertyName"]/*' />
        /// <devdoc>
        ///    <para>Specifies a pointer to a stream that contains the type for the attribute.</para>
        /// </devdoc>
        [
            DefaultValue(null),
            DSDescriptionAttribute(Res.DSSortName)
        ]
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _propertyName = value;
            }
        }

        /// <include file='doc\SortOption.uex' path='docs/doc[@for="SortOption.Direction"]/*' />
        /// <devdoc>
        /// <para>Specifies one of the <see cref='System.DirectoryServices.SortDirection'/> values.</para>
        /// </devdoc>
        [
            DefaultValue(SortDirection.Ascending),
            DSDescriptionAttribute(Res.DSSortDirection)
        ]
        public SortDirection Direction
        {
            get
            {
                return _sortDirection;
            }

            set
            {
                if (value < SortDirection.Ascending || value > SortDirection.Descending)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SortDirection));

                _sortDirection = value;
            }
        }
    }
}
