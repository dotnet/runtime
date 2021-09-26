// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Debug = System.Diagnostics.Debug;

namespace ILTrim
{
    // Set of methods that help with mapping various *List fields of metadata records.
    // As an example of a *List field, consider the FieldList field on the TypeDef record.
    //
    // The value of the FieldList of a particular TypeDef record captures the FieldDef
    // token of the first field of the type. If there's no field, the FieldList field
    // captures the first token of a field of the following type.
    //
    // The number of fields on a type can be quickly calculated by subtracting the value
    // of FieldList of the current type from the FieldList value of next type. For the last
    // type, we look at number of record in the field table instead of FieldList of the next
    // type.
    //
    //                                                    Field table
    //
    //               TypeDef table                       +-----------+
    //                                                ---+___________| 1
    //               +----------------|---+   -------/   |___________| 2
    //             1 |________________|_1_+--/           |___________| 3
    //             2 |________________|_4_+--------------+___________| 4
    //             3 |________________|_4_+----------/   |___________| 5
    //             4 |                | 7 +------\       |___________| 6
    //               +----------------|---+       -------+           | 7
    //                                  ^                +-----------+
    //                                  |
    //                                  |
    //                                  |
    //                           FieldList column
    //
    // In the table above, TypeDef 1 owns fields 1, 2, 3. TypeDef 2 owns no fields,
    // TypeDef 3 owns fields 4, 5, 6 and TypeDef 4 owns field 7.
    //
    // The problem with building a TokenMap for this is twofold:
    // 1. We cannot simply call MapToken on the value in the FieldList record because
    //    the field in question might not have been marked and we don't have an output
    //    token for it. If that's the case, we need to find the next field record with
    //    a mapping.
    // 2. System.Reflection.Metadata abstracts away the FieldList record and we can't get
    //    a token to begin with. We can enumerate the fields on the on the type, but if
    //    the type has no fields, we have no choice but to look at the fields of the next
    //    type until we find a field.
    partial class TokenMap
    {
        public MethodDefinitionHandle MapTypeMethodList(TypeDefinitionHandle typeDefHandle)
        {
            // Grabbing the mapped token to use as a MethodList is awakward because
            // S.R.Metadata abstracts away the raw value of the MethodList field of the record.
            //
            // All we can do is enumerate the methods on the type, but that might not be useful
            // if the type has no methods. If the type has no methods, we need to produce
            // the token of the first method of the next type with methods.

            MethodDefinitionHandle sourceToken = default;
            for (TypeDefinitionHandle currentType = typeDefHandle;
                MetadataTokens.GetRowNumber(currentType) <= _reader.GetTableRowCount(TableIndex.TypeDef);
                currentType = MetadataTokens.TypeDefinitionHandle(MetadataTokens.GetRowNumber(currentType) + 1))
            {
                MethodDefinitionHandleCollection methodList = _reader.GetTypeDefinition(currentType).GetMethods();
                if (methodList.Count > 0)
                {
                    var enumerator = methodList.GetEnumerator();
                    enumerator.MoveNext();
                    sourceToken = enumerator.Current;
                    break;
                }
            }

            MethodDefinitionHandle result = default;
            if (!sourceToken.IsNil)
            {
                // We got a token in the source assembly, but the token might not be present in the output.
                // We need to find the first token starting with this one that is part of the output.
                for (int currentMethodRow = MetadataTokens.GetRowNumber(sourceToken);
                    currentMethodRow < _tokenMap[(int)TableIndex.MethodDef].Length;
                    currentMethodRow++)
                {
                    EntityHandle currentMethod = _tokenMap[(int)TableIndex.MethodDef][currentMethodRow];
                    if (!currentMethod.IsNil)
                    {
                        result = (MethodDefinitionHandle)currentMethod;
                        break;
                    }
                }
            }

            // If no type after this type has marked methods, we return the number of total methods
            // in the output plus 1
            if (result.IsNil)
            {
                result = MetadataTokens.MethodDefinitionHandle(_preAssignedRowIdsPerTable[(int)TableIndex.MethodDef] + 1);
            }

            return result;
        }

        public FieldDefinitionHandle MapTypeFieldList(TypeDefinitionHandle typeDefHandle)
        {
            // Grabbing the mapped token to use as a FieldList is awakward because
            // S.R.Metadata abstracts away the raw value of the FieldList field of the record.
            //
            // All we can do is enumerate the fields on the type, but that might not be useful
            // if the type has no fields. If the type has no fields, we need to produce
            // the token of the first field of the next type with fields.

            FieldDefinitionHandle sourceToken = default;
            for (TypeDefinitionHandle currentType = typeDefHandle;
                MetadataTokens.GetRowNumber(currentType) <= _reader.GetTableRowCount(TableIndex.TypeDef);
                currentType = MetadataTokens.TypeDefinitionHandle(MetadataTokens.GetRowNumber(currentType) + 1))
            {
                FieldDefinitionHandleCollection fieldList = _reader.GetTypeDefinition(currentType).GetFields();
                if (fieldList.Count > 0)
                {
                    var enumerator = fieldList.GetEnumerator();
                    enumerator.MoveNext();
                    sourceToken = enumerator.Current;
                    break;
                }
            }

            FieldDefinitionHandle result = default;
            if (!sourceToken.IsNil)
            {
                // We got a token in the source assembly, but the token might not be present in the output.
                // We need to find the first token starting with this one that is part of the output.
                for (int currentFieldRow = MetadataTokens.GetRowNumber(sourceToken);
                    currentFieldRow < _tokenMap[(int)TableIndex.Field].Length;
                    currentFieldRow++)
                {
                    EntityHandle currentField = _tokenMap[(int)TableIndex.Field][currentFieldRow];
                    if (!currentField.IsNil)
                    {
                        result = (FieldDefinitionHandle)currentField;
                        break;
                    }
                }
            }

            // If no type after this type has marked fields, we return the number of total fields
            // in the output plus 1
            if (result.IsNil)
            {
                result = MetadataTokens.FieldDefinitionHandle(_preAssignedRowIdsPerTable[(int)TableIndex.Field] + 1);
            }

            return result;
        }

        public ParameterHandle MapMethodParamList(MethodDefinitionHandle methodDefHandle)
        {
            // Grabbing the mapped token to use as a ParameterList is awakward because
            // S.R.Metadata abstracts away the raw value of the ParameterList field of the record.
            //
            // All we can do is enumerate the parameters on the method, but that might not be useful
            // if the method has no parameters. If the method has no parameters, we need to produce
            // the token of the first parameter of the next method with parameters.

            ParameterHandle sourceToken = default;
            for (MethodDefinitionHandle currentMethod = methodDefHandle;
                MetadataTokens.GetRowNumber(currentMethod) <= _reader.GetTableRowCount(TableIndex.MethodDef);
                currentMethod = MetadataTokens.MethodDefinitionHandle(MetadataTokens.GetRowNumber(currentMethod) + 1))
            {
                ParameterHandleCollection parameterList = _reader.GetMethodDefinition(currentMethod).GetParameters();
                if (parameterList.Count > 0)
                {
                    var enumerator = parameterList.GetEnumerator();
                    enumerator.MoveNext();
                    sourceToken = enumerator.Current;
                    break;
                }
            }

            ParameterHandle result = default;
            if (!sourceToken.IsNil)
            {
                // We got a token in the source assembly, but the token might not be present in the output.
                // We need to find the first token starting with this one that is part of the output.
                for (int currentParameterRow = MetadataTokens.GetRowNumber(sourceToken);
                    currentParameterRow < _tokenMap[(int)TableIndex.Param].Length;
                    currentParameterRow++)
                {
                    EntityHandle currentParameter = _tokenMap[(int)TableIndex.Param][currentParameterRow];
                    if (!currentParameter.IsNil)
                    {
                        result = (ParameterHandle)currentParameter;
                        break;
                    }
                }
            }

            // If no method after this method has marked parameters, we return the number of total parameters
            // in the output plus 1
            if (result.IsNil)
            {
                result = MetadataTokens.ParameterHandle(_preAssignedRowIdsPerTable[(int)TableIndex.Param] + 1);
            }

            return result;
        }
    }
}
