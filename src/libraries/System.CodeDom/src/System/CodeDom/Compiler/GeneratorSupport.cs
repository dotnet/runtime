// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    [Flags]
    public enum GeneratorSupport
    {
        ArraysOfArrays = 0x1,
        EntryPointMethod = 0x2,
        GotoStatements = 0x4,
        MultidimensionalArrays = 0x8,
        StaticConstructors = 0x10,
        TryCatchStatements = 0x20,
        ReturnTypeAttributes = 0x40,
        DeclareValueTypes = 0x80,
        DeclareEnums = 0x0100,
        DeclareDelegates = 0x0200,
        DeclareInterfaces = 0x0400,
        DeclareEvents = 0x0800,
        AssemblyAttributes = 0x1000,
        ParameterAttributes = 0x2000,
        ReferenceParameters = 0x4000,
        ChainedConstructorArguments = 0x8000,
        NestedTypes = 0x00010000,
        MultipleInterfaceMembers = 0x00020000,
        PublicStaticMembers = 0x00040000,
        ComplexExpressions = 0x00080000,
        Win32Resources = 0x00100000,
        Resources = 0x00200000,
        PartialTypes = 0x00400000,
        GenericTypeReference = 0x00800000,
        GenericTypeDeclaration = 0x01000000,
        DeclareIndexerProperties = 0x02000000,
    }
}
