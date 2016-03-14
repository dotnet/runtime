// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class TypeDescriptionProviderAttribute : Attribute
    {
        private string _typeName;

        /// <devdoc>
        ///     Creates a new TypeDescriptionProviderAttribute object.
        /// </devdoc>
        public TypeDescriptionProviderAttribute(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            _typeName = typeName;
        }

        /// <devdoc>
        ///     Creates a new TypeDescriptionProviderAttribute object.
        /// </devdoc>
        public TypeDescriptionProviderAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            _typeName = type.AssemblyQualifiedName;
        }

        /// <devdoc>
        ///     The TypeName property returns the assembly qualified type name 
        ///     for the type description provider.
        /// </devdoc>
        public string TypeName
        {
            get
            {
                return _typeName;
            }
        }
    }
}

