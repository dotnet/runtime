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
    ///       Represents a primitive value.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodePrimitiveExpression : CodeExpression
    {
        private object _value;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodePrimitiveExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodePrimitiveExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodePrimitiveExpression'/> using the specified
        ///       object.
        ///    </para>
        /// </devdoc>
        public CodePrimitiveExpression(object value)
        {
            Value = value;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the object to represent.
        ///    </para>
        /// </devdoc>
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
    }
}
