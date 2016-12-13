// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System;
    using System.ComponentModel;

    /// <include file='doc\DirectoryDescriptionAttribute.uex' path='docs/doc[@for="DSDescriptionAttribute"]/*' />
    /// <internalonly/>
    /// <devdoc>
    ///    <para>DescriptionAttribute marks a property, event, or extender with a
    ///       description. Visual designers can display this description when referencing
    ///       the member.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public class DSDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced = false;

        /// <include file='doc\DirectoryDescriptionAttribute.uex' path='docs/doc[@for="DSDescriptionAttribute.DSDescriptionAttribute"]/*' />
        /// <devdoc>
        ///     Constructs a new sys description.
        /// </devdoc>
        public DSDescriptionAttribute(string description) : base(description)
        {
        }

        /// <include file='doc\DirectoryDescriptionAttribute.uex' path='docs/doc[@for="DSDescriptionAttribute.Description"]/*' />
        /// <devdoc>
        ///     Retrieves the description text.
        /// </devdoc>
        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;
                    DescriptionValue = Res.GetString(base.Description);
                }
                return base.Description;
            }
        }
    }
}
