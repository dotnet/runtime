// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       The base class for CodeDom objects
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeObject
    {
        private IDictionary _userData = null;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeObject()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public IDictionary UserData
        {
            get
            {
                if (_userData == null)
                {
                    _userData = new ListDictionary();
                }
                return _userData;
            }
        }
    }
}
