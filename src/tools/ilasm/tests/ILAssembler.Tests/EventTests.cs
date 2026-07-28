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
    public class EventTests
    {
        [Fact]
        public void CustomAttribute_OnEvent_NotDropped()
        {
            // Custom attributes inside event declarations must be emitted
            // and owned by the event, not silently dropped.
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
                .class public auto ansi MyClass extends [mscorlib]System.Object
                {
                    .method public specialname instance void add_MyEvent(class MyDelegate) cil managed
                    {
                        ret
                    }
                    .method public specialname instance void remove_MyEvent(class MyDelegate) cil managed
                    {
                        ret
                    }
                    .event MyDelegate MyEvent
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                        .addon instance void MyClass::add_MyEvent(class MyDelegate)
                        .removeon instance void MyClass::remove_MyEvent(class MyDelegate)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the event
            var eventHandle = reader.EventDefinitions.Single();
            var evt = reader.GetEventDefinition(eventHandle);
            Assert.Equal("MyEvent", reader.GetString(evt.Name));

            // The event should have a custom attribute (ObsoleteAttribute)
            var attrs = reader.GetCustomAttributes(eventHandle);
            Assert.True(attrs.Count >= 1, $"Event should have at least 1 custom attribute, got {attrs.Count}");
        }

        [Fact]
        public void EventWithRaiseAccessor_EmitsAccessorMetadata()
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
                .class public auto ansi MyClass extends [mscorlib]System.Object
                {
                    .method public hidebysig specialname instance void add_MyEvent(class MyDelegate value) cil managed
                    {
                        ret
                    }
                    .method public hidebysig specialname instance void remove_MyEvent(class MyDelegate value) cil managed
                    {
                        ret
                    }
                    .method public hidebysig specialname instance void raise_MyEvent() cil managed
                    {
                        ret
                    }
                    .event MyDelegate MyEvent
                    {
                        .addon instance void MyClass::add_MyEvent(class MyDelegate)
                        .removeon instance void MyClass::remove_MyEvent(class MyDelegate)
                        .fire instance void MyClass::raise_MyEvent()
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var eventHandle = reader.EventDefinitions.Single();
            var @event = reader.GetEventDefinition(eventHandle);
            var accessors = @event.GetAccessors();
            string eventTypeName = @event.Type.Kind switch
            {
                HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)@event.Type).Name),
                HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)@event.Type).Name),
                _ => throw new InvalidOperationException($"Unexpected event type handle kind '{@event.Type.Kind}'."),
            };

            Assert.Equal("MyEvent", reader.GetString(@event.Name));
            Assert.Equal("MyDelegate", eventTypeName);
            Assert.Equal("add_MyEvent", reader.GetString(reader.GetMethodDefinition(accessors.Adder).Name));
            Assert.Equal("remove_MyEvent", reader.GetString(reader.GetMethodDefinition(accessors.Remover).Name));
            Assert.Equal("raise_MyEvent", reader.GetString(reader.GetMethodDefinition(accessors.Raiser).Name));
            Assert.Equal(3, reader.GetTableRowCount(TableIndex.MethodSemantics));
        }

        [Fact]
        public void EventAttributesOtherAccessorAndCustomAttribute_EmitMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyDelegate extends [mscorlib]System.MulticastDelegate
                {
                    .method public specialname rtspecialname instance void .ctor(object 'object', native int 'method') runtime managed { }
                    .method public virtual instance void Invoke() runtime managed { }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public specialname instance void add_Changed(class MyDelegate value) cil managed { ret }
                    .method public specialname instance void remove_Changed(class MyDelegate value) cil managed { ret }
                    .method public specialname instance void raise_Changed() cil managed { ret }
                    .method public specialname instance void other_Changed() cil managed { ret }

                    .event specialname rtspecialname MyDelegate Changed
                    {
                        .addon instance void Test::add_Changed(class MyDelegate)
                        .removeon instance void Test::remove_Changed(class MyDelegate)
                        .fire instance void Test::raise_Changed()
                        .other instance void Test::other_Changed()
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var eventHandle = Assert.Single(reader.EventDefinitions);
            var @event = reader.GetEventDefinition(eventHandle);
            var accessors = @event.GetAccessors();

            Assert.True(@event.Attributes.HasFlag(EventAttributes.SpecialName));
            Assert.Equal("add_Changed", reader.GetString(reader.GetMethodDefinition(accessors.Adder).Name));
            Assert.Equal("remove_Changed", reader.GetString(reader.GetMethodDefinition(accessors.Remover).Name));
            Assert.Equal("raise_Changed", reader.GetString(reader.GetMethodDefinition(accessors.Raiser).Name));
            Assert.Equal(
                "other_Changed",
                reader.GetString(reader.GetMethodDefinition(Assert.Single(accessors.Others)).Name));
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(eventHandle))).Value));
        }
    }
}
