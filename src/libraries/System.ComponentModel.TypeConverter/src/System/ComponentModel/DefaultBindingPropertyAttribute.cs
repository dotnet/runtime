// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the default binding property for a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultBindingPropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/> class.
        /// </summary>
        public DefaultBindingPropertyAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/> class.
        /// </summary>
        public DefaultBindingPropertyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the default binding property for the component this attribute is
        /// bound to.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/>, which is <see langword='null'/>. This
        /// <see langword='static '/>field is read-only. 
        /// </summary>
        public static readonly DefaultBindingPropertyAttribute Default = new DefaultBindingPropertyAttribute();

        public override bool Equals(object obj)
        {
            return obj is DefaultBindingPropertyAttribute other && other.Name == Name;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
