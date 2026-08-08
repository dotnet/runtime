// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class PropertyEventTests
    {
        [Fact]
        public void PropertyAndEvent_EmitMapsAndMethodSemantics()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyDelegate extends [mscorlib]System.MulticastDelegate
                {
                    .method public specialname rtspecialname instance void .ctor(object 'object', native int 'method') runtime managed
                    {
                    }
                    .method public virtual instance void Invoke() runtime managed
                    {
                    }
                }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .field private int32 _value

                    .method public hidebysig specialname instance int32 get_Value() cil managed
                    {
                        ldarg.0
                        ldfld int32 Test::_value
                        ret
                    }

                    .method public hidebysig specialname instance void set_Value(int32 value) cil managed
                    {
                        ldarg.0
                        ldarg.1
                        stfld int32 Test::_value
                        ret
                    }

                    .method public hidebysig specialname instance void add_Changed(class MyDelegate value) cil managed
                    {
                        ret
                    }

                    .method public hidebysig specialname instance void remove_Changed(class MyDelegate value) cil managed
                    {
                        ret
                    }

                    .property int32 Value()
                    {
                        .get instance int32 Test::get_Value()
                        .set instance void Test::set_Value(int32)
                    }

                    .event MyDelegate Changed
                    {
                        .addon instance void Test::add_Changed(class MyDelegate)
                        .removeon instance void Test::remove_Changed(class MyDelegate)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(1, reader.GetTableRowCount(TableIndex.Property));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.Event));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.PropertyMap));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.EventMap));
            Assert.Equal(4, reader.GetTableRowCount(TableIndex.MethodSemantics));

            var property = reader.GetPropertyDefinition(reader.PropertyDefinitions.Single());
            var @event = reader.GetEventDefinition(reader.EventDefinitions.Single());

            Assert.Equal("Value", reader.GetString(property.Name));
            Assert.Equal("Changed", reader.GetString(@event.Name));
        }
    }
}
