// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para> Specifies which methods are extender
    ///       properties.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProvidePropertyAttribute : Attribute
    {
        private readonly string _propertyName;
        private readonly string _receiverTypeName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </devdoc>
        public ProvidePropertyAttribute(string propertyName, Type receiverType)
        {
            _propertyName = propertyName;
            _receiverTypeName = receiverType.AssemblyQualifiedName;
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.ProvidePropertyAttribute'/> class.</para>
        /// </devdoc>
        public ProvidePropertyAttribute(string propertyName, string receiverTypeName)
        {
            _propertyName = propertyName;
            _receiverTypeName = receiverTypeName;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of a property that this class provides.
        ///    </para>
        /// </devdoc>
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the data type this property can extend
        ///    </para>
        /// </devdoc>
        public string ReceiverTypeName
        {
            get
            {
                return _receiverTypeName;
            }
        }

        /// <devdoc>
        ///    <para>ProvidePropertyAttribute overrides this to include the type name and the property name</para>
        /// </devdoc>
        public override object TypeId
        {
            get
            {
                return GetType().FullName + _propertyName;
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
    }
}


