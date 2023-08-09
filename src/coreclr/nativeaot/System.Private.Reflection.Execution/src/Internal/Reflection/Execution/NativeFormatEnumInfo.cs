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
                    var handle = field.DefaultValue;
                    unsortedBoxedValues[i] = handle.HandleType switch
                    {
                        HandleType.ConstantSByteValue => (object)(byte)handle.ToConstantSByteValueHandle(reader).GetConstantSByteValue(reader).Value,
                        HandleType.ConstantByteValue => handle.ToConstantByteValueHandle(reader).GetConstantByteValue(reader).Value,
                        HandleType.ConstantInt16Value => (ushort)handle.ToConstantInt16ValueHandle(reader).GetConstantInt16Value(reader).Value,
                        HandleType.ConstantUInt16Value => handle.ToConstantUInt16ValueHandle(reader).GetConstantUInt16Value(reader).Value,
                        HandleType.ConstantInt32Value => (uint)handle.ToConstantInt32ValueHandle(reader).GetConstantInt32Value(reader).Value,
                        HandleType.ConstantUInt32Value => handle.ToConstantUInt32ValueHandle(reader).GetConstantUInt32Value(reader).Value,
                        HandleType.ConstantInt64Value => (ulong)handle.ToConstantInt64ValueHandle(reader).GetConstantInt64Value(reader).Value,
                        HandleType.ConstantUInt64Value => handle.ToConstantUInt64ValueHandle(reader).GetConstantUInt64Value(reader).Value,
                        _ => throw new InvalidOperationException(), // unreachable - we would have thrown InvalidOperationException earlier
                    };
                    i++;
                }
            }

            isFlags = false;
            foreach (CustomAttributeHandle cah in typeDef.CustomAttributes)
            {
                if (cah.IsCustomAttributeOfType(reader, "System", "FlagsAttribute"))
                {
                    isFlags = true;
                    break;
                }
            }
        }
    }
}
