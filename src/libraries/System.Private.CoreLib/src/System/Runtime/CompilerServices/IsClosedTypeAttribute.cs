// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved for use by a compiler for tracking metadata.
    /// This attribute should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class IsClosedTypeAttribute : Attribute
    {
        private Type[] _derivedTypes = Type.EmptyTypes;

        /// <summary>Initializes the attribute.</summary>
        public IsClosedTypeAttribute()
        {
        }

        /// <summary>Gets or sets the derived types of the closed type.</summary>
        /// <value>An array of the derived types of the closed type. A <see langword="null" /> value is normalized to an empty array.</value>
        public Type[] DerivedTypes
        {
            get => _derivedTypes;
            set => _derivedTypes = value ?? Type.EmptyTypes;
        }
    }
}
