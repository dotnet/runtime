// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        private MethodDesc _objectEqualsMethod;

        private class ValueTypeMethodHashtable : LockFreeReaderHashtable<DefType, MethodDesc>
        {
            protected override int GetKeyHashCode(DefType key) => key.GetHashCode();
            protected override int GetValueHashCode(MethodDesc value) => value.OwningType.GetHashCode();
            protected override bool CompareKeyToValue(DefType key, MethodDesc value) => key == value.OwningType;
            protected override bool CompareValueToValue(MethodDesc v1, MethodDesc v2) => v1.OwningType == v2.OwningType;

            protected override MethodDesc CreateValueFromKey(DefType key)
            {
                return new ValueTypeGetFieldHelperMethodOverride(key);
            }
        }

        private ValueTypeMethodHashtable _valueTypeMethodHashtable = new ValueTypeMethodHashtable();

        protected virtual IEnumerable<MethodDesc> GetAllMethodsForValueType(TypeDesc valueType, bool virtualOnly)
        {
            TypeDesc valueTypeDefinition = valueType.GetTypeDefinition();

            if (RequiresGetFieldHelperMethod((MetadataType)valueTypeDefinition))
            {
                MethodDesc getFieldHelperMethod = _valueTypeMethodHashtable.GetOrCreateValue((DefType)valueTypeDefinition);

                if (valueType != valueTypeDefinition)
                {
                    yield return GetMethodForInstantiatedType(getFieldHelperMethod, (InstantiatedType)valueType);
                }
                else
                {
                    yield return getFieldHelperMethod;
                }
            }

            IEnumerable<MethodDesc> metadataMethods = virtualOnly ? valueType.GetVirtualMethods() : valueType.GetMethods();
            foreach (MethodDesc method in metadataMethods)
                yield return method;
        }

        private bool RequiresGetFieldHelperMethod(MetadataType valueType)
        {
            if (_objectEqualsMethod == null)
                _objectEqualsMethod = GetWellKnownType(WellKnownType.Object).GetMethod("Equals", null);

            // If the classlib doesn't have Object.Equals, we don't need this.
            if (_objectEqualsMethod == null)
                return false;

            // Byref-like valuetypes cannot be boxed.
            if (valueType.IsByRefLike)
                return false;

            // Enums get their overrides from System.Enum.
            if (valueType.IsEnum)
                return false;

            // These need to provide an implementation of Equals/GetHashCode because of NaN handling.
            // The helper would be useless.
            if (valueType.IsWellKnownType(WellKnownType.Double) || valueType.IsWellKnownType(WellKnownType.Single))
                return false;

            return !_typeStateHashtable.GetOrCreateValue(valueType).CanCompareValueTypeBits;
        }

        private class TypeState
        {
            private enum Flags
            {
                CanCompareValueTypeBits         = 0x0000_0001,
                CanCompareValueTypeBitsComputed = 0x0000_0002,
            }

            private volatile Flags _flags;
            private readonly TypeStateHashtable _hashtable;

            public TypeDesc Type { get; }

            public bool CanCompareValueTypeBits
            {
                get
                {
                    Flags flags = _flags;
                    if ((flags & Flags.CanCompareValueTypeBitsComputed) == 0)
                    {
                        Debug.Assert(Type.IsValueType);
                        if (ComputeCanCompareValueTypeBits((MetadataType)Type))
                            flags |= Flags.CanCompareValueTypeBits;
                        flags |= Flags.CanCompareValueTypeBitsComputed;

                        _flags = flags;
                    }
                    return (flags & Flags.CanCompareValueTypeBits) != 0;
                }
            }

            public TypeState(TypeDesc type, TypeStateHashtable hashtable)
            {
                Type = type;
                _hashtable = hashtable;
            }

            private bool ComputeCanCompareValueTypeBits(MetadataType type)
            {
                Debug.Assert(type.IsValueType);

                if (type.ContainsGCPointers)
                    return false;

                if (type.IsGenericDefinition)
                    return false;

                OverlappingFieldTracker overlappingFieldTracker = new OverlappingFieldTracker(type);

                bool result = true;
                foreach (var field in type.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    if (!overlappingFieldTracker.TrackField(field))
                    {
                        // This field overlaps with another field - can't compare memory
                        result = false;
                        break;
                    }

                    TypeDesc fieldType = field.FieldType;
                    if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType.IsFunctionPointer)
                    {
                        TypeFlags category = fieldType.UnderlyingType.Category;
                        if (category == TypeFlags.Single || category == TypeFlags.Double)
                        {
                            // Double/Single have weird behaviors around negative/positive zero
                            result = false;
                            break;
                        }
                    }
                    else
                    {
                        // Would be a suprise if this wasn't a valuetype. We checked ContainsGCPointers above.
                        Debug.Assert(fieldType.IsValueType);

                        MethodDesc objectEqualsMethod = ((CompilerTypeSystemContext)fieldType.Context)._objectEqualsMethod;

                        // If the field overrides Equals, we can't use the fast helper because we need to call the method.
                        if (fieldType.FindVirtualFunctionTargetMethodOnObjectType(objectEqualsMethod).OwningType == fieldType)
                        {
                            result = false;
                            break;
                        }

                        if (!_hashtable.GetOrCreateValue((MetadataType)fieldType).CanCompareValueTypeBits)
                        {
                            result = false;
                            break;
                        }
                    }
                }

                // If there are gaps, we can't memcompare
                if (result && overlappingFieldTracker.HasGaps)
                    result = false;

                return result;
            }
        }

        private class TypeStateHashtable : LockFreeReaderHashtable<TypeDesc, TypeState>
        {
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(TypeState value) => value.Type.GetHashCode();
            protected override bool CompareKeyToValue(TypeDesc key, TypeState value) => key == value.Type;
            protected override bool CompareValueToValue(TypeState v1, TypeState v2) => v1.Type == v2.Type;

            protected override TypeState CreateValueFromKey(TypeDesc key)
            {
                return new TypeState(key, this);
            }
        }
        private TypeStateHashtable _typeStateHashtable = new TypeStateHashtable();

        private struct OverlappingFieldTracker
        {
            private bool[] _usedBytes;

            public OverlappingFieldTracker(MetadataType type)
            {
                _usedBytes = new bool[type.InstanceFieldSize.AsInt];
            }

            public bool TrackField(FieldDesc field)
            {
                int fieldBegin = field.Offset.AsInt;

                TypeDesc fieldType = field.FieldType;

                int fieldEnd;
                if (fieldType.IsPointer || fieldType.IsFunctionPointer)
                {
                    fieldEnd = fieldBegin + field.Context.Target.PointerSize;
                }
                else
                {
                    Debug.Assert(fieldType.IsValueType);
                    fieldEnd = fieldBegin + ((DefType)fieldType).InstanceFieldSize.AsInt;
                }

                for (int i = fieldBegin; i < fieldEnd; i++)
                {
                    if (_usedBytes[i])
                        return false;
                    _usedBytes[i] = true;
                }

                return true;
            }

            public bool HasGaps
            {
                get
                {
                    for (int i = 0; i < _usedBytes.Length; i++)
                        if (!_usedBytes[i])
                            return true;

                    return false;
                }
            }
        }
    }
}
