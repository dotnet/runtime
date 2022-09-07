// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using MethodSemanticsAttributes = Internal.Metadata.NativeFormat.MethodSemanticsAttributes;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private Event HandleEvent(Cts.Ecma.EcmaModule module, Ecma.EventDefinitionHandle eventHandle)
        {
            Ecma.MetadataReader reader = module.MetadataReader;

            Ecma.EventDefinition eventDef = reader.GetEventDefinition(eventHandle);

            Ecma.EventAccessors acc = eventDef.GetAccessors();
            Cts.MethodDesc adderMethod = acc.Adder.IsNil ? null : module.GetMethod(acc.Adder);
            Cts.MethodDesc raiserMethod = acc.Raiser.IsNil ? null : module.GetMethod(acc.Raiser);
            Cts.MethodDesc removerMethod = acc.Remover.IsNil ? null : module.GetMethod(acc.Remover);

            bool adderHasMetadata = adderMethod != null && _policy.GeneratesMetadata(adderMethod);
            bool raiserHasMetadata = raiserMethod != null && _policy.GeneratesMetadata(raiserMethod);
            bool removerHasMetadata = removerMethod != null && _policy.GeneratesMetadata(removerMethod);

            // Policy: If none of the accessors has metadata, event doesn't have metadata
            if (!adderHasMetadata && !raiserHasMetadata && !removerHasMetadata)
                return null;

            Event result = new Event
            {
                Name = HandleString(reader.GetString(eventDef.Name)),
                Flags = eventDef.Attributes,
                Type = HandleType(module.GetType(eventDef.Type)),
            };

            if (adderHasMetadata)
            {
                result.MethodSemantics.Add(new MethodSemantics
                {
                    Attributes = MethodSemanticsAttributes.AddOn,
                    Method = HandleMethodDefinition(adderMethod),
                });
            }

            if (raiserHasMetadata)
            {
                result.MethodSemantics.Add(new MethodSemantics
                {
                    Attributes = MethodSemanticsAttributes.Fire,
                    Method = HandleMethodDefinition(raiserMethod),
                });
            }

            if (removerHasMetadata)
            {
                result.MethodSemantics.Add(new MethodSemantics
                {
                    Attributes = MethodSemanticsAttributes.RemoveOn,
                    Method = HandleMethodDefinition(removerMethod),
                });
            }

            Ecma.CustomAttributeHandleCollection customAttributes = eventDef.GetCustomAttributes();
            if (customAttributes.Count > 0)
            {
                result.CustomAttributes = HandleCustomAttributes(module, customAttributes);
            }

            return result;
        }

    }
}
