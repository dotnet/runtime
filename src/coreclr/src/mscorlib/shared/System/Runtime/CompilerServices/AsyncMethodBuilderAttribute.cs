// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates the type of the async method builder that should be used by a language compiler to
    /// build the attributed type when used as the return type of an async method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class AsyncMethodBuilderAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="AsyncMethodBuilderAttribute"/>.</summary>
        /// <param name="builderType">The <see cref="Type"/> of the associated builder.</param>
        public AsyncMethodBuilderAttribute(Type builderType) => BuilderType = builderType;

        /// <summary>Gets the <see cref="Type"/> of the associated builder.</summary>
        public Type BuilderType { get; }
    }
}
