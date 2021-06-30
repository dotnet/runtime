// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// The type resolution service is used to load types at design time.
    /// </summary>
    public interface ITypeResolutionService
    {
        /// <summary>
        /// Retrieves the requested assembly.
        /// </summary>
        Assembly? GetAssembly(AssemblyName name);

        /// <summary>
        /// Retrieves the requested assembly.
        /// </summary>
        Assembly? GetAssembly(AssemblyName name, bool throwOnError);

        /// <summary>
        /// Loads a type with the given name.
        /// </summary>
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name);

        /// <summary>
        /// Loads a type with the given name.
        /// </summary>
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError);

        /// <summary>
        /// Loads a type with the given name.
        /// </summary>
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError, bool ignoreCase);

        /// <summary>
        /// References the given assembly name. Once an assembly has
        /// been referenced types may be loaded from it without
        /// qualifying them with the assembly.
        /// </summary>
        void ReferenceAssembly(AssemblyName name);

        /// <summary>
        /// Returns the path to the file name from which the assembly was loaded.
        /// </summary>
        string? GetPathOfAssembly(AssemblyName name);
    }
}
