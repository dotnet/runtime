// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Use this attribute to specify typical properties on components that can be bound
    /// to application settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingsBindableAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a property is appropriate to bind settings to.
        /// </summary>
        public static readonly SettingsBindableAttribute Yes = new SettingsBindableAttribute(true);

        /// <summary>
        /// Specifies that a property is not appropriate to bind settings to.
        /// </summary>
        public static readonly SettingsBindableAttribute No = new SettingsBindableAttribute(false);

        public SettingsBindableAttribute(bool bindable)
        {
            Bindable = bindable;
        }

        /// <summary>
        /// Gets a value indicating whether a property is appropriate to bind settings to.
        /// </summary>
        public bool Bindable { get; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is SettingsBindableAttribute other && other.Bindable == Bindable;
        }

        public override int GetHashCode() => Bindable.GetHashCode();
    }
}
