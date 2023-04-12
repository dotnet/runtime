// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

#if TYPE_LOADER_IMPLEMENTATION
using MetadataType = Internal.TypeSystem.DefType;
#endif

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedType : MetadataType
    {
        private MetadataType _typeDef;
        private Instantiation _instantiation;

        internal InstantiatedType(MetadataType typeDef, Instantiation instantiation)
        {
            Debug.Assert(!(typeDef is InstantiatedType));
            _typeDef = typeDef;

            Debug.Assert(instantiation.Length > 0);
            _instantiation = instantiation;

            _baseType = this; // Not yet initialized flag
        }

        private int _hashCode;

        public override int GetHashCode()
        {
            if (_hashCode == 0)
                _hashCode = _instantiation.ComputeGenericInstanceHashCode(_typeDef.GetHashCode());
            return _hashCode;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _typeDef.Context;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _instantiation;
            }
        }

        private MetadataType _baseType /* = this */;

        private MetadataType InitializeBaseType()
        {
            var uninst = _typeDef.BaseType;

            return (_baseType = (uninst != null) ? (MetadataType)uninst.InstantiateSignature(_instantiation, default(Instantiation)) : null);
        }

        public override DefType BaseType
        {
            get
            {
                if (_baseType == this)
                    return InitializeBaseType();
                return _baseType;
            }
        }

        // Type system implementations that support the notion of intrinsic types
        // will provide an implementation that adds the flag if necessary.
        partial void AddComputedIntrinsicFlag(ref TypeFlags flags);

        // Type system implementations that support the notion of inline arrays
        // will provide an implementation that adds the flag if necessary.
        partial void AddComputedInlineArrayFlag(ref TypeFlags flags);

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= _typeDef.Category;
            }

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;

                if (_typeDef.HasVariance)
                    flags |= TypeFlags.HasGenericVariance;
            }

            if ((mask & TypeFlags.HasFinalizerComputed) != 0)
            {
                flags |= TypeFlags.HasFinalizerComputed;

                if (_typeDef.HasFinalizer)
                    flags |= TypeFlags.HasFinalizer;
            }

            if ((mask & TypeFlags.AttributeCacheComputed) != 0)
            {
                flags |= TypeFlags.AttributeCacheComputed;

                if (_typeDef.IsByRefLike)
                    flags |= TypeFlags.IsByRefLike;

                AddComputedInlineArrayFlag(ref flags);

                AddComputedIntrinsicFlag(ref flags);
            }

            return flags;
        }

        public override string Name
        {
            get
            {
                return _typeDef.Name;
            }
        }

        public override string Namespace
        {
            get
            {
                return _typeDef.Namespace;
            }
        }

        public override IEnumerable<MethodDesc> GetMethods()
        {
            foreach (var typicalMethodDef in _typeDef.GetMethods())
            {
                yield return _typeDef.Context.GetMethodForInstantiatedType(typicalMethodDef, this);
            }
        }

        public override IEnumerable<MethodDesc> GetVirtualMethods()
        {
            foreach (var typicalMethodDef in _typeDef.GetVirtualMethods())
            {
                yield return _typeDef.Context.GetMethodForInstantiatedType(typicalMethodDef, this);
            }
        }

        // TODO: Substitutions, generics, modopts, ...
        public override MethodDesc GetMethod(string name, MethodSignature signature, Instantiation substitution)
        {
            MethodDesc typicalMethodDef = _typeDef.GetMethod(name, signature, substitution);
            if (typicalMethodDef == null)
                return null;
            return _typeDef.Context.GetMethodForInstantiatedType(typicalMethodDef, this);
        }

        public override MethodDesc GetStaticConstructor()
        {
            MethodDesc typicalCctor = _typeDef.GetStaticConstructor();
            if (typicalCctor == null)
                return null;
            return _typeDef.Context.GetMethodForInstantiatedType(typicalCctor, this);
        }

        public override MethodDesc GetDefaultConstructor()
        {
            MethodDesc typicalCtor = _typeDef.GetDefaultConstructor();
            if (typicalCtor == null)
                return null;
            return _typeDef.Context.GetMethodForInstantiatedType(typicalCtor, this);
        }

        public override MethodDesc GetFinalizer()
        {
            MethodDesc typicalFinalizer = _typeDef.GetFinalizer();
            if (typicalFinalizer == null)
                return null;

            DefType typeInHierarchy = this;

            // Note, we go back to the type definition/typical method definition in this code.
            // If the finalizer is implemented on a base type that is also a generic, then the
            // typicalFinalizer in that case is a MethodForInstantiatedType for an instantiated type
            // which is instantiated over the open type variables of the derived type.

            while (typicalFinalizer.OwningType.GetTypeDefinition() != typeInHierarchy.GetTypeDefinition())
            {
                typeInHierarchy = typeInHierarchy.BaseType;
            }

            if (typeInHierarchy == typicalFinalizer.OwningType)
            {
                return typicalFinalizer;
            }
            else
            {
                Debug.Assert(typeInHierarchy is InstantiatedType);
                return _typeDef.Context.GetMethodForInstantiatedType(typicalFinalizer.GetTypicalMethodDefinition(), (InstantiatedType)typeInHierarchy);
            }
        }

        public override IEnumerable<FieldDesc> GetFields()
        {
            foreach (var fieldDef in _typeDef.GetFields())
            {
                yield return _typeDef.Context.GetFieldForInstantiatedType(fieldDef, this);
            }
        }

        // TODO: Substitutions, generics, modopts, ...
        public override FieldDesc GetField(string name)
        {
            FieldDesc fieldDef = _typeDef.GetField(name);
            if (fieldDef == null)
                return null;
            return _typeDef.Context.GetFieldForInstantiatedType(fieldDef, this);
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc[] clone = null;

            for (int i = 0; i < _instantiation.Length; i++)
            {
                TypeDesc uninst = _instantiation[i];
                TypeDesc inst = uninst.InstantiateSignature(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new TypeDesc[_instantiation.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = _instantiation[j];
                        }
                    }
                    clone[i] = inst;
                }
            }

            return (clone == null) ? this : _typeDef.Context.GetInstantiatedType(_typeDef, new Instantiation(clone));
        }

        /// <summary>
        /// Instantiate an array of TypeDescs over typeInstantiation and methodInstantiation
        /// </summary>
        public static T[] InstantiateTypeArray<T>(T[] uninstantiatedTypes, Instantiation typeInstantiation, Instantiation methodInstantiation) where T : TypeDesc
        {
            T[] clone = null;

            for (int i = 0; i < uninstantiatedTypes.Length; i++)
            {
                T uninst = uninstantiatedTypes[i];
                TypeDesc inst = uninst.InstantiateSignature(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new T[uninstantiatedTypes.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = uninstantiatedTypes[j];
                        }
                    }
                    clone[i] = (T)inst;
                }
            }

            return clone ?? uninstantiatedTypes;
        }

        // Strips instantiation. E.g C<int> -> C<T>
        public override TypeDesc GetTypeDefinition()
        {
            return _typeDef;
        }

        public override DefType ContainingType
        {
            get
            {
                // Return the result from the typical type definition.
                return _typeDef.ContainingType;
            }
        }

        public override TypeDesc UnderlyingType
        {
            get
            {
                if (!IsEnum)
                    return this;
                else
                    return _typeDef.UnderlyingType;
            }
        }
    }
}
