// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class PropertyTests
    {
        [Fact]
        public void Property_BasicProperty_IsEmitted()
        {
            // First check if properties work at all without initOpt
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private int32 _value

                    .property int32 Value()
                    {
                        .get instance int32 Test::get_Value()
                    }

                    .method public hidebysig specialname instance int32 get_Value() cil managed
                    {
                        ldarg.0
                        ldfld int32 Test::_value
                        ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            // Check for diagnostics
            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // Check how many properties are in the table
            var propCount = reader.GetTableRowCount(TableIndex.Property);
            Assert.True(propCount > 0, $"Expected at least 1 property, got {propCount}");
        }

        [Fact]
        public void PropertyAttributesOtherAccessorAndCustomAttribute_EmitMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public specialname instance int32 get_Value() cil managed { ldc.i4.0 ret }
                    .method public specialname instance void set_Value(int32 value) cil managed { ret }
                    .method public specialname instance void other_Value() cil managed { ret }

                    .property specialname rtspecialname instance int32 Value()
                    {
                        .get instance int32 Test::get_Value()
                        .set instance void Test::set_Value(int32)
                        .other instance void Test::other_Value()
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var propertyHandle = Assert.Single(reader.PropertyDefinitions);
            var property = reader.GetPropertyDefinition(propertyHandle);
            var accessors = property.GetAccessors();

            Assert.True(property.Attributes.HasFlag(PropertyAttributes.SpecialName));
            Assert.Equal("get_Value", reader.GetString(reader.GetMethodDefinition(accessors.Getter).Name));
            Assert.Equal("set_Value", reader.GetString(reader.GetMethodDefinition(accessors.Setter).Name));
            Assert.Equal(
                "other_Value",
                reader.GetString(reader.GetMethodDefinition(Assert.Single(accessors.Others)).Name));
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(propertyHandle))).Value));
        }


        [Fact]
        public void PropertyInitOpt_WithConstantValue_CreatesConstantEntry()
        {
            // Test that .property with initOpt creates a constant entry
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private int32 _value

                    .property int32 Value() = int32(42)
                    {
                        .get instance int32 Test::get_Value()
                    }

                    .method public hidebysig specialname instance int32 get_Value() cil managed
                    {
                        ldarg.0
                        ldfld int32 Test::_value
                        ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // Find the property
            var propertyHandle = reader.PropertyDefinitions.First();
            var property = reader.GetPropertyDefinition(propertyHandle);

            // Check attributes include HasDefault
            Assert.True((property.Attributes & System.Reflection.PropertyAttributes.HasDefault) != 0,
                $"Expected HasDefault attribute, got {property.Attributes}");

            // Check for constant
            var constantHandle = property.GetDefaultValue();
            Assert.False(constantHandle.IsNil, "No constant for property");
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.Int32, constant.TypeCode);
            var value = reader.GetBlobReader(constant.Value).ReadInt32();
            Assert.Equal(42, value);
        }

        [Fact]
        public void PropertyInitOpt_WithStringConstant_CreatesConstantEntry()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .field private string _name

                    .property string Name() = "DefaultName"
                    {
                        .get instance string Test::get_Name()
                    }

                    .method public hidebysig specialname instance string get_Name() cil managed
                    {
                        ldarg.0
                        ldfld string Test::_name
                        ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // Find the property
            var propertyHandle = reader.PropertyDefinitions.First();
            var property = reader.GetPropertyDefinition(propertyHandle);

            // Check attributes include HasDefault
            Assert.True((property.Attributes & System.Reflection.PropertyAttributes.HasDefault) != 0,
                $"Expected HasDefault attribute, got {property.Attributes}");

            // Check for constant
            var constantHandle = property.GetDefaultValue();
            Assert.False(constantHandle.IsNil, "No constant for property");
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.String, constant.TypeCode);
            // String constants are stored as UTF-16
            var blobReader = reader.GetBlobReader(constant.Value);
            var stringBytes = blobReader.ReadBytes(blobReader.Length);
            var value = System.Text.Encoding.Unicode.GetString(stringBytes);
            Assert.Equal("DefaultName", value);
        }

        [Fact]
        public void CustomAttribute_OnProperty_NotDropped()
        {
            // Custom attributes inside property declarations (e.g., DispIdAttribute)
            // must be emitted and owned by the property, not silently dropped.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi IFoo extends [mscorlib]System.Object
                {
                    .method public specialname instance int32 get_Value() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                    .property instance int32 Value()
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                        .get instance int32 IFoo::get_Value()
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the property
            var propHandle = reader.PropertyDefinitions.Single();
            var prop = reader.GetPropertyDefinition(propHandle);
            Assert.Equal("Value", reader.GetString(prop.Name));

            // The property should have a custom attribute (ObsoleteAttribute)
            var attrs = reader.GetCustomAttributes(propHandle);
            Assert.True(attrs.Count >= 1, $"Property should have at least 1 custom attribute, got {attrs.Count}");
        }

        [Fact]
        public void IndexedProperty_EmitsSignatureAndAccessorMetadata()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .property instance int32 Item(int32)
                    {
                        .get instance int32 Test::get_Item(int32)
                        .set instance void Test::set_Item(int32, int32)
                    }

                    .method public hidebysig specialname instance int32 get_Item(int32 index) cil managed
                    {
                        ldarg.1
                        ret
                    }

                    .method public hidebysig specialname instance void set_Item(int32 index, int32 value) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var propertyHandle = reader.PropertyDefinitions.Single();
            var property = reader.GetPropertyDefinition(propertyHandle);
            var accessors = property.GetAccessors();
            MethodSignature<string> signature =
                property.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);

            Assert.Equal("Item", reader.GetString(property.Name));
            Assert.Equal("get_Item", reader.GetString(reader.GetMethodDefinition(accessors.Getter).Name));
            Assert.Equal("set_Item", reader.GetString(reader.GetMethodDefinition(accessors.Setter).Name));
            Assert.True(signature.Header.IsInstance);
            Assert.Equal("int32", signature.ReturnType);
            Assert.Equal(new[] { "int32" }, signature.ParameterTypes);
        }
    }
}
