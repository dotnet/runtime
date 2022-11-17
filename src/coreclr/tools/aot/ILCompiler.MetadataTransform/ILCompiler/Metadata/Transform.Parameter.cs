// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using GenericParameterKind = Internal.Metadata.NativeFormat.GenericParameterKind;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private GenericParameter HandleGenericParameter(Cts.GenericParameterDesc genParam)
        {
            var result = new GenericParameter
            {
                Kind = genParam.Kind == Cts.GenericParameterKind.Type ?
                    GenericParameterKind.GenericTypeParameter : GenericParameterKind.GenericMethodParameter,
                Number = checked((ushort)genParam.Index),
            };

            foreach (Cts.TypeDesc constraint in genParam.TypeConstraints)
            {
                result.Constraints.Add(HandleType(constraint));
            }

            var ecmaGenParam = genParam as Cts.Ecma.EcmaGenericParameter;
            if (ecmaGenParam != null)
            {
                Ecma.MetadataReader reader = ecmaGenParam.MetadataReader;
                Ecma.GenericParameter genParamDef = reader.GetGenericParameter(ecmaGenParam.Handle);

                result.Flags = genParamDef.Attributes;
                result.Name = HandleString(reader.GetString(genParamDef.Name));

                Ecma.CustomAttributeHandleCollection customAttributes = genParamDef.GetCustomAttributes();
                if (customAttributes.Count > 0)
                {
                    result.CustomAttributes = HandleCustomAttributes(ecmaGenParam.Module, customAttributes);
                }
            }
            else
                throw new NotImplementedException();

            return result;
        }
    }
}
