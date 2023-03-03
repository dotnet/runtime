// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using MethodSemanticsAttributes = Internal.Metadata.NativeFormat.MethodSemanticsAttributes;
using CallingConventions = System.Reflection.CallingConventions;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private Property HandleProperty(Cts.Ecma.EcmaModule module, Ecma.PropertyDefinitionHandle property)
        {
            Ecma.MetadataReader reader = module.MetadataReader;

            Ecma.PropertyDefinition propDef = reader.GetPropertyDefinition(property);

            Ecma.PropertyAccessors acc = propDef.GetAccessors();
            Cts.MethodDesc getterMethod = acc.Getter.IsNil ? null : module.GetMethod(acc.Getter);
            Cts.MethodDesc setterMethod = acc.Setter.IsNil ? null : module.GetMethod(acc.Setter);

            bool getterHasMetadata = getterMethod != null && _policy.GeneratesMetadata(getterMethod);
            bool setterHasMetadata = setterMethod != null && _policy.GeneratesMetadata(setterMethod);

            // Policy: If neither the getter nor setter have metadata, property doesn't have metadata
            if (!getterHasMetadata && !setterHasMetadata)
                return null;

            Ecma.BlobReader sigBlobReader = reader.GetBlobReader(propDef.Signature);
            Cts.PropertySignature sig = new Cts.Ecma.EcmaSignatureParser(module, sigBlobReader, Cts.NotFoundBehavior.Throw).ParsePropertySignature();

            Property result = new Property
            {
                Name = HandleString(reader.GetString(propDef.Name)),
                Flags = propDef.Attributes,
                Signature = new PropertySignature
                {
                    CallingConvention = sig.IsStatic ? CallingConventions.Standard : CallingConventions.HasThis,
                    Type = HandleType(sig.ReturnType)
                },
            };

            result.Signature.Parameters.Capacity = sig.Length;
            for (int i = 0; i < sig.Length; i++)
                result.Signature.Parameters.Add(HandleType(sig[i]));

            if (getterHasMetadata)
            {
                result.MethodSemantics.Add(new MethodSemantics
                {
                    Attributes = MethodSemanticsAttributes.Getter,
                    Method = HandleMethodDefinition(getterMethod),
                });
            }

            if (setterHasMetadata)
            {
                result.MethodSemantics.Add(new MethodSemantics
                {
                    Attributes = MethodSemanticsAttributes.Setter,
                    Method = HandleMethodDefinition(setterMethod),
                });
            }

            Ecma.ConstantHandle defaultValue = propDef.GetDefaultValue();
            if (!defaultValue.IsNil)
            {
                result.DefaultValue = HandleConstant(module, defaultValue);
            }

            Ecma.CustomAttributeHandleCollection customAttributes = propDef.GetCustomAttributes();
            if (customAttributes.Count > 0)
            {
                result.CustomAttributes = HandleCustomAttributes(module, customAttributes);
            }

            return result;
        }

    }
}
