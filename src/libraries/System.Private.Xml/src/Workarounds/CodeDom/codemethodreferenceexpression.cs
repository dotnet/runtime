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
    ///       Represents an
    ///       expression to invoke a method, to be called on a given target.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeMethodReferenceExpression : CodeExpression
    {
        private CodeExpression _targetObject;
        private string _methodName;

        private CodeTypeReferenceCollection _typeArguments;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeMethodReferenceExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeMethodReferenceExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeMethodReferenceExpression'/> using the specified
        ///       target object and method name.
        ///    </para>
        /// </devdoc>
        public CodeMethodReferenceExpression(CodeExpression targetObject, string methodName)
        {
            TargetObject = targetObject;
            MethodName = methodName;
        }

        public CodeMethodReferenceExpression(CodeExpression targetObject, string methodName, params CodeTypeReference[] typeParameters)
        {
            TargetObject = targetObject;
            MethodName = methodName;
            if (typeParameters != null && typeParameters.Length > 0)
            {
                TypeArguments.AddRange(typeParameters);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the target object.
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
        ///       Gets or sets the name of the method to invoke.
        ///    </para>
        /// </devdoc>
        public string MethodName
        {
            get
            {
                return (_methodName == null) ? string.Empty : _methodName;
            }
            set
            {
                _methodName = value;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CodeTypeReferenceCollection TypeArguments
        {
            get
            {
                if (_typeArguments == null)
                {
                    _typeArguments = new CodeTypeReferenceCollection();
                }
                return _typeArguments;
            }
        }
    }
}
