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
        private readonly string _propertyName;
        private readonly string _receiverTypeName;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </summary>
        public ProvidePropertyAttribute(string propertyName, Type receiverType)
        {
            _propertyName = propertyName;
            _receiverTypeName = receiverType.AssemblyQualifiedName;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </summary>
        public ProvidePropertyAttribute(string propertyName, string receiverTypeName)
        {
            _propertyName = propertyName;
            _receiverTypeName = receiverTypeName;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of a property that this class provides.
        ///    </para>
        /// </summary>
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the data type this property can extend
        ///    </para>
        /// </summary>
        public string ReceiverTypeName
        {
            get
            {
                return _receiverTypeName;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ProvidePropertyAttribute other = obj as ProvidePropertyAttribute;

            return (other != null) && other._propertyName == _propertyName && other._receiverTypeName == _receiverTypeName;
        }

        public override int GetHashCode()
        {
            return _propertyName.GetHashCode() ^ _receiverTypeName.GetHashCode();
        }

        public override object TypeId
        {
            get
            {
                return base.GetType().FullName + _propertyName;
            }
        }
    }
}


