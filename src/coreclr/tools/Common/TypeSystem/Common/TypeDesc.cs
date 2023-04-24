// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the fundamental base type of all types within the type system.
    /// </summary>
    public abstract partial class TypeDesc : TypeSystemEntity
    {
#pragma warning disable CA1825 // avoid Array.Empty<T>() instantiation for TypeLoader
        public static readonly TypeDesc[] EmptyTypes = new TypeDesc[0];
#pragma warning restore CA1825

        /// Inherited types are required to override, and should use the algorithms
        /// in TypeHashingAlgorithms in their implementation.
        public abstract override int GetHashCode();

        public override bool Equals(object o)
        {
            // Its only valid to compare two TypeDescs in the same context
            Debug.Assert(o is not TypeDesc || ReferenceEquals(((TypeDesc)o).Context, this.Context));
            return ReferenceEquals(this, o);
        }

#if DEBUG
        public static bool operator ==(TypeDesc left, TypeDesc right)
        {
            // Its only valid to compare two TypeDescs in the same context
            Debug.Assert(left is null || right is null || ReferenceEquals(left.Context, right.Context));
            return ReferenceEquals(left, right);
        }

        public static bool operator !=(TypeDesc left, TypeDesc right)
        {
            // Its only valid to compare two TypeDescs in the same context
            Debug.Assert(left is null || right is null || ReferenceEquals(left.Context, right.Context));
            return !ReferenceEquals(left, right);
        }
#endif

        // The most frequently used type properties are cached here to avoid excessive virtual calls
        private TypeFlags _typeFlags;

        /// <summary>
        /// Gets the generic instantiation information of this type.
        /// For generic definitions, retrieves the generic parameters of the type.
        /// For generic instantiation, retrieves the generic arguments of the type.
        /// </summary>
        public virtual Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type has a generic instantiation.
        /// This will be true for generic type instantiations and generic definitions.
        /// </summary>
        public bool HasInstantiation
        {
            get
            {
                return this.Instantiation.Length != 0;
            }
        }

        internal void SetWellKnownType(WellKnownType wellKnownType)
        {
            TypeFlags flags;

            switch (wellKnownType)
            {
                case WellKnownType.Void:
                case WellKnownType.Boolean:
                case WellKnownType.Char:
                case WellKnownType.SByte:
                case WellKnownType.Byte:
                case WellKnownType.Int16:
                case WellKnownType.UInt16:
                case WellKnownType.Int32:
                case WellKnownType.UInt32:
                case WellKnownType.Int64:
                case WellKnownType.UInt64:
                case WellKnownType.IntPtr:
                case WellKnownType.UIntPtr:
                case WellKnownType.Single:
                case WellKnownType.Double:
                    flags = (TypeFlags)wellKnownType;
                    break;

                case WellKnownType.ValueType:
                case WellKnownType.Enum:
                    flags = TypeFlags.Class;
                    break;

                case WellKnownType.Nullable:
                    flags = TypeFlags.Nullable;
                    break;

                case WellKnownType.Object:
                case WellKnownType.String:
                case WellKnownType.Array:
                case WellKnownType.MulticastDelegate:
                case WellKnownType.Exception:
                    flags = TypeFlags.Class;
                    break;

                case WellKnownType.RuntimeTypeHandle:
                case WellKnownType.RuntimeMethodHandle:
                case WellKnownType.RuntimeFieldHandle:
                case WellKnownType.TypedReference:
                    flags = TypeFlags.ValueType;
                    break;

                default:
                    throw new ArgumentException();
            }

            _typeFlags = flags;
        }

        protected abstract TypeFlags ComputeTypeFlags(TypeFlags mask);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private TypeFlags InitializeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = ComputeTypeFlags(mask);

            if ((flags & mask) == 0)
                flags = Context.ComputeTypeFlags(this, flags, mask);

            Debug.Assert((flags & mask) != 0);
            _typeFlags |= flags;

            return flags & mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal TypeFlags GetTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = _typeFlags & mask;
            if (flags != 0)
                return flags;
            return InitializeTypeFlags(mask);
        }

        /// <summary>
        /// Retrieves the category of the type. This is one of the possible values of
        /// <see cref="TypeFlags"/> less than <see cref="TypeFlags.CategoryMask"/>.
        /// </summary>
        public TypeFlags Category
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type is an interface type.
        /// </summary>
        public bool IsInterface
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) == TypeFlags.Interface;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type is a value type (not a reference type).
        /// </summary>
        public bool IsValueType
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) < TypeFlags.Class;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is one of the primitive types (boolean, char, void,
        /// a floating-point, or an integer type).
        /// </summary>
        public bool IsPrimitive
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) < TypeFlags.ValueType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is one of the primitive numeric types
        /// (a floating-point or an integer type).
        /// </summary>
        public bool IsPrimitiveNumeric
        {
            get
            {
                switch (GetTypeFlags(TypeFlags.CategoryMask))
                {
                    case TypeFlags.SByte:
                    case TypeFlags.Byte:
                    case TypeFlags.Int16:
                    case TypeFlags.UInt16:
                    case TypeFlags.Int32:
                    case TypeFlags.UInt32:
                    case TypeFlags.Int64:
                    case TypeFlags.UInt64:
                    case TypeFlags.IntPtr:
                    case TypeFlags.UIntPtr:
                    case TypeFlags.Single:
                    case TypeFlags.Double:
                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an enum type.
        /// Access <see cref="UnderlyingType"/> to retrieve the underlying integral type.
        /// </summary>
        public bool IsEnum
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) == TypeFlags.Enum;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a delegate type.
        /// </summary>
        public bool IsDelegate
        {
            get
            {
                var baseType = this.BaseType;
                return (baseType != null) ? baseType.IsWellKnownType(WellKnownType.MulticastDelegate) : false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is System.Void type.
        /// </summary>
        public bool IsVoid
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) == TypeFlags.Void;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is System.String type.
        /// </summary>
        public bool IsString
        {
            get
            {
                return this.IsWellKnownType(WellKnownType.String);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is System.Object type.
        /// </summary>
        public bool IsObject
        {
            get
            {
                return this.IsWellKnownType(WellKnownType.Object);
            }
        }

        public bool IsTypedReference
        {
            get
            {
                return this.IsWellKnownType(WellKnownType.TypedReference);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a generic definition, or
        /// an instance of System.Nullable`1.
        /// </summary>
        public bool IsNullable
        {
            get
            {
                return this.GetTypeDefinition().IsWellKnownType(WellKnownType.Nullable);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an array type (<see cref="ArrayType"/>).
        /// Note this will return true for both multidimensional array types and vector types.
        /// Use <see cref="IsSzArray"/> to check for vector types.
        /// </summary>
        public bool IsArray
        {
            get
            {
                return this is ArrayType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a vector type. A vector is a single-dimensional
        /// array with a zero lower bound. To check for arrays in general, use <see cref="IsArray"/>.
        /// </summary>
        public bool IsSzArray
        {
            get
            {
                return this.IsArray && ((ArrayType)this).IsSzArray;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a non-vector array type.
        /// To check for arrays in general, use <see cref="IsArray"/>.
        /// </summary>
        public bool IsMdArray
        {
            get
            {
                return this.IsArray && ((ArrayType)this).IsMdArray;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a managed pointer type (<see cref="ByRefType"/>).
        /// </summary>
        public bool IsByRef
        {
            get
            {
                return this is ByRefType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an unmanaged pointer type (<see cref="PointerType"/>).
        /// </summary>
        public bool IsPointer
        {
            get
            {
                return this is PointerType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an unmanaged function pointer type (<see cref="FunctionPointerType"/>).
        /// </summary>
        public bool IsFunctionPointer
        {
            get
            {
                return this is FunctionPointerType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a <see cref="SignatureTypeVariable"/> or <see cref="SignatureMethodVariable"/>.
        /// </summary>
        public bool IsSignatureVariable
        {
            get
            {
                return this is SignatureTypeVariable || this is SignatureMethodVariable;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a generic parameter (<see cref="GenericParameterDesc"/>).
        /// </summary>
        public bool IsGenericParameter
        {
            get
            {
                return GetTypeFlags(TypeFlags.CategoryMask) == TypeFlags.GenericParameter;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a pointer, byref, array, or szarray type,
        /// and can be used as a ParameterizedType.
        /// </summary>
        public bool IsParameterizedType
        {
            get
            {
                TypeFlags flags = GetTypeFlags(TypeFlags.CategoryMask);
                Debug.Assert((flags >= TypeFlags.Array && flags <= TypeFlags.Pointer) == (this is ParameterizedType));
                return (flags >= TypeFlags.Array && flags <= TypeFlags.Pointer);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a class, an interface, a value type, or a
        /// generic instance of one of them.
        /// </summary>
        public bool IsDefType
        {
            get
            {
                Debug.Assert(GetTypeFlags(TypeFlags.CategoryMask) <= TypeFlags.Interface == this is DefType);
                return GetTypeFlags(TypeFlags.CategoryMask) <= TypeFlags.Interface;
            }
        }

        /// <summary>
        /// Gets a value indicating whether locations of this type refer to an object on the GC heap.
        /// </summary>
        public bool IsGCPointer
        {
            get
            {
                TypeFlags category = GetTypeFlags(TypeFlags.CategoryMask);
                return category == TypeFlags.Class
                    || category == TypeFlags.Array
                    || category == TypeFlags.SzArray
                    || category == TypeFlags.Interface;
            }
        }

        /// <summary>
        /// Gets the type from which this type derives from, or null if there's no such type.
        /// </summary>
        public virtual DefType BaseType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type has a base type.
        /// </summary>
        public bool HasBaseType
        {
            get
            {
                return BaseType != null;
            }
        }

        /// <summary>
        /// If this is an enum type, gets the underlying integral type of the enum type.
        /// For all other types, returns 'this'.
        /// </summary>
        public virtual TypeDesc UnderlyingType
        {
            get
            {
                if (!this.IsEnum)
                    return this;

                foreach (var field in this.GetFields())
                {
                    if (!field.IsStatic)
                        return field.FieldType;
                }

                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, this);
                return null; // Unreachable
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type has a class constructor method.
        /// Use <see cref="GetStaticConstructor"/> to retrieve it.
        /// </summary>
        public bool HasStaticConstructor
        {
            get
            {
                return (GetTypeFlags(TypeFlags.HasStaticConstructor | TypeFlags.HasStaticConstructorComputed) & TypeFlags.HasStaticConstructor) != 0;
            }
        }

        /// <summary>
        /// Gets all methods on this type defined within the type's metadata.
        /// This will not include methods injected by the type system context.
        /// </summary>
        public virtual IEnumerable<MethodDesc> GetMethods()
        {
            return MethodDesc.EmptyMethods;
        }

        /// <summary>
        /// Gets a subset of methods returned by <see cref="GetMethods"/> that are virtual.
        /// </summary>
        public virtual IEnumerable<MethodDesc> GetVirtualMethods()
        {
            foreach (MethodDesc method in GetMethods())
                if (method.IsVirtual)
                    yield return method;
        }

        /// <summary>
        /// Gets a named method on the type. This method only looks at methods defined
        /// in type's metadata. The <paramref name="signature"/> parameter can be null.
        /// If signature is not specified and there are multiple matches, the first one
        /// is returned. Returns null if method not found.
        /// </summary>
        public MethodDesc GetMethod(string name, MethodSignature signature)
        {
            return GetMethod(name, signature, default(Instantiation));
        }

        /// <summary>
        /// Gets a named method on the type. This method only looks at methods defined
        /// in type's metadata. The <paramref name="signature"/> parameter can be null.
        /// If signature is not specified and there are multiple matches, the first one
        /// is returned. If substitution is not null, then substitution will be applied to
        /// possible target methods before signature comparison. Returns null if method not found.
        /// </summary>
        public virtual MethodDesc GetMethod(string name, MethodSignature signature, Instantiation substitution)
        {
            foreach (var method in GetMethods())
            {
                if (method.Name == name)
                {
                    if (signature == null || signature.Equals(method.Signature.ApplySubstitution(substitution)))
                        return method;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the class constructor method of this type.
        /// </summary>
        /// <returns></returns>
        public virtual MethodDesc GetStaticConstructor()
        {
            return null;
        }

        /// <summary>
        /// Retrieves the public parameterless constructor method of the type, or null if there isn't one
        /// or the type is abstract.
        /// </summary>
        public virtual MethodDesc GetDefaultConstructor()
        {
            return null;
        }

        /// <summary>
        /// Gets all fields on the type as defined in the metadata.
        /// </summary>
        public virtual IEnumerable<FieldDesc> GetFields()
        {
            return FieldDesc.EmptyFields;
        }

        /// <summary>
        /// Gets a named field on the type. Returns null if the field wasn't found.
        /// </summary>
        // TODO: Substitutions, generics, modopts, ...
        // TODO: field signature
        public virtual FieldDesc GetField(string name)
        {
            foreach (var field in GetFields())
            {
                if (field.Name == name)
                    return field;
            }
            return null;
        }

        public virtual TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            return this;
        }

        /// <summary>
        /// Gets the definition of the type. If this is a generic type instance,
        /// this method strips the instantiation (E.g C&lt;int&gt; -> C&lt;T&gt;)
        /// </summary>
        public virtual TypeDesc GetTypeDefinition()
        {
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether this is a type definition. Returns false
        /// if this is an instantiated generic type.
        /// </summary>
        public bool IsTypeDefinition
        {
            get
            {
                return GetTypeDefinition() == this;
            }
        }

        /// <summary>
        /// Determine if two types share the same type definition
        /// </summary>
        public bool HasSameTypeDefinition(TypeDesc otherType)
        {
            return GetTypeDefinition() == otherType.GetTypeDefinition();
        }

        /// <summary>
        /// Gets a value indicating whether this type has a finalizer method.
        /// Use <see cref="GetFinalizer"/> to retrieve the method.
        /// </summary>
        public bool HasFinalizer
        {
            get
            {
                return (GetTypeFlags(TypeFlags.HasFinalizer | TypeFlags.HasFinalizerComputed) & TypeFlags.HasFinalizer) != 0;
            }
        }

        /// <summary>
        /// Gets the finalizer method (an override of the System.Object::Finalize method)
        /// if this type has one. Returns null if the type doesn't define one.
        /// </summary>
        public virtual MethodDesc GetFinalizer()
        {
            return null;
        }

        /// <summary>
        /// Gets a value indicating whether this type has generic variance (the definition of the type
        /// has a generic parameter that is co- or contravariant).
        /// </summary>
        public bool HasVariance
        {
            get
            {
                return (GetTypeFlags(TypeFlags.HasGenericVariance | TypeFlags.HasGenericVarianceComputed) & TypeFlags.HasGenericVariance) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type is an uninstantiated definition of a generic type.
        /// </summary>
        public bool IsGenericDefinition
        {
            get
            {
                return HasInstantiation && IsTypeDefinition;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a byref-like type
        /// (a <code>TypedReference</code>, <code>Span&lt;T&gt;</code>, etc.).
        /// </summary>
        public bool IsByRefLike
        {
            get
            {
                return (GetTypeFlags(TypeFlags.IsByRefLike | TypeFlags.AttributeCacheComputed) & TypeFlags.IsByRefLike) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this type implements <code>IDynamicInterfaceCastable</code>
        /// </summary>
        public bool IsIDynamicInterfaceCastable
        {
            get
            {
                return (GetTypeFlags(TypeFlags.IsIDynamicInterfaceCastable | TypeFlags.IsIDynamicInterfaceCastableComputed) & TypeFlags.IsIDynamicInterfaceCastable) != 0;
            }
        }
    }
}
