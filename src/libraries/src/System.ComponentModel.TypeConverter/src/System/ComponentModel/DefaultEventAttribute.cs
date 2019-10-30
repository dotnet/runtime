// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the default event for a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultEventAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultEventAttribute'/> class.
        /// </summary>
        public DefaultEventAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the default event for the component this attribute is bound to.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.DefaultEventAttribute'/>, which is
        /// <see langword='null'/>.
        /// This <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly DefaultEventAttribute Default = new DefaultEventAttribute(null);

        public override bool Equals(object obj)
        {
            return (obj is DefaultEventAttribute other) && other.Name == Name;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
