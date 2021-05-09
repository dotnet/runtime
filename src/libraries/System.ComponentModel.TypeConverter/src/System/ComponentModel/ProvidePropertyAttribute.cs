// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies which methods are extender properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProvidePropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.
        /// </summary>
        public ProvidePropertyAttribute(
            string propertyName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type receiverType)
        {
            if (receiverType == null)
            {
                throw new ArgumentNullException(nameof(receiverType));
            }

            PropertyName = propertyName;
            ReceiverTypeName = receiverType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.
        /// </summary>
        public ProvidePropertyAttribute(
            string propertyName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string receiverTypeName)
        {
            PropertyName = propertyName;
            ReceiverTypeName = receiverTypeName;
        }

        /// <summary>
        /// Gets the name of a property that this class provides.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the name of the data type this property can extend
        /// </summary>
        // Using PublicParameterlessConstructor to preserve the type. See https://github.com/mono/linker/issues/1878
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string ReceiverTypeName { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is ProvidePropertyAttribute other
                && other.PropertyName == PropertyName
                && other.ReceiverTypeName == ReceiverTypeName;
        }

        public override int GetHashCode() => HashCode.Combine(PropertyName, ReceiverTypeName);

        public override object TypeId => GetType().FullName + PropertyName;
    }
}
