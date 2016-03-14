// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies whether a property or event should be displayed in
    ///       a property browsing window.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class BrowsableAttribute : Attribute
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies that a property or event can be modified at
        ///       design time. This <see langword='static '/>
        ///       field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly BrowsableAttribute Yes = new BrowsableAttribute(true);

        /// <devdoc>
        ///    <para>
        ///       Specifies that a property or event cannot be modified at
        ///       design time. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly BrowsableAttribute No = new BrowsableAttribute(false);

        /// <devdoc>
        /// <para>Specifies the default value for the <see cref='System.ComponentModel.BrowsableAttribute'/>,
        ///    which is <see cref='System.ComponentModel.BrowsableAttribute.Yes'/>. This <see langword='static '/>field is read-only.</para>
        /// </devdoc>
        public static readonly BrowsableAttribute Default = Yes;

        private bool _browsable = true;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.BrowsableAttribute'/> class.</para>
        /// </devdoc>
        public BrowsableAttribute(bool browsable)
        {
            _browsable = browsable;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether an object is browsable.
        ///    </para>
        /// </devdoc>
        public bool Browsable
        {
            get
            {
                return _browsable;
            }
        }

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            BrowsableAttribute other = obj as BrowsableAttribute;

            return (other != null) && other.Browsable == _browsable;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return _browsable.GetHashCode();
        }

#if !SILVERLIGHT
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return (this.Equals(Default));
        }
#endif
    }
}
