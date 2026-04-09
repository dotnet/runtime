// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the default property for a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultPropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultPropertyAttribute'/> class.
        /// </summary>
        public DefaultPropertyAttribute(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the default property for the component this attribute is
        /// bound to.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.DefaultPropertyAttribute'/>, which is <see langword='null'/>. This
        /// <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly DefaultPropertyAttribute Default = new DefaultPropertyAttribute(null);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is DefaultPropertyAttribute other) && other.Name == Name;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
