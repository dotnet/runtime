// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace System.Resources.Extensions.BinaryFormat;

internal sealed partial class BinaryFormattedObject
{
    /// <summary>
    ///  Resolver for types.
    /// </summary>
    internal interface ITypeResolver
    {
        /// <summary>
        ///  Resolves the given type name against the specified library.
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetType(String)")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type GetType(TypeName typeName);
    }
}
