// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public partial class TypeSystemContext
    {
        private struct RuntimeDeterminedTypeKey
        {
            private DefType _plainCanonType;
            private GenericParameterDesc _detailsType;

            public RuntimeDeterminedTypeKey(DefType plainCanonType, GenericParameterDesc detailsType)
            {
                _plainCanonType = plainCanonType;
                _detailsType = detailsType;
            }

            public sealed class RuntimeDeterminedTypeKeyHashtable : LockFreeReaderHashtable<RuntimeDeterminedTypeKey, RuntimeDeterminedType>
            {
                protected override int GetKeyHashCode(RuntimeDeterminedTypeKey key)
                {
                    return key._detailsType.GetHashCode() ^ key._plainCanonType.GetHashCode();
                }

                protected override int GetValueHashCode(RuntimeDeterminedType value)
                {
                    return value.RuntimeDeterminedDetailsType.GetHashCode() ^ value.CanonicalType.GetHashCode();
                }

                protected override bool CompareKeyToValue(RuntimeDeterminedTypeKey key, RuntimeDeterminedType value)
                {
                    return key._detailsType == value.RuntimeDeterminedDetailsType && key._plainCanonType == value.CanonicalType;
                }

                protected override bool CompareValueToValue(RuntimeDeterminedType value1, RuntimeDeterminedType value2)
                {
                    return value1.RuntimeDeterminedDetailsType == value2.RuntimeDeterminedDetailsType
                        && value1.CanonicalType == value2.CanonicalType;
                }

                protected override RuntimeDeterminedType CreateValueFromKey(RuntimeDeterminedTypeKey key)
                {
                    return new RuntimeDeterminedType(key._plainCanonType, key._detailsType);
                }
            }
        }

        private RuntimeDeterminedTypeKey.RuntimeDeterminedTypeKeyHashtable _runtimeDeterminedTypes = new RuntimeDeterminedTypeKey.RuntimeDeterminedTypeKeyHashtable();

        public RuntimeDeterminedType GetRuntimeDeterminedType(DefType plainCanonType, GenericParameterDesc detailsType)
        {
            return _runtimeDeterminedTypes.GetOrCreateValue(new RuntimeDeterminedTypeKey(plainCanonType, detailsType));
        }

        protected internal virtual TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
        {
            throw new NotSupportedException();
        }

        //
        // Methods for runtime determined type
        //

        private struct MethodForRuntimeDeterminedTypeKey
        {
            private MethodDesc _typicalMethodDef;
            private RuntimeDeterminedType _rdType;
            private int _hashcode;

            public MethodForRuntimeDeterminedTypeKey(MethodDesc typicalMethodDef, RuntimeDeterminedType rdType)
            {
                _typicalMethodDef = typicalMethodDef;
                _rdType = rdType;
                _hashcode = TypeHashingAlgorithms.ComputeMethodHashCode(rdType.CanonicalType.GetHashCode(), TypeHashingAlgorithms.ComputeNameHashCode(typicalMethodDef.Name));
            }

            public MethodDesc TypicalMethodDef
            {
                get
                {
                    return _typicalMethodDef;
                }
            }

            public RuntimeDeterminedType RDType
            {
                get
                {
                    return _rdType;
                }
            }

            public sealed class MethodForRuntimeDeterminedTypeKeyHashtable : LockFreeReaderHashtable<MethodForRuntimeDeterminedTypeKey, MethodForRuntimeDeterminedType>
            {
                protected override int GetKeyHashCode(MethodForRuntimeDeterminedTypeKey key)
                {
                    return key._hashcode;
                }

                protected override int GetValueHashCode(MethodForRuntimeDeterminedType value)
                {
                    return value.GetHashCode();
                }

                protected override bool CompareKeyToValue(MethodForRuntimeDeterminedTypeKey key, MethodForRuntimeDeterminedType value)
                {
                    if (key._typicalMethodDef != value.GetTypicalMethodDefinition())
                        return false;

                    return key._rdType == value.OwningType;
                }

                protected override bool CompareValueToValue(MethodForRuntimeDeterminedType value1, MethodForRuntimeDeterminedType value2)
                {
                    return (value1.GetTypicalMethodDefinition() == value2.GetTypicalMethodDefinition()) && (value1.OwningType == value2.OwningType);
                }

                protected override MethodForRuntimeDeterminedType CreateValueFromKey(MethodForRuntimeDeterminedTypeKey key)
                {
                    return new MethodForRuntimeDeterminedType(key.TypicalMethodDef, key.RDType, key._hashcode);
                }
            }
        }

        private MethodForRuntimeDeterminedTypeKey.MethodForRuntimeDeterminedTypeKeyHashtable _methodForRDTypes = new MethodForRuntimeDeterminedTypeKey.MethodForRuntimeDeterminedTypeKeyHashtable();

        public MethodDesc GetMethodForRuntimeDeterminedType(MethodDesc typicalMethodDef, RuntimeDeterminedType rdType)
        {
            Debug.Assert(!(typicalMethodDef is MethodForRuntimeDeterminedType));
            Debug.Assert(typicalMethodDef.IsTypicalMethodDefinition);

            return _methodForRDTypes.GetOrCreateValue(new MethodForRuntimeDeterminedTypeKey(typicalMethodDef, rdType));
        }
    }
}
