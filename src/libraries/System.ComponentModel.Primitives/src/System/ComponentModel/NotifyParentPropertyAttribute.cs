// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Indicates whether the parent property is notified if a child namespace
    /// property is modified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotifyParentPropertyAttribute : Attribute
    {
        /// <summary>
        /// Specifies that the parent property should be notified on changes to the
        /// child class property. This field is read-only.
        /// </summary>
        public static readonly NotifyParentPropertyAttribute Yes = new NotifyParentPropertyAttribute(true);

        /// <summary>
        /// Specifies that the parent property should not be notified of changes to the
        /// child class property. This field is read-only.
        /// </summary>
        public static readonly NotifyParentPropertyAttribute No = new NotifyParentPropertyAttribute(false);

        /// <summary>
        /// Specifies the default attribute state, that the parent property should not be
        /// notified of changes to the child class property.
        /// This field is read-only.
        /// </summary>
        public static readonly NotifyParentPropertyAttribute Default = No;

        /// <summary>
        /// Initializes a new instance of the NotifyPropertyAttribute class that uses the
        /// specified value to indicate whether the parent property should be notified
        /// when a child namespace property is modified.
        /// </summary>
        public NotifyParentPropertyAttribute(bool notifyParent)
        {
            NotifyParent = notifyParent;
        }


        /// <summary>
        /// Gets or sets whether the parent property should be notified on changes to a
        /// child namespace property.
        /// </summary>
        public bool NotifyParent { get; }

        /// <summary>
        /// Tests whether the specified object is the same as the current object.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is NotifyParentPropertyAttribute other && other.NotifyParent == NotifyParent;

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
