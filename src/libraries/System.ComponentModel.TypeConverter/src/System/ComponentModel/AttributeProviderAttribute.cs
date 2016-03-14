// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    using System;
    using System.Security.Permissions;

    /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute"]/*' />
    /// <devdoc>
    /// </devdoc>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Property)]
    public class AttributeProviderAttribute : Attribute
    {
        private string _typeName;
        private string _propertyName;

        /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute.AttributeProviderAttribute"]/*' />
        /// <devdoc>
        ///     Creates a new AttributeProviderAttribute object.
        /// </devdoc>
        public AttributeProviderAttribute(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            _typeName = typeName;
        }

        /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute.AttributeProviderAttribute"]/*' />
        /// <devdoc>
        ///     Creates a new AttributeProviderAttribute object.
        /// </devdoc>
        public AttributeProviderAttribute(string typeName, string propertyName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            _typeName = typeName;
            _propertyName = propertyName;
        }

        /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute.AttributeProviderAttribute1"]/*' />
        /// <devdoc>
        ///     Creates a new AttributeProviderAttribute object.
        /// </devdoc>
        public AttributeProviderAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            _typeName = type.AssemblyQualifiedName;
        }

        /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute.TypeName"]/*' />
        /// <devdoc>
        ///     The TypeName property returns the assembly qualified type name 
        ///     passed into the constructor.
        /// </devdoc>
        public string TypeName
        {
            get
            {
                return _typeName;
            }
        }

        /// <include file='doc\AttributeProviderAttribute.uex' path='docs/doc[@for="AttributeProviderAttribute.TypeName"]/*' />
        /// <devdoc>
        ///     The TypeName property returns the property name that will be used to query attributes from.
        /// </devdoc>
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }
    }
}

