// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;
using System.Collections.Generic;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    public static partial class CastingHelper
    {
        static partial void IsEquivalentTo(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect visited, ref bool isEquivalentTo)
        {
            isEquivalentTo = IsEquivalentToHelper(thisType, otherType, visited);
            return;
        }

        private static bool IsEquivalentToHelper(TypeDesc thisType, TypeDesc otherType, StackOverflowProtect visited)
        {
            if (thisType == otherType)
                return true;

            if (thisType.Category != otherType.Category)
                return false;

            switch (thisType.Category)
            {
                case TypeFlags.SignatureTypeVariable:
                case TypeFlags.SignatureMethodVariable:
                case TypeFlags.GenericParameter:
                    return false;

                case TypeFlags.Array:
                    var arrayType = (ArrayType)thisType;
                    var otherArrayType = (ArrayType)otherType;
                    if (arrayType.Rank != otherArrayType.Rank)
                        return false;
                    return arrayType.ParameterType.IsEquivalentTo(otherArrayType.ParameterType, visited);

                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                    return ((ParameterizedType)thisType).ParameterType.IsEquivalentTo(((ParameterizedType)otherType).ParameterType, visited);

                case TypeFlags.FunctionPointer:
                    return false;

                default:
                    Debug.Assert(thisType.IsDefType);
                    if (!thisType.IsTypeDefEquivalent || !otherType.IsTypeDefEquivalent)
                    {
                        if (thisType.HasInstantiation && otherType.HasInstantiation)
                        {
                            // We might be in the generic interface case
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return ((DefType)thisType).IsEquivalentToDefType((DefType)otherType, visited);
            }
        }

        private static bool IsEquivalentToDefType(this DefType thisType, DefType otherType, StackOverflowProtect visited)
        {
            if (thisType.HasInstantiation)
            {
                // Limit equivalence on generics only to interfaces
                if (!thisType.IsInterface || !otherType.IsInterface)
                {
                    return false;
                }

                if (thisType.Instantiation.Length != otherType.Instantiation.Length)
                {
                    return false;
                }

                // Generic equivalence only allows the instantiation to be non-equal
                if (!thisType.HasSameTypeDefinition(otherType))
                    return false;

                for (int i = 0; i < thisType.Instantiation.Length; i++)
                {
                    if (!thisType.Instantiation[i].IsEquivalentTo(otherType.Instantiation[i], visited))
                    {
                        return false;
                    }
                }

                return true;
            }

            return IsEquivalentTo_TypeDefinition((MetadataType)thisType.GetTypeDefinition(), (MetadataType)otherType.GetTypeDefinition(), visited);

            static bool IsEquivalentTo_TypeDefinition(MetadataType type1, MetadataType type2, StackOverflowProtect visited)
            {
                Debug.Assert(type1.GetTypeDefinition() == type1);
                Debug.Assert(type2.GetTypeDefinition() == type2);

                var stackOverflowProtectKey = new CastingPair(type1, type2);
                if (visited != null)
                {
                    if (visited.Contains(stackOverflowProtectKey))
                    {
                        // we are in the process of comparing these tokens already. Assume success
                        return true;
                    }
                }

                StackOverflowProtect protect = new StackOverflowProtect(stackOverflowProtectKey, visited);

                TypeIdentifierData data1 = type1.TypeIdentifierData;
                TypeIdentifierData data2 = type2.TypeIdentifierData;
                if (data1 == null || data2 == null)
                {
                    return false;
                }

                // Check to ensure that the types are actually opted into equivalence
                if (!type1.IsTypeDefEquivalent || !type2.IsTypeDefEquivalent)
                    return false;

                if (!data1.Equals(data2))
                    return false;

                if (type1.Name != type2.Name)
                    return false;

                if (type1.Namespace != type2.Namespace)
                    return false;

                var containingType1 = (MetadataType)type1.ContainingType;
                var containingType2 = (MetadataType)type2.ContainingType;

                // Types must be either not nested, or nested in equivalent types
                if ((containingType1 == null) != (containingType2 == null))
                {
                    return false;
                }

                if ((containingType1 != null) && !IsEquivalentTo_TypeDefinition(containingType1, containingType2, visited))
                {
                    return false;
                }

                if (type1.IsInterface != type2.IsInterface)
                {
                    return false;
                }
                if (type1.IsInterface)
                {
                    return true;
                }
                if ((type1.IsEnum != type2.IsEnum) || (type1.IsValueType != type2.IsValueType))
                {
                    return false;
                }

                if (type1.IsEnum)
                {
                    return CompareStructuresForEquivalence(type1, type2, visited, enumMode: true);
                }
                else if (type1.IsValueType)
                {
                    return CompareStructuresForEquivalence(type1, type2, visited, enumMode: false);
                }
                else if ((type1.IsDelegate == type2.IsDelegate) && type1.IsDelegate)
                {
                    return CompareDelegatesForEquivalence(type1, type2, visited);
                }

                return false;
            }

            static bool CompareDelegatesForEquivalence(MetadataType type1, MetadataType type2, StackOverflowProtect visited)
            {
                var invoke1 = type1.GetMethod("Invoke", null);
                var invoke2 = type2.GetMethod("Invoke", null);

                if (invoke1 == null)
                    return false;

                if (invoke2 == null)
                    return false;

                return invoke1.Signature.EquivalentTo(invoke2.Signature, visited);
            }

            static bool CompareStructuresForEquivalence(MetadataType type1, MetadataType type2, StackOverflowProtect visited, bool enumMode)
            {
                foreach (var method in type1.GetMethods())
                {
                    // If there are any methods, then it isn't actually a type-equivalent type
                    return false;
                }

                foreach (var method in type2.GetMethods())
                {
                    // If there are any methods, then it isn't actually a type-equivalent type
                    return false;
                }

                // Compare field types for equivalence
                var fields1 = type1.GetFields().GetEnumerator();
                var fields2 = type2.GetFields().GetEnumerator();

                while (true)
                {
                    bool nonTypeEquivalentValidFieldFound;

                    FieldDesc field1 = GetNextTypeEquivalentField(fields1, enumMode, out nonTypeEquivalentValidFieldFound);
                    if (nonTypeEquivalentValidFieldFound)
                        return false;
                    FieldDesc field2 = GetNextTypeEquivalentField(fields2, enumMode, out nonTypeEquivalentValidFieldFound);
                    if (nonTypeEquivalentValidFieldFound)
                        return false;

                    if ((field1 == null) && (field2 == null))
                    {
                        // We ran out of fields on both types before finding a failure
                        break;
                    }

                    if ((field1 == null) || (field2 == null))
                    {
                        // we ran out of fields on 1 type.
                        return false;
                    }

                    // Compare the field signatures for equivalence
                    // TODO: Technically this comparison should include custom modifiers on the field signatures
                    if (!field1.FieldType.IsEquivalentTo(field2.FieldType, visited))
                    {
                        return false;
                    }

                    // Compare the field marshal details
                    var marshalAsDescriptor1 = field1.GetMarshalAsDescriptor();
                    var marshalAsDescriptor2 = field2.GetMarshalAsDescriptor();

                    if (marshalAsDescriptor1 == null || marshalAsDescriptor2 == null)
                    {
                        if (marshalAsDescriptor1 != marshalAsDescriptor2)
                            return false;
                    }
                    else if (!marshalAsDescriptor1.Equals(marshalAsDescriptor2))
                    {
                        return false;
                    }
                }

                // At this point we know that the set of fields is the same, and have the same types
                if (!enumMode)
                {
                    if (!CompareTypeLayout(type1, type2))
                    {
                        return false;
                    }
                }
                return true;

                static bool CompareTypeLayout(MetadataType type1, MetadataType type2)
                {
                    // Types must either be Sequential or Explicit layout
                    if (type1.IsSequentialLayout != type2.IsSequentialLayout)
                    {
                        return false;
                    }

                    if (type1.IsExplicitLayout != type2.IsExplicitLayout)
                    {
                        return false;
                    }

                    if (!(type1.IsSequentialLayout || type1.IsExplicitLayout))
                    {
                        return false;
                    }

                    bool explicitLayout = type1.IsExplicitLayout;

                    // they must have the same charset
                    if (type1.PInvokeStringFormat != type2.PInvokeStringFormat)
                    {
                        return false;
                    }

                    var layoutMetadata1 = type1.GetClassLayout();
                    var layoutMetadata2 = type2.GetClassLayout();
                    if ((layoutMetadata1.PackingSize != layoutMetadata2.PackingSize) ||
                        (layoutMetadata1.Size != layoutMetadata2.Size))
                        return false;

                    if ((explicitLayout) && !(layoutMetadata1.Offsets == null && layoutMetadata2.Offsets == null))
                    {
                        if (layoutMetadata1.Offsets == null)
                            return false;

                        if (layoutMetadata2.Offsets == null)
                            return false;

                        for (int index = 0; index < layoutMetadata1.Offsets.Length; index++)
                        {
                            if (layoutMetadata1.Offsets[index].Offset != layoutMetadata2.Offsets[index].Offset)
                                return false;
                        }
                    }

                    return true;
                }

                static FieldDesc GetNextTypeEquivalentField(IEnumerator<FieldDesc> fieldEnum, bool enumMode, out bool fieldNotValidInEquivalentTypeFound)
                {
                    fieldNotValidInEquivalentTypeFound = false;
                    while (fieldEnum.MoveNext())
                    {
                        var field = fieldEnum.Current;

                        if (field.GetAttributeEffectiveVisibility() == EffectiveVisibility.Public && !field.IsStatic)
                            return field;

                        // Only public instance fields, and literal fields on enums are permitted in type equivalent structures
                        if (!enumMode || !field.IsLiteral)
                        {
                            fieldNotValidInEquivalentTypeFound = true;
                            return null;
                        }
                    }
                    return null;
                }
            }
        }
    }
}
