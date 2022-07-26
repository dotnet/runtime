// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract class RuntimeGenericParameterTypeInfo : RuntimeTypeInfo
    {
        protected RuntimeGenericParameterTypeInfo(int position)
        {
            _position = position;
        }

        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => false;
        protected sealed override bool IsArrayImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        protected sealed override bool IsByRefImpl() => false;
        protected sealed override bool IsPointerImpl() => false;
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => true;
        public abstract override bool IsGenericTypeParameter { get; }
        public abstract override bool IsGenericMethodParameter { get; }
        public sealed override bool IsByRefLike => false;

        public sealed override Assembly Assembly
        {
            get
            {
                return DeclaringType.Assembly;
            }
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                return true;
            }
        }

        public abstract override MethodBase DeclaringMethod { get; }



        public sealed override Type[] GetGenericParameterConstraints()
        {
            return ConstraintInfos.CloneTypeArray();
        }

        public sealed override string FullName
        {
            get
            {
                return null;  // We return null as generic parameter types are not roundtrippable through Type.GetType().
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // Unlike most other MemberInfo objects, generic parameter types never get cloned due to containing generic types being instantiated.
            // That is, their DeclaringType is always the generic type definition. As a Type, the ReflectedType property is always equal to the DeclaringType.
            //
            // Because of these conditions, we can safely implement both the method token equivalence and the "is this type from the same implementor"
            // check as our regular Equals() method.
            return Equals(other);
        }

        public sealed override int GenericParameterPosition
        {
            get
            {
                return _position;
            }
        }

        public sealed override string Namespace
        {
            get
            {
                return DeclaringType.Namespace;
            }
        }

        public sealed override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                return null;
            }
        }

        public sealed override string ToString()
        {
            return Name;
        }

        protected sealed override TypeAttributes GetAttributeFlagsImpl()
        {
            return TypeAttributes.Public;
        }

        internal sealed override bool CanBrowseWithoutMissingMetadataExceptions => true;

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                Debug.Fail("Since this class always returns null for FullName, this helper should be unreachable.");
                return null;
            }
        }

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable
        {
            get
            {
                return default(RuntimeTypeHandle);
            }
        }

        //
        // Returns the generic parameter substitutions to use when enumerating declared members, base class and implemented interfaces.
        //
        internal abstract override TypeContext TypeContext { get; }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                QTypeDefRefOrSpec[] constraints = Constraints;
                TypeInfo[] constraintInfos = ConstraintInfos;
                for (int i = 0; i < constraints.Length; i++)
                {
                    TypeInfo constraintInfo = constraintInfos[i];
                    if (constraintInfo.IsInterface)
                        continue;
                    return constraints[i];
                }

                RuntimeNamedTypeInfo objectTypeInfo = typeof(object).CastToRuntimeNamedTypeInfo();
                return objectTypeInfo.TypeDefinitionQHandle;
            }
        }

        //
        // Returns the *directly implemented* interfaces as typedefs, specs or refs. ImplementedInterfaces will take care of the transitive closure and
        // insertion of the TypeContext.
        //
        internal sealed override QTypeDefRefOrSpec[] TypeRefDefOrSpecsForDirectlyImplementedInterfaces
        {
            get
            {
                LowLevelList<QTypeDefRefOrSpec> result = new LowLevelList<QTypeDefRefOrSpec>();
                QTypeDefRefOrSpec[] constraints = Constraints;
                TypeInfo[] constraintInfos = ConstraintInfos;
                for (int i = 0; i < constraints.Length; i++)
                {
                    if (constraintInfos[i].IsInterface)
                        result.Add(constraints[i]);
                }
                return result.ToArray();
            }
        }

        protected abstract QTypeDefRefOrSpec[] Constraints { get; }

        private TypeInfo[] ConstraintInfos
        {
            get
            {
                QTypeDefRefOrSpec[] constraints = Constraints;
                if (constraints.Length == 0)
                    return Array.Empty<TypeInfo>();
                TypeInfo[] constraintInfos = new TypeInfo[constraints.Length];
                for (int i = 0; i < constraints.Length; i++)
                {
                    constraintInfos[i] = constraints[i].Resolve(TypeContext);
                }
                return constraintInfos;
            }
        }

        private readonly int _position;
    }
}
