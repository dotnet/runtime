// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Specifies a type that should receive notifications of metadata updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Type" /> specified by this attribute should implement static methods matching the signature of one or more
    /// of the following (visibility of the methods does not matter):
    /// <c>static void ClearCache(Type[]? updatedTypes)</c>
    /// <c>static void UpdateApplication(Type[]? updatedTypes)</c>
    /// </para>
    /// <para>
    /// After a metadata update is applied, <c>ClearCache</c> is invoked for every handler that specifies one. This gives update handlers
    /// an opportunity to clear any caches that are inferred based on the application's metadata. After all <c>ClearCache</c> methods
    /// have been invoked, <c>UpdateApplication</c> is invoked for every handler that specifies one.  This enables applications to refresh
    /// application state, trigger a UI re-render, or other such reactions. When specified, the <c>updatedTypes</c> parameter contains the
    /// set of types that were affected by the metadata update; if it's null, any type may have been updated.
    /// </para>
    /// </remarks>
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
