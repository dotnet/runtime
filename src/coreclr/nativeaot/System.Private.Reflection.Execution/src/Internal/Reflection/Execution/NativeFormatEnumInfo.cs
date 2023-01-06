// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.Runtime.Augments;
using Internal.Metadata.NativeFormat;

namespace Internal.Reflection.Execution
{
    static class NativeFormatEnumInfo
    {
        public static void GetEnumValuesAndNames(MetadataReader reader, TypeDefinitionHandle typeDefHandle,
            out object[] unsortedBoxedValues, out string[] unsortedNames, out bool isFlags)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);

            // Count the number of static fields. The single instance field may or may not have metadata,
            // so using `typeDef.Fields.Count - 1` is not reliable.
            int staticFieldCount = 0;
            foreach (FieldHandle fieldHandle in typeDef.Fields)
            {
                Field field = fieldHandle.GetField(reader);
                if (0 != (field.Flags & FieldAttributes.Static))
                {
                    staticFieldCount++;
                }
            }

            unsortedNames = new string[staticFieldCount];
            unsortedBoxedValues = new object[staticFieldCount]; // TODO: Avoid boxing the values

            int i = 0;
            foreach (FieldHandle fieldHandle in typeDef.Fields)
            {
                Field field = fieldHandle.GetField(reader);
                if (0 != (field.Flags & FieldAttributes.Static))
                {
                    unsortedNames[i] = field.Name.GetString(reader);
                    unsortedBoxedValues[i] = field.DefaultValue.ParseConstantNumericValue(reader);
                    i++;
                }
            }

            isFlags = false;
            foreach (CustomAttributeHandle cah in typeDef.CustomAttributes)
            {
                if (cah.IsCustomAttributeOfType(reader, "System", "FlagsAttribute"))
                    isFlags = true;
            }
        }
    }
}
