// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext : IModuleResolver
    {
        public TypeSystemContext() : this(new TargetDetails(TargetArchitecture.Unknown, TargetOS.Unknown, TargetAbi.Unknown))
        {
        }

        public TypeSystemContext(TargetDetails target)
        {
            Target = target;

            _instantiatedTypes = new InstantiatedTypeKey.InstantiatedTypeKeyHashtable();

            _arrayTypes = new ArrayTypeKey.ArrayTypeKeyHashtable();

            _byRefTypes = new ByRefHashtable();

            _pointerTypes = new PointerHashtable();

            _functionPointerTypes = new FunctionPointerHashtable();

            _instantiatedMethods = new InstantiatedMethodKey.InstantiatedMethodKeyHashtable();

            _methodForInstantiatedTypes = new MethodForInstantiatedTypeKey.MethodForInstantiatedTypeKeyHashtable();

            _fieldForInstantiatedTypes = new FieldForInstantiatedTypeKey.FieldForInstantiatedTypeKeyHashtable();

            _signatureVariables = new SignatureVariableHashtable(this);
        }

        public TargetDetails Target
        {
            get;
        }

        public ModuleDesc SystemModule
        {
            get;
            private set;
        }

        protected void InitializeSystemModule(ModuleDesc systemModule)
        {
            Debug.Assert(SystemModule == null);
            SystemModule = systemModule;
        }

        public abstract DefType GetWellKnownType(WellKnownType wellKnownType, bool throwIfNotFound = true);

        public virtual ModuleDesc ResolveAssembly(AssemblyName name, bool throwIfNotFound = true)
        {
            if (throwIfNotFound)
                throw new NotSupportedException();
            return null;
        }

        internal virtual ModuleDesc ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound = true)
        {
            if (throwIfNotFound)
                throw new NotSupportedException();
            return null;
        }

        ModuleDesc IModuleResolver.ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound)
        {
            return ResolveModule(referencingModule, fileName, throwIfNotFound);
        }

        //
        // Array types
        //

        public ArrayType GetArrayType(TypeDesc elementType)
        {
            return GetArrayType(elementType, -1);
        }

        //
        // MDArray types
        //

        private struct ArrayTypeKey
        {
            private TypeDesc _elementType;
            private int _rank;

            public ArrayTypeKey(TypeDesc elementType, int rank)
            {
                _elementType = elementType;
                _rank = rank;
            }

            public TypeDesc ElementType
            {
                get
                {
                    return _elementType;
                }
            }

            public int Rank
            {
                get
                {
                    return _rank;
                }
            }

            public class ArrayTypeKeyHashtable : LockFreeReaderHashtable<ArrayTypeKey, ArrayType>
            {
                protected override int GetKeyHashCode(ArrayTypeKey key)
                {
                    return TypeHashingAlgorithms.ComputeArrayTypeHashCode(key._elementType, key._rank);
                }

                protected override int GetValueHashCode(ArrayType value)
                {
                    return TypeHashingAlgorithms.ComputeArrayTypeHashCode(value.ElementType, value.IsSzArray ? -1 : value.Rank);
                }

                protected override bool CompareKeyToValue(ArrayTypeKey key, ArrayType value)
                {
                    if (key._elementType != value.ElementType)
                        return false;

                    if (value.IsSzArray)
                        return key._rank == -1;

                    return key._rank == value.Rank;
                }

                protected override bool CompareValueToValue(ArrayType value1, ArrayType value2)
                {
                    return (value1.ElementType == value2.ElementType) && (value1.Rank == value2.Rank) && value1.IsSzArray == value2.IsSzArray;
                }

                protected override ArrayType CreateValueFromKey(ArrayTypeKey key)
                {
                    return new ArrayType(key.ElementType, key.Rank);
                }
            }
        }

        private ArrayTypeKey.ArrayTypeKeyHashtable _arrayTypes;

        public ArrayType GetArrayType(TypeDesc elementType, int rank)
        {
            return _arrayTypes.GetOrCreateValue(new ArrayTypeKey(elementType, rank));
        }

        //
        // ByRef types
        //
        public class ByRefHashtable : LockFreeReaderHashtable<TypeDesc, ByRefType>
        {
            protected override int GetKeyHashCode(TypeDesc key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(ByRefType value)
            {
                return value.ParameterType.GetHashCode();
            }

            protected override bool CompareKeyToValue(TypeDesc key, ByRefType value)
            {
                return key == value.ParameterType;
            }

            protected override bool CompareValueToValue(ByRefType value1, ByRefType value2)
            {
                return value1.ParameterType == value2.ParameterType;
            }

            protected override ByRefType CreateValueFromKey(TypeDesc key)
            {
                return new ByRefType(key);
            }
        }

        private ByRefHashtable _byRefTypes;

        public ByRefType GetByRefType(TypeDesc parameterType)
        {
            return _byRefTypes.GetOrCreateValue(parameterType);
        }

        //
        // Pointer types
        //
        public class PointerHashtable : LockFreeReaderHashtable<TypeDesc, PointerType>
        {
            protected override int GetKeyHashCode(TypeDesc key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(PointerType value)
            {
                return value.ParameterType.GetHashCode();
            }

            protected override bool CompareKeyToValue(TypeDesc key, PointerType value)
            {
                return key == value.ParameterType;
            }

            protected override bool CompareValueToValue(PointerType value1, PointerType value2)
            {
                return value1.ParameterType == value2.ParameterType;
            }

            protected override PointerType CreateValueFromKey(TypeDesc key)
            {
                return new PointerType(key);
            }
        }

        private PointerHashtable _pointerTypes;

        public PointerType GetPointerType(TypeDesc parameterType)
        {
            return _pointerTypes.GetOrCreateValue(parameterType);
        }

        //
        // Function pointer types
        //
        public class FunctionPointerHashtable : LockFreeReaderHashtable<MethodSignature, FunctionPointerType>
        {
            protected override int GetKeyHashCode(MethodSignature key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(FunctionPointerType value)
            {
                return value.Signature.GetHashCode();
            }

            protected override bool CompareKeyToValue(MethodSignature key, FunctionPointerType value)
            {
                return key.Equals(value.Signature);
            }

            protected override bool CompareValueToValue(FunctionPointerType value1, FunctionPointerType value2)
            {
                return value1.Signature.Equals(value2.Signature);
            }

            protected override FunctionPointerType CreateValueFromKey(MethodSignature key)
            {
                return new FunctionPointerType(key);
            }
        }

        private FunctionPointerHashtable _functionPointerTypes;

        public FunctionPointerType GetFunctionPointerType(MethodSignature signature)
        {
            return _functionPointerTypes.GetOrCreateValue(signature);
        }

        //
        // Instantiated types
        //

        private struct InstantiatedTypeKey
        {
            private TypeDesc _typeDef;
            private Instantiation _instantiation;

            public InstantiatedTypeKey(TypeDesc typeDef, Instantiation instantiation)
            {
                _typeDef = typeDef;
                _instantiation = instantiation;
            }

            public TypeDesc TypeDef
            {
                get
                {
                    return _typeDef;
                }
            }

            public Instantiation Instantiation
            {
                get
                {
                    return _instantiation;
                }
            }

            public class InstantiatedTypeKeyHashtable : LockFreeReaderHashtable<InstantiatedTypeKey, InstantiatedType>
            {
                protected override int GetKeyHashCode(InstantiatedTypeKey key)
                {
                    return key._instantiation.ComputeGenericInstanceHashCode(key._typeDef.GetHashCode());
                }

                protected override int GetValueHashCode(InstantiatedType value)
                {
                    return value.Instantiation.ComputeGenericInstanceHashCode(value.GetTypeDefinition().GetHashCode());
                }

                protected override bool CompareKeyToValue(InstantiatedTypeKey key, InstantiatedType value)
                {
                    if (key._typeDef != value.GetTypeDefinition())
                        return false;

                    Instantiation valueInstantiation = value.Instantiation;

                    if (key._instantiation.Length != valueInstantiation.Length)
                        return false;

                    for (int i = 0; i < key._instantiation.Length; i++)
                    {
                        if (key._instantiation[i] != valueInstantiation[i])
                            return false;
                    }

                    return true;
                }

                protected override bool CompareValueToValue(InstantiatedType value1, InstantiatedType value2)
                {
                    if (value1.GetTypeDefinition() != value2.GetTypeDefinition())
                        return false;

                    Instantiation value1Instantiation = value1.Instantiation;
                    Instantiation value2Instantiation = value2.Instantiation;

                    if (value1Instantiation.Length != value2Instantiation.Length)
                        return false;

                    for (int i = 0; i < value1Instantiation.Length; i++)
                    {
                        if (value1Instantiation[i] != value2Instantiation[i])
                            return false;
                    }

                    return true;
                }

                protected override InstantiatedType CreateValueFromKey(InstantiatedTypeKey key)
                {
                    return new InstantiatedType((MetadataType)key.TypeDef, key.Instantiation);
                }
            }
        }

        private InstantiatedTypeKey.InstantiatedTypeKeyHashtable _instantiatedTypes;

        public InstantiatedType GetInstantiatedType(MetadataType typeDef, Instantiation instantiation)
        {
            return _instantiatedTypes.GetOrCreateValue(new InstantiatedTypeKey(typeDef, instantiation));
        }

        //
        // Instantiated methods
        //

        private struct InstantiatedMethodKey
        {
            private MethodDesc _methodDef;
            private Instantiation _instantiation;
            private int _hashcode;

            public InstantiatedMethodKey(MethodDesc methodDef, Instantiation instantiation)
            {
                _methodDef = methodDef;
                _instantiation = instantiation;
                _hashcode = TypeHashingAlgorithms.ComputeMethodHashCode(methodDef.OwningType.GetHashCode(),
                    instantiation.ComputeGenericInstanceHashCode(TypeHashingAlgorithms.ComputeNameHashCode(methodDef.Name)));
            }

            public MethodDesc MethodDef
            {
                get
                {
                    return _methodDef;
                }
            }

            public Instantiation Instantiation
            {
                get
                {
                    return _instantiation;
                }
            }

            public class InstantiatedMethodKeyHashtable : LockFreeReaderHashtable<InstantiatedMethodKey, InstantiatedMethod>
            {
                protected override int GetKeyHashCode(InstantiatedMethodKey key)
                {
                    return key._hashcode;
                }

                protected override int GetValueHashCode(InstantiatedMethod value)
                {
                    return value.GetHashCode();
                }

                protected override bool CompareKeyToValue(InstantiatedMethodKey key, InstantiatedMethod value)
                {
                    if (key._methodDef != value.GetMethodDefinition())
                        return false;

                    Instantiation valueInstantiation = value.Instantiation;

                    if (key._instantiation.Length != valueInstantiation.Length)
                        return false;

                    for (int i = 0; i < key._instantiation.Length; i++)
                    {
                        if (key._instantiation[i] != valueInstantiation[i])
                            return false;
                    }

                    return true;
                }

                protected override bool CompareValueToValue(InstantiatedMethod value1, InstantiatedMethod value2)
                {
                    if (value1.GetMethodDefinition() != value2.GetMethodDefinition())
                        return false;

                    Instantiation value1Instantiation = value1.Instantiation;
                    Instantiation value2Instantiation = value2.Instantiation;

                    if (value1Instantiation.Length != value2Instantiation.Length)
                        return false;

                    for (int i = 0; i < value1Instantiation.Length; i++)
                    {
                        if (value1Instantiation[i] != value2Instantiation[i])
                            return false;
                    }

                    return true;
                }

                protected override InstantiatedMethod CreateValueFromKey(InstantiatedMethodKey key)
                {
                    return new InstantiatedMethod(key.MethodDef, key.Instantiation, key._hashcode);
                }
            }
        }

        private InstantiatedMethodKey.InstantiatedMethodKeyHashtable _instantiatedMethods;

        public InstantiatedMethod GetInstantiatedMethod(MethodDesc methodDef, Instantiation instantiation)
        {
            Debug.Assert(!(methodDef is InstantiatedMethod));
            return _instantiatedMethods.GetOrCreateValue(new InstantiatedMethodKey(methodDef, instantiation));
        }

        //
        // Methods for instantiated type
        //

        private struct MethodForInstantiatedTypeKey
        {
            private MethodDesc _typicalMethodDef;
            private InstantiatedType _instantiatedType;
            private int _hashcode;

            public MethodForInstantiatedTypeKey(MethodDesc typicalMethodDef, InstantiatedType instantiatedType)
            {
                _typicalMethodDef = typicalMethodDef;
                _instantiatedType = instantiatedType;
                _hashcode = TypeHashingAlgorithms.ComputeMethodHashCode(instantiatedType.GetHashCode(), TypeHashingAlgorithms.ComputeNameHashCode(typicalMethodDef.Name));
            }

            public MethodDesc TypicalMethodDef
            {
                get
                {
                    return _typicalMethodDef;
                }
            }

            public InstantiatedType InstantiatedType
            {
                get
                {
                    return _instantiatedType;
                }
            }

            public class MethodForInstantiatedTypeKeyHashtable : LockFreeReaderHashtable<MethodForInstantiatedTypeKey, MethodForInstantiatedType>
            {
                protected override int GetKeyHashCode(MethodForInstantiatedTypeKey key)
                {
                    return key._hashcode;
                }

                protected override int GetValueHashCode(MethodForInstantiatedType value)
                {
                    return value.GetHashCode();
                }

                protected override bool CompareKeyToValue(MethodForInstantiatedTypeKey key, MethodForInstantiatedType value)
                {
                    if (key._typicalMethodDef != value.GetTypicalMethodDefinition())
                        return false;

                    return key._instantiatedType == value.OwningType;
                }

                protected override bool CompareValueToValue(MethodForInstantiatedType value1, MethodForInstantiatedType value2)
                {
                    return (value1.GetTypicalMethodDefinition() == value2.GetTypicalMethodDefinition()) && (value1.OwningType == value2.OwningType);
                }

                protected override MethodForInstantiatedType CreateValueFromKey(MethodForInstantiatedTypeKey key)
                {
                    return new MethodForInstantiatedType(key.TypicalMethodDef, key.InstantiatedType, key._hashcode);
                }
            }
        }

        private MethodForInstantiatedTypeKey.MethodForInstantiatedTypeKeyHashtable _methodForInstantiatedTypes;

        public MethodDesc GetMethodForInstantiatedType(MethodDesc typicalMethodDef, InstantiatedType instantiatedType)
        {
            Debug.Assert(!(typicalMethodDef is MethodForInstantiatedType));
            Debug.Assert(!(typicalMethodDef is InstantiatedMethod));

            return _methodForInstantiatedTypes.GetOrCreateValue(new MethodForInstantiatedTypeKey(typicalMethodDef, instantiatedType));
        }

        //
        // Fields for instantiated type
        //

        private struct FieldForInstantiatedTypeKey
        {
            private FieldDesc _fieldDef;
            private InstantiatedType _instantiatedType;

            public FieldForInstantiatedTypeKey(FieldDesc fieldDef, InstantiatedType instantiatedType)
            {
                _fieldDef = fieldDef;
                _instantiatedType = instantiatedType;
            }

            public FieldDesc TypicalFieldDef
            {
                get
                {
                    return _fieldDef;
                }
            }

            public InstantiatedType InstantiatedType
            {
                get
                {
                    return _instantiatedType;
                }
            }

            public class FieldForInstantiatedTypeKeyHashtable : LockFreeReaderHashtable<FieldForInstantiatedTypeKey, FieldForInstantiatedType>
            {
                protected override int GetKeyHashCode(FieldForInstantiatedTypeKey key)
                {
                    return key._fieldDef.GetHashCode() ^ key._instantiatedType.GetHashCode();
                }

                protected override int GetValueHashCode(FieldForInstantiatedType value)
                {
                    return value.GetTypicalFieldDefinition().GetHashCode() ^ value.OwningType.GetHashCode();
                }

                protected override bool CompareKeyToValue(FieldForInstantiatedTypeKey key, FieldForInstantiatedType value)
                {
                    if (key._fieldDef != value.GetTypicalFieldDefinition())
                        return false;

                    return key._instantiatedType == value.OwningType;
                }

                protected override bool CompareValueToValue(FieldForInstantiatedType value1, FieldForInstantiatedType value2)
                {
                    return (value1.GetTypicalFieldDefinition() == value2.GetTypicalFieldDefinition()) && (value1.OwningType == value2.OwningType);
                }

                protected override FieldForInstantiatedType CreateValueFromKey(FieldForInstantiatedTypeKey key)
                {
                    return new FieldForInstantiatedType(key.TypicalFieldDef, key.InstantiatedType);
                }
            }
        }

        private FieldForInstantiatedTypeKey.FieldForInstantiatedTypeKeyHashtable _fieldForInstantiatedTypes;

        public FieldDesc GetFieldForInstantiatedType(FieldDesc fieldDef, InstantiatedType instantiatedType)
        {
            return _fieldForInstantiatedTypes.GetOrCreateValue(new FieldForInstantiatedTypeKey(fieldDef, instantiatedType));
        }

        //
        // Signature variables
        //
        private class SignatureVariableHashtable : LockFreeReaderHashtable<uint, SignatureVariable>
        {
            private TypeSystemContext _context;
            public SignatureVariableHashtable(TypeSystemContext context)
            {
                _context = context;
            }

            protected override int GetKeyHashCode(uint key)
            {
                return (int)key;
            }

            protected override int GetValueHashCode(SignatureVariable value)
            {
                uint combinedIndex = value.IsMethodSignatureVariable ? ((uint)value.Index | 0x80000000) : (uint)value.Index;
                return (int)combinedIndex;
            }

            protected override bool CompareKeyToValue(uint key, SignatureVariable value)
            {
                uint combinedIndex = value.IsMethodSignatureVariable ? ((uint)value.Index | 0x80000000) : (uint)value.Index;
                return key == combinedIndex;
            }

            protected override bool CompareValueToValue(SignatureVariable value1, SignatureVariable value2)
            {
                uint combinedIndex1 = value1.IsMethodSignatureVariable ? ((uint)value1.Index | 0x80000000) : (uint)value1.Index;
                uint combinedIndex2 = value2.IsMethodSignatureVariable ? ((uint)value2.Index | 0x80000000) : (uint)value2.Index;

                return combinedIndex1 == combinedIndex2;
            }

            protected override SignatureVariable CreateValueFromKey(uint key)
            {
                bool method = ((key & 0x80000000) != 0);
                int index = (int)(key & 0x7FFFFFFF);
                if (method)
                    return new SignatureMethodVariable(_context, index);
                else
                    return new SignatureTypeVariable(_context, index);
            }
        }

        private SignatureVariableHashtable _signatureVariables;

        public TypeDesc GetSignatureVariable(int index, bool method)
        {
            if (index < 0)
                throw new BadImageFormatException();

            uint combinedIndex = method ? ((uint)index | 0x80000000) : (uint)index;
            return _signatureVariables.GetOrCreateValue(combinedIndex);
        }

        protected internal virtual IEnumerable<MethodDesc> GetAllMethods(TypeDesc type)
        {
            return type.GetMethods();
        }

        protected internal virtual IEnumerable<MethodDesc> GetAllVirtualMethods(TypeDesc type)
        {
            return type.GetVirtualMethods();
        }

        /// <summary>
        /// Abstraction to allow the type system context to affect the field layout
        /// algorithm used by types to lay themselves out.
        /// </summary>
        public virtual FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            // Type system contexts that support computing field layout need to override this.
            throw new NotSupportedException();
        }

        /// <summary>
        /// Abstraction to allow the type system context to control the interfaces
        /// algorithm used by types.
        /// </summary>
        public RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForType(TypeDesc type)
        {
            if (type.IsDefType)
            {
                return GetRuntimeInterfacesAlgorithmForDefType((DefType)type);
            }
            else if (type.IsArray)
            {
                ArrayType arrType = (ArrayType)type;
                TypeDesc elementType = arrType.ElementType;
                if (arrType.IsSzArray && !elementType.IsPointer && !elementType.IsFunctionPointer)
                {
                    return GetRuntimeInterfacesAlgorithmForNonPointerArrayType((ArrayType)type);
                }
                else
                {
                    return BaseTypeRuntimeInterfacesAlgorithm.Instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Abstraction to allow the type system context to control the interfaces
        /// algorithm used by types.
        /// </summary>
        protected virtual RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            // Type system contexts that support computing runtime interfaces need to override this.
            throw new NotSupportedException();
        }

        /// <summary>
        /// Abstraction to allow the type system context to control the interfaces
        /// algorithm used by single dimensional array types.
        /// </summary>
        protected virtual RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            // Type system contexts that support computing runtime interfaces need to override this.
            throw new NotSupportedException();
        }

        public virtual VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
            // Type system contexts that support virtual method resolution need to override this.
            throw new NotSupportedException();
        }

        // Abstraction to allow different runtimes to have different policy about which fields are
        // in the GC static region, and which are not
        protected internal virtual bool ComputeHasGCStaticBase(FieldDesc field)
        {
            // Type system contexts that support this need to override this.
            throw new NotSupportedException();
        }

        /// <summary>
        /// TypeSystemContext controlled type flags computation. This allows computation of flags which depend
        /// on the particular TypeSystemContext in use
        /// </summary>
        internal TypeFlags ComputeTypeFlags(TypeDesc type, TypeFlags flags, TypeFlags mask)
        {
            // If we are looking to compute HasStaticConstructor, and we haven't yet assigned a value
            if ((mask & TypeFlags.HasStaticConstructorComputed) == TypeFlags.HasStaticConstructorComputed)
            {
                TypeDesc typeDefinition = type.GetTypeDefinition();

                if (typeDefinition != type)
                {
                    // If the type definition is different, the code was working with an instantiated generic or some such.
                    // In that case, just query the HasStaticConstructor property, as it can cache the answer
                    if (typeDefinition.HasStaticConstructor)
                        flags |= TypeFlags.HasStaticConstructor;
                }
                else
                {
                    if (ComputeHasStaticConstructor(typeDefinition))
                    {
                        flags |= TypeFlags.HasStaticConstructor;
                    }
                }

                flags |= TypeFlags.HasStaticConstructorComputed;
            }

            // We are looking to compute IsIDynamicInterfaceCastable and we haven't yet assigned a value
            if ((mask & TypeFlags.IsIDynamicInterfaceCastableComputed) == TypeFlags.IsIDynamicInterfaceCastableComputed)
            {
                TypeDesc typeDefinition = type.GetTypeDefinition();
                if (!typeDefinition.IsValueType)
                {
                    foreach (DefType interfaceType in typeDefinition.RuntimeInterfaces)
                    {
                        if (IsIDynamicInterfaceCastableInterface(interfaceType))
                        {
                            flags |= TypeFlags.IsIDynamicInterfaceCastable;
                            break;
                        }
                    }
                }

                flags |= TypeFlags.IsIDynamicInterfaceCastableComputed;
            }

            return flags;
        }

        /// <summary>
        /// Algorithm to control which types are considered to have static constructors
        /// </summary>
        protected internal abstract bool ComputeHasStaticConstructor(TypeDesc type);

        /// <summary>
        /// Determine if the type implements <code>IDynamicInterfaceCastable</code>
        /// </summary>
        protected internal abstract bool IsIDynamicInterfaceCastableInterface(DefType type);
    }
}
