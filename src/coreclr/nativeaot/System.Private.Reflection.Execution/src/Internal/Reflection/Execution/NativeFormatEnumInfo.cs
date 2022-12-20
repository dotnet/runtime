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
        private static void GetEnumValuesAndNames(MetadataReader reader, TypeDefinitionHandle typeDefHandle,
            out object[] sortedBoxedValues, out string[] sortedNames, out bool isFlags)
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

            var names = new string[staticFieldCount];
            var boxedValues = new object[staticFieldCount]; // TODO: Avoid boxing the values

            int i = 0;
            foreach (FieldHandle fieldHandle in typeDef.Fields)
            {
                Field field = fieldHandle.GetField(reader);
                if (0 != (field.Flags & FieldAttributes.Static))
                {
                    names[i] = field.Name.GetString(reader);
                    boxedValues[i] = field.DefaultValue.ParseConstantNumericValue(reader);
                    i++;
                }
            }

            // Using object overload to avoid generic expansion for every underlying enum type
            Array.Sort<object, string>(boxedValues, names);

            sortedBoxedValues = boxedValues;
            sortedNames = names;

            isFlags = false;
            foreach (CustomAttributeHandle cah in typeDef.CustomAttributes)
            {
                if (cah.IsCustomAttributeOfType(reader, "System", "FlagsAttribute"))
                    isFlags = true;
            }
        }

        public static EnumInfo<TUnderlyingValue> Create<TUnderlyingValue>(RuntimeTypeHandle typeHandle, MetadataReader reader, TypeDefinitionHandle typeDefHandle)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            GetEnumValuesAndNames(reader, typeDefHandle, out object[] boxedValues, out string[] names, out bool isFlags);

            var values = new TUnderlyingValue[boxedValues.Length];
            for (int i = 0; i < boxedValues.Length; i++)
                values[i] = (TUnderlyingValue)boxedValues[i];

            return new EnumInfo<TUnderlyingValue>(RuntimeAugments.GetEnumUnderlyingType(typeHandle), values, names, isFlags);
        }
    }
}
