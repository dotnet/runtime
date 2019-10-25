// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// TODO https://github.com/dotnet/corefx/issues/41201: Design and expose this publicly.

namespace System.Runtime.CompilerServices
{
    /// <summary>States a dependency that one member has on another.</summary>
    /// <remarks>
    /// This can be used to inform tooling of a dependency that is otherwise not evident purely from
    /// metadata and IL, for example a member relied on via reflection.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field /* AttributeTargets.Class | AttributeTargets.Struct */, // TODO: https://github.com/mono/linker/issues/797
        AllowMultiple = true, Inherited = false)]
    internal sealed class PreserveDependencyAttribute : Attribute
    {
        /// <summary>Initializes the attribute.</summary>
        /// <param name="memberSignature">The signature of the member depended.</param>
        public PreserveDependencyAttribute(string memberSignature)
        {
            MemberSignature = memberSignature;
        }

        /// <summary>Initializes the attribute.</summary>
        /// <param name="memberSignature">The signature of the member depended on.</param>
        /// <param name="typeName">The full name of the type containing <paramref name="memberSignature"/>.</param>
        public PreserveDependencyAttribute(string memberSignature, string typeName)
        {
            MemberSignature = memberSignature;
            TypeName = typeName;
        }

        /// <summary>Initializes the attribute.</summary>
        /// <param name="memberSignature">The signature of the member depended on.</param>
        /// <param name="typeName">The full name of the type containing <paramref name="memberSignature"/>.</param>
        /// <param name="assemblyName">The name of the assembly containing <paramref name="typeName"/>.</param>
        public PreserveDependencyAttribute(string memberSignature, string typeName, string assemblyName)
        {
            MemberSignature = memberSignature;
            TypeName = typeName;
            AssemblyName = assemblyName;
        }

        /// <summary>Gets the signature of the member depended on.</summary>
        public string MemberSignature { get; }

        /// <summary>Gets the full name of the type containing the specified member.</summary>
        /// <remarks>If no type name is specified, the type of the consumer is assumed.</remarks>
        public string? TypeName { get; }

        /// <summary>Gets the assembly name of the specified type.</summary>
        /// <remarks>If no assembly name is specified, the assembly of the consumer is assumed.</remarks>
        public string? AssemblyName { get; }

        /// <summary>Gets or sets the condition in which the dependency is applicable, e.g. "DEBUG".</summary>
        public string? Condition { get; set; }
    }
}
