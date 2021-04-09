// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    /// <summary>Specifies a type that should receive notifications of metadata updates.</summary>
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
