// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Specifies a type that should receive notifications of metadata updates.
    /// <para>
    /// The <see cref="Type" /> specified by this attribute must have at least one static method with the following signature:
    /// <c>static void ClearCache(Type[]? updatedTypes)</c>
    /// <c>static void UpdateApplication(Type[]? updatedTypes)</c>
    /// </para>
    /// <para>
    /// Once a metadata update is applied, <c>ClearCache</c> is invoked for every handler that specifies one. This gives update handlers
    /// an opportunity to clear any caches that are inferred based from the application's metadata. This is followed by invoking the <c>UpdateHandler</c>
    /// method is invoked letting applications update their contents, trigger a UI re-render etc. When specified, the <c>updatedTypes</c>
    /// parameter indicates the sequence of types that were affected by the metadata update.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetadataUpdateHandlerAttribute : Attribute
    {
        /// <summary>Initializes the attribute.</summary>
        /// <param name="handlerType">A type that handles metadata updates and that should be notified when any occur.</param>
        public MetadataUpdateHandlerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type handlerType) =>
            HandlerType = handlerType;

        /// <summary>Gets the type that handles metadata updates and that should be notified when any occur.</summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type HandlerType { get; }
    }
}
