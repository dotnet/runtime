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
    }
}
