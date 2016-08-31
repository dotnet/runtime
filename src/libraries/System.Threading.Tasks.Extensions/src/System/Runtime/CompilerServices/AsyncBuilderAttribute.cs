// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates the async method builder that should be used by the C# compiler to build
    /// the attributed type when used as the return type of an async method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncBuilderAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="AsyncBuilderAttribute"/>.</summary>
        /// <param name="builderType">The associated builder type.</param>
        public AsyncBuilderAttribute(Type builderType) { }
    }
}
