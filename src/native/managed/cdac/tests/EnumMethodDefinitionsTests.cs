// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class EnumMethodDefinitionsTests
{
    // Synthetic metadata containing a variety of type and method definitions.
    //
    //  System.String            → Concat, Join, Concat (overload), .ctor, .ctor (overload)
    //  MyNs.MyClass             → MyMethod, ToString, mymethod (case variant), .ctor
    //  GlobalType (no namespace)→ Foo
    //  MyNs.Generic`1           → DoWork
    //  MyNs.Outer               → OuterMethod
    //  MyNs.Outer+Inner         → InnerMethod
    //  Deep.Nested.Ns.DeepType  → DeepMethod
    private static readonly byte[] s_metadataBytes = BuildTestMetadata();

    private static byte[] BuildTestMetadata()
    {
        var mdBuilder = new MetadataBuilder();

        mdBuilder.AddModule(
            0,
            mdBuilder.GetOrAddString("TestModule"),
            mdBuilder.GetOrAddGuid(Guid.Empty),
            default, default);

        mdBuilder.AddAssembly(
            mdBuilder.GetOrAddString("TestAssembly"),
            new Version(1, 0, 0, 0),
            default, default, 0,
            AssemblyHashAlgorithm.None);

        var sigBlob = new BlobBuilder();
        sigBlob.WriteByte(0x00); // DEFAULT calling convention
        sigBlob.WriteCompressedInteger(0);
        sigBlob.WriteByte(0x01); // ELEMENT_TYPE_VOID
        BlobHandle voidSig = mdBuilder.GetOrAddBlob(sigBlob);

        int methodRow = 1;

        MethodDefinitionHandle NextMethod() => MetadataTokens.MethodDefinitionHandle(methodRow);

        void AddMethod(string name)
        {
            mdBuilder.AddMethodDefinition(
                MethodAttributes.Public, default,
                mdBuilder.GetOrAddString(name), voidSig, -1,
                MetadataTokens.ParameterHandle(1));
            methodRow++;
        }

        // <Module> (required first type)
        mdBuilder.AddTypeDefinition(
            default, default, mdBuilder.GetOrAddString("<Module>"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());

        // System.String with overloaded Concat and constructors
        mdBuilder.AddTypeDefinition(
            TypeAttributes.Public,
            mdBuilder.GetOrAddString("System"),
            mdBuilder.GetOrAddString("String"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("Concat");
        AddMethod("Join");
        AddMethod("Concat");
        AddMethod(".ctor");
        AddMethod(".ctor");

        // MyNs.MyClass with case-variant method names and a constructor
        mdBuilder.AddTypeDefinition(
            TypeAttributes.Public,
            mdBuilder.GetOrAddString("MyNs"),
            mdBuilder.GetOrAddString("MyClass"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("MyMethod");
        AddMethod("ToString");
        AddMethod("mymethod");
        AddMethod(".ctor");

        // GlobalType with no namespace
        mdBuilder.AddTypeDefinition(
            TypeAttributes.Public, default,
            mdBuilder.GetOrAddString("GlobalType"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("Foo");

        // MyNs.Generic`1 (generic type with backtick arity notation)
        mdBuilder.AddTypeDefinition(
            TypeAttributes.Public,
            mdBuilder.GetOrAddString("MyNs"),
            mdBuilder.GetOrAddString("Generic`1"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("DoWork");

        // MyNs.Outer
        TypeDefinitionHandle outerHandle = mdBuilder.AddTypeDefinition(
            TypeAttributes.Public,
            mdBuilder.GetOrAddString("MyNs"),
            mdBuilder.GetOrAddString("Outer"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("OuterMethod");

        // Inner (nested inside Outer)
        TypeDefinitionHandle innerHandle = mdBuilder.AddTypeDefinition(
            TypeAttributes.NestedPublic, default,
            mdBuilder.GetOrAddString("Inner"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("InnerMethod");
        mdBuilder.AddNestedType(innerHandle, outerHandle);

        // Deep.Nested.Ns.DeepType (multi-level namespace)
        mdBuilder.AddTypeDefinition(
            TypeAttributes.Public,
            mdBuilder.GetOrAddString("Deep.Nested.Ns"),
            mdBuilder.GetOrAddString("DeepType"),
            default, MetadataTokens.FieldDefinitionHandle(1), NextMethod());
        AddMethod("DeepMethod");

        var rootBuilder = new MetadataRootBuilder(mdBuilder);
        var blobBuilder = new BlobBuilder();
        rootBuilder.Serialize(blobBuilder, 0, 0);
        return blobBuilder.ToArray();
    }

    [Theory]
    // Fully qualified names
    [InlineData("System.String.Join", 0u, 1)]
    [InlineData("MyNs.MyClass.MyMethod", 0u, 1)]
    [InlineData("Deep.Nested.Ns.DeepType.DeepMethod", 0u, 1)]
    // Namespaces are optional — omitting the namespace matches any namespace
    [InlineData("String.Join", 0u, 1)]
    [InlineData("MyClass.MyMethod", 0u, 1)]
    [InlineData("GlobalType.Foo", 0u, 1)]
    [InlineData("DeepType.DeepMethod", 0u, 1)]
    // Overloaded methods enumerate all matching definitions
    [InlineData("System.String.Concat", 0u, 2)]
    // Case-sensitive (flag 0): exact match required
    [InlineData("MyNs.MyClass.mymethod", 0u, 1)]
    [InlineData("MyNs.MyClass.MYMETHOD", 0u, 0)]
    // Case-insensitive (flag 1): matches both MyMethod and mymethod
    [InlineData("MyNs.MyClass.MYMETHOD", 1u, 2)]
    [InlineData("myns.myclass.mymethod", 1u, 2)]
    [InlineData("myns.myclass.MYMETHOD", 1u, 2)]
    // Method parameters in parentheses are stripped and ignored
    [InlineData("System.String.Join()", 0u, 1)]
    [InlineData("System.String.Concat(System.String, System.String)", 0u, 2)]
    [InlineData("MyNs.MyClass.MyMethod(int)", 0u, 1)]
    // Generic types use backtick arity notation
    [InlineData("MyNs.Generic`1.DoWork", 0u, 1)]
    // Nested types with '+' separator
    [InlineData("MyNs.Outer+Inner.InnerMethod", 0u, 1)]
    // Nested types with '/' separator
    [InlineData("MyNs.Outer/Inner.InnerMethod", 0u, 1)]
    // Nested types without namespace
    [InlineData("Outer+Inner.InnerMethod", 0u, 1)]
    [InlineData("Outer/Inner.InnerMethod", 0u, 1)]
    // Constructors use the '.ctor' name — the double-dot is handled correctly
    [InlineData("System.String..ctor", 0u, 2)]
    [InlineData("MyNs.MyClass..ctor", 0u, 1)]
    // Non-existent method on an existing type returns zero matches
    [InlineData("System.String.NonExistent", 0u, 0)]
    // Outer type method is still accessible
    [InlineData("MyNs.Outer.OuterMethod", 0u, 1)]
    public void EnumMethodsByName_ReturnsExpectedCount(string fullName, uint flags, int expectedCount)
    {
        fixed (byte* ptr = s_metadataBytes)
        {
            var reader = new MetadataReader(ptr, s_metadataBytes.Length);
            var emd = new ClrDataModule.EnumMethodDefinitions(reader, flags, TargetPointer.Null);
            emd.Start(fullName);

            int count = 0;
            while (emd.Enumerator.MoveNext())
                count++;

            Assert.Equal(expectedCount, count);
        }
    }

    [Theory]
    // Non-existent type
    [InlineData("NonExistent.Method")]
    // Generic type without backtick notation is not found
    [InlineData("MyNs.Generic.DoWork")]
    // Bare method name without any type qualifier
    [InlineData("Method")]
    // Return type prefix causes resolution failure
    [InlineData("void System.String.Join")]
    public void EnumMethodsByName_ThrowsForUnresolvableInput(string fullName)
    {
        fixed (byte* ptr = s_metadataBytes)
        {
            var reader = new MetadataReader(ptr, s_metadataBytes.Length);
            var emd = new ClrDataModule.EnumMethodDefinitions(reader, 0, TargetPointer.Null);

            Assert.Throws<ArgumentException>(() => emd.Start(fullName));
        }
    }
}
