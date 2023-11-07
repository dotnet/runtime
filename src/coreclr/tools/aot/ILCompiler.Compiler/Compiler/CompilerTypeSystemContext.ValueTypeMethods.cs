// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private MethodDesc _objectEqualsMethod;
        private MetadataType _iAsyncStateMachineType;

        private sealed class ValueTypeMethodHashtable : LockFreeReaderHashtable<DefType, MethodDesc>
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

            if (RequiresValueTypeGetFieldHelperMethod((MetadataType)valueTypeDefinition))
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

        protected virtual IEnumerable<MethodDesc> GetAllMethodsForAttribute(TypeDesc attributeType, bool virtualOnly)
        {
            TypeDesc attributeTypeDefinition = attributeType.GetTypeDefinition();

            if (RequiresAttributeGetFieldHelperMethod(attributeTypeDefinition))
            {
                MethodDesc getFieldHelperMethod = _valueTypeMethodHashtable.GetOrCreateValue((DefType)attributeTypeDefinition);

                if (attributeType != attributeTypeDefinition)
                {
                    yield return GetMethodForInstantiatedType(getFieldHelperMethod, (InstantiatedType)attributeType);
                }
                else
                {
                    yield return getFieldHelperMethod;
                }
            }

            IEnumerable<MethodDesc> metadataMethods = virtualOnly ? attributeType.GetVirtualMethods() : attributeType.GetMethods();
            foreach (MethodDesc method in metadataMethods)
                yield return method;
        }

        private bool RequiresValueTypeGetFieldHelperMethod(MetadataType valueType)
        {
            _objectEqualsMethod ??= GetWellKnownType(WellKnownType.Object).GetMethod("Equals", null);

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

            // Heuristic: async state machines don't need equality/hashcode.
            if (IsAsyncStateMachineType(valueType))
                return false;

            return !_typeStateHashtable.GetOrCreateValue(valueType).CanCompareValueTypeBits;
        }

        public bool IsAsyncStateMachineType(MetadataType type)
        {
            Debug.Assert(type.IsValueType);
            _iAsyncStateMachineType ??= SystemModule.GetType("System.Runtime.CompilerServices", "IAsyncStateMachine", throwIfNotFound: false);
            return type.HasCustomAttribute("System.Runtime.CompilerServices", "CompilerGeneratedAttribute")
                && Array.IndexOf(type.RuntimeInterfaces, _iAsyncStateMachineType) >= 0;
        }

        private bool RequiresAttributeGetFieldHelperMethod(TypeDesc attributeTypeDef)
        {
            _objectEqualsMethod ??= GetWellKnownType(WellKnownType.Object).GetMethod("Equals", null);

            // If the classlib doesn't have Object.Equals, we don't need this.
            if (_objectEqualsMethod == null)
                return false;

            foreach (FieldDesc field in attributeTypeDef.GetFields())
            {
                if (field.IsStatic)
                    continue;

                return true;
            }

            return false;
        }

        private sealed class TypeState
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
                        MetadataType mdType = (MetadataType)Type;
                        if (ComparerIntrinsics.CanCompareValueTypeBits(mdType, ((CompilerTypeSystemContext)mdType.Context)._objectEqualsMethod))
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
        }

        private sealed class TypeStateHashtable : LockFreeReaderHashtable<TypeDesc, TypeState>
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
    }
}
