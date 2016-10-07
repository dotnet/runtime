// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <internalonly/>
    /// <summary>
    ///    <para>
    ///       ExtenderProvidedPropertyAttribute is an attribute that marks that a property
    ///       was actually offered up by and extender provider.
    ///    </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ExtenderProvidedPropertyAttribute : Attribute
    {
        private PropertyDescriptor _extenderProperty;
        private IExtenderProvider _provider;
        private Type _receiverType;

        /// <summary>
        ///     Creates a new ExtenderProvidedPropertyAttribute.
        /// </summary>
        internal static ExtenderProvidedPropertyAttribute Create(PropertyDescriptor extenderProperty, Type receiverType, IExtenderProvider provider)
        {
            ExtenderProvidedPropertyAttribute e = new ExtenderProvidedPropertyAttribute();
            e._extenderProperty = extenderProperty;
            e._receiverType = receiverType;
            e._provider = provider;
            return e;
        }

        /// <summary>
        ///     Creates an empty ExtenderProvidedPropertyAttribute.
        /// </summary>
        public ExtenderProvidedPropertyAttribute()
        {
        }

        /// <summary>
        ///     PropertyDescriptor of the property that is being provided.
        /// </summary>
        public PropertyDescriptor ExtenderProperty
        {
            get
            {
                return _extenderProperty;
            }
        }

        /// <summary>
        ///     Extender provider that is providing the property.
        /// </summary>
        public IExtenderProvider Provider
        {
            get
            {
                return _provider;
            }
        }

        /// <summary>
        ///     The type of object that can receive these properties.
        /// </summary>
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

        public override bool IsDefaultAttribute()
        {
            return _receiverType == null;
        }
    }
}
