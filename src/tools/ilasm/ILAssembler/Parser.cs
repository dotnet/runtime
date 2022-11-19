// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace ILAssembler;

internal abstract record Declaration
{
    public ImmutableArray<CustomAttributeDeclaration> CustomAttributes { get; init; }
    public ImmutableArray<DeclarativeSecurityAttributeDeclaration> DeclarativeSecurityAttributes { get; init; }
}

internal sealed record AsemblyDeclaration(string Name, Version version, string? Culture, AssemblyNameFlags Flags, byte[]? PublicKey, byte[]? PublicKeyToken, AssemblyHashAlgorithm HashAlgorithm) : Declaration;

internal class Parser
{

}
