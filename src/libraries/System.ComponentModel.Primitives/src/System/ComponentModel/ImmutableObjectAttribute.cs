// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies that a object has no sub properties that are editable.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ImmutableObjectAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a object has no sub properties that are editable.
        ///
        /// This is usually used in the properties window to determine if an expandable
        /// object should be rendered as read-only.
        /// </summary>
        public static readonly ImmutableObjectAttribute Yes = new ImmutableObjectAttribute(true);

        /// <summary>
        /// Specifies that a object has at least one editable sub-property.
        ///
        /// This is usually used in the properties window to determine if an expandable
        /// object should be rendered as read-only.
        /// </summary>
        public static readonly ImmutableObjectAttribute No = new ImmutableObjectAttribute(false);


        /// <summary>
        /// Defaults to ImmutableObjectAttribute.No
        /// </summary>
        public static readonly ImmutableObjectAttribute Default = No;

        /// <summary>
        /// Constructs an ImmutableObjectAttribute object.
        /// </summary>
        public ImmutableObjectAttribute(bool immutable)
        {
            Immutable = immutable;
        }

        public bool Immutable { get; }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is ImmutableObjectAttribute other && other.Immutable == Immutable;

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
