// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a row in the Property table.
    /// </summary>
    public sealed class EventDefinitionNode : TokenBasedNode
    {
        public EventDefinitionNode(EcmaModule module, EventDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private EventDefinitionHandle Handle => (EventDefinitionHandle)_handle;

        // TODO: this could be done when reflection-marking a type for better performance.
        TypeDefinitionHandle GetDeclaringType()
        {
            MetadataReader reader = _module.MetadataReader;
            EventAccessors accessors = reader.GetEventDefinition(Handle).GetAccessors();
            MethodDefinitionHandle accessorMethodHandle = !accessors.Remover.IsNil
                ? accessors.Remover
                : accessors.Adder;
            Debug.Assert(!accessorMethodHandle.IsNil);
            return reader.GetMethodDefinition(accessorMethodHandle).GetDeclaringType();
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _module.MetadataReader;

            EventDefinition eventDef = reader.GetEventDefinition(Handle);

            TypeDefinitionHandle declaringTypeHandle = GetDeclaringType();

            TypeDefinition declaringType = reader.GetTypeDefinition(declaringTypeHandle);

            DependencyList dependencies = new DependencyList();

            dependencies.Add(factory.TypeDefinition(_module, declaringTypeHandle), "Event owning type");

            if (!eventDef.Type.IsNil)
                dependencies.Add(factory.GetNodeForToken(_module, eventDef.Type), "Event type");

            foreach (CustomAttributeHandle customAttribute in eventDef.GetCustomAttributes())
            {
                dependencies.Add(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a event");
            }

            EventAccessors accessors = eventDef.GetAccessors();
            if (!accessors.Adder.IsNil)
                dependencies.Add(factory.MethodDefinition(_module, accessors.Adder), "Event adder");
            if (!accessors.Remover.IsNil)
                dependencies.Add(factory.MethodDefinition(_module, accessors.Remover), "Event remover");
            if (!accessors.Raiser.IsNil)
                dependencies.Add(factory.MethodDefinition(_module, accessors.Remover), "Event raiser");
            Debug.Assert(accessors.Others.Length == 0);

            return dependencies;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;

            EventDefinition eventDef = reader.GetEventDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            EventDefinitionHandle targetEventHandle = builder.AddEvent(
                eventDef.Attributes,
                builder.GetOrAddString(reader.GetString(eventDef.Name)),
                writeContext.TokenMap.MapToken(eventDef.Type));

            // Add MethodSemantics rows to link properties with accessor methods.
            // MethodSemantics rows may be added in any order.
            EventAccessors accessors = eventDef.GetAccessors();
            if (!accessors.Adder.IsNil)
            {
                builder.AddMethodSemantics(
                    targetEventHandle,
                    MethodSemanticsAttributes.Adder,
                    (MethodDefinitionHandle)writeContext.TokenMap.MapToken(accessors.Adder));
            }
            if (!accessors.Remover.IsNil)
            {
                builder.AddMethodSemantics(
                    targetEventHandle,
                    MethodSemanticsAttributes.Remover,
                    (MethodDefinitionHandle)writeContext.TokenMap.MapToken(accessors.Remover));
            }
            if (!accessors.Raiser.IsNil)
            {
                builder.AddMethodSemantics(
                    targetEventHandle,
                    MethodSemanticsAttributes.Raiser,
                    (MethodDefinitionHandle)writeContext.TokenMap.MapToken(accessors.Raiser));
            }

            return targetEventHandle;
        }

        public override string ToString()
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetEventDefinition(Handle).Name);
        }
    }
}
