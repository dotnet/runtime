// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.ComponentModel
{
    /// <summary>
    /// ExtenderProvidedPropertyAttribute is an attribute that marks that a property
    /// was actually offered up by and extender provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ExtenderProvidedPropertyAttribute : Attribute
    {
        /// <summary>
        /// Creates a new ExtenderProvidedPropertyAttribute.
        /// </summary>
        internal static ExtenderProvidedPropertyAttribute Create(PropertyDescriptor extenderProperty, Type receiverType, IExtenderProvider provider)
        {
            return new ExtenderProvidedPropertyAttribute
            {
                ExtenderProperty = extenderProperty,
                ReceiverType = receiverType,
                Provider = provider
            };
        }

        /// <summary>
        /// Creates an empty ExtenderProvidedPropertyAttribute.
        /// </summary>
        public ExtenderProvidedPropertyAttribute()
        {
        }

        /// <summary>
        /// PropertyDescriptor of the property that is being provided.
        /// </summary>
        public PropertyDescriptor ExtenderProperty { get; private set; }

        /// <summary>
        /// Extender provider that is providing the property.
        /// </summary>
        public IExtenderProvider Provider { get; private set; }

        /// <summary>
        /// The type of object that can receive these properties.
        /// </summary>
        public Type ReceiverType { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            if (!(obj is ExtenderProvidedPropertyAttribute other))
            {
                return false;
            }

            // ExtenderProperty is null if the attribute is created with
            // the default constructor. In this case, all the properties
            // are null (and are immutable), so we only need to check the
            // nullability of one property to know about the nullability
            // of the rest.
            if (other.ExtenderProperty == null)
            {
                Debug.Assert(other.Provider == null);
                Debug.Assert(other.ReceiverType == null);
                Debug.Assert(Provider == null);
                Debug.Assert(ReceiverType == null);

                return ExtenderProperty == null;
            }

            return other.ExtenderProperty.Equals(ExtenderProperty)
                && other.Provider.Equals(Provider)
                && other.ReceiverType.Equals(ReceiverType);
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => ReceiverType == null;
    }
}
