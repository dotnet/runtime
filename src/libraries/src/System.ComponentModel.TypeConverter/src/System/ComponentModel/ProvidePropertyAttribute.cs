// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para> Specifies which methods are extender
    ///       properties.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProvidePropertyAttribute : Attribute
    {
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </summary>
        public ProvidePropertyAttribute(string propertyName, Type receiverType)
        {
            PropertyName = propertyName;
            ReceiverTypeName = receiverType.AssemblyQualifiedName;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </summary>
        public ProvidePropertyAttribute(string propertyName, string receiverTypeName)
        {
            PropertyName = propertyName;
            ReceiverTypeName = receiverTypeName;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of a property that this class provides.
        ///    </para>
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        ///    <para>
        ///       Gets the name of the data type this property can extend
        ///    </para>
        /// </summary>
        public string ReceiverTypeName { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ProvidePropertyAttribute other = obj as ProvidePropertyAttribute;

            return (other != null) && other.PropertyName == PropertyName && other.ReceiverTypeName == ReceiverTypeName;
        }

        public override int GetHashCode()
        {
            return PropertyName.GetHashCode() ^ ReceiverTypeName.GetHashCode();
        }

        public override object TypeId => base.GetType().FullName + PropertyName;
    }
}


