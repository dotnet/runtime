// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Represents an event member.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeMemberEvent : CodeTypeMember
    {
        private CodeTypeReference _type;
        private CodeTypeReference _privateImplements = null;
        private CodeTypeReferenceCollection _implementationTypes = null;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeMemberEvent()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the member field type.
        ///    </para>
        /// </devdoc>
        public CodeTypeReference Type
        {
            get
            {
                if (_type == null)
                {
                    _type = new CodeTypeReference("");
                }
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReference PrivateImplementationType
        {
            get
            {
                return _privateImplements;
            }
            set
            {
                _privateImplements = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReferenceCollection ImplementationTypes
        {
            get
            {
                if (_implementationTypes == null)
                {
                    _implementationTypes = new CodeTypeReferenceCollection();
                }
                return _implementationTypes;
            }
        }
    }
}
