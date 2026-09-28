// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This attribute is used to mark extension members and associate them with a specific marker type (which provides detailed information about an extension block and its receiver parameter).
    /// This attribute should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public sealed class ExtensionMarkerAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="ExtensionMarkerAttribute"/> class.</summary>
        /// <param name="name">The name of the marker type this extension member is associated with.</param>
        public ExtensionMarkerAttribute(string name)
            => Name = name;

        /// <summary>The name of the marker type this extension member is associated with.</summary>
        public string Name { get; }
    }
}
