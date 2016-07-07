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
    ///       Represents a reference to a field.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeFieldReferenceExpression : CodeExpression
    {
        private CodeExpression _targetObject;
        private string _fieldName;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeFieldReferenceExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeFieldReferenceExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeFieldReferenceExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeFieldReferenceExpression(CodeExpression targetObject, string fieldName)
        {
            TargetObject = targetObject;
            FieldName = fieldName;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the target object.
        ///    </para>
        /// </devdoc>
        public CodeExpression TargetObject
        {
            get
            {
                return _targetObject;
            }
            set
            {
                _targetObject = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the field name.
        ///    </para>
        /// </devdoc>
        public string FieldName
        {
            get
            {
                return (_fieldName == null) ? string.Empty : _fieldName;
            }
            set
            {
                _fieldName = value;
            }
        }
    }
}
