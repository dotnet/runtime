// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <internalonly/>
    /// <devdoc>
    ///    <para>
    ///       ExtenderProvidedPropertyAttribute is an attribute that marks that a property
    ///       was actually offered up by and extender provider.
    ///    </para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ExtenderProvidedPropertyAttribute : Attribute
    {
        private PropertyDescriptor _extenderProperty;
        private IExtenderProvider _provider;
        private Type _receiverType;

        /// <devdoc>
        ///     Creates a new ExtenderProvidedPropertyAttribute.
        /// </devdoc>
        internal static ExtenderProvidedPropertyAttribute Create(PropertyDescriptor extenderProperty, Type receiverType, IExtenderProvider provider)
        {
            ExtenderProvidedPropertyAttribute e = new ExtenderProvidedPropertyAttribute();
            e._extenderProperty = extenderProperty;
            e._receiverType = receiverType;
            e._provider = provider;
            return e;
        }

        /// <devdoc>
        ///     Creates an empty ExtenderProvidedPropertyAttribute.
        /// </devdoc>
        public ExtenderProvidedPropertyAttribute()
        {
        }

        /// <devdoc>
        ///     PropertyDescriptor of the property that is being provided.
        /// </devdoc>
        public PropertyDescriptor ExtenderProperty
        {
            get
            {
                return _extenderProperty;
            }
        }

        /// <devdoc>
        ///     Extender provider that is providing the property.
        /// </devdoc>
        public IExtenderProvider Provider
        {
            get
            {
                return _provider;
            }
        }

        /// <devdoc>
        ///     The type of object that can receive these properties.
        /// </devdoc>
        public Type ReceiverType
        {
            get
            {
                return _receiverType;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ExtenderProvidedPropertyAttribute other = obj as ExtenderProvidedPropertyAttribute;

            return (other != null) && other._extenderProperty.Equals(_extenderProperty) && other._provider.Equals(_provider) && other._receiverType.Equals(_receiverType);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return _receiverType == null;
        }
    }
}
