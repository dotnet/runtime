// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates the type of the async method builder that should be used by a language compiler to build the attributed method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method |
                    AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Module,
                    Inherited = false, AllowMultiple = true)]
    public sealed class AsyncMethodBuilderOverrideAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="AsyncMethodBuilderOverrideAttribute"/>.</summary>
        /// <param name="builderType">The <see cref="Type"/> of the associated builder.</param>
        public AsyncMethodBuilderOverrideAttribute(Type builderType) => BuilderType = builderType;

        /// <summary>Gets the <see cref="Type"/> of the associated builder.</summary>
        public Type BuilderType { get; }
    }
}
