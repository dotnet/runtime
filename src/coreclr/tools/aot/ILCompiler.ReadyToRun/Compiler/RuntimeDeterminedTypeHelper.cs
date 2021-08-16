// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.Text;

namespace ILCompiler
{
    /// <summary>
    /// Helper class used to collapse runtime determined types
    /// based on their kind and index as we otherwise don't need
    /// to distinguish among them for the purpose of emitting
    /// signatures and generic lookups.
    /// </summary>
    public static class RuntimeDeterminedTypeHelper
    {
        public static bool Equals(Instantiation instantiation1, Instantiation instantiation2)
        {
            if (instantiation1.Length != instantiation2.Length)
            {
                return false;
            }
            for (int argIndex = 0; argIndex < instantiation1.Length; argIndex++)
            {
                if (!Equals(instantiation1[argIndex], instantiation2[argIndex]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Equals(TypeDesc type1, TypeDesc type2)
        {
            if (type1 == type2)
            {
                return true;
            }

            if (type1 == null || type2 == null)
            {
                return false;
            }

            RuntimeDeterminedType runtimeDeterminedType1 = type1 as RuntimeDeterminedType;
            RuntimeDeterminedType runtimeDeterminedType2 = type2 as RuntimeDeterminedType;
            if (runtimeDeterminedType1 != null || runtimeDeterminedType2 != null)
            {
                if (runtimeDeterminedType1 == null || runtimeDeterminedType2 == null)
                {
                    return false;
                }
                return runtimeDeterminedType1.RuntimeDeterminedDetailsType.Index == runtimeDeterminedType2.RuntimeDeterminedDetailsType.Index &&
                    runtimeDeterminedType1.RuntimeDeterminedDetailsType.Kind == runtimeDeterminedType2.RuntimeDeterminedDetailsType.Kind;
            }

            ArrayType arrayType1 = type1 as ArrayType;
            ArrayType arrayType2 = type2 as ArrayType;
            if (arrayType1 != null || arrayType2 != null)
            {
                if (arrayType1 == null || arrayType2 == null)
                {
                    return false;
                }
                return arrayType1.Rank == arrayType2.Rank &&
                    arrayType1.IsSzArray == arrayType2.IsSzArray &&
                    Equals(arrayType1.ElementType, arrayType2.ElementType);
            }

            ByRefType byRefType1 = type1 as ByRefType;
            ByRefType byRefType2 = type2 as ByRefType;
            if (byRefType1 != null || byRefType2 != null)
            {
                if (byRefType1 == null || byRefType2 == null)
                {
                    return false;
                }
                return Equals(byRefType1.ParameterType, byRefType2.ParameterType);
            }

            if (type1.GetTypeDefinition() != type2.GetTypeDefinition() ||
                !Equals(type1.Instantiation, type2.Instantiation))
            {
                return false;
            }

            return true;
        }

        public static bool Equals(MethodDesc method1, MethodDesc method2)
        {
            if (method1 == method2)
            {
                return true;
            }
            if (method1 == null || method2 == null)
            {
                return false;
            }

            if (!Equals(method1.OwningType, method2.OwningType) ||
                method1.Signature.Length != method2.Signature.Length ||
                !Equals(method1.Instantiation, method2.Instantiation) ||
                !Equals(method1.Signature.ReturnType, method2.Signature.ReturnType))
            {
                return false;
            }
            for (int argIndex = 0; argIndex < method1.Signature.Length; argIndex++)
            {
                if (!Equals(method1.Signature[argIndex], method2.Signature[argIndex]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Equals(MethodWithToken methodWithToken1, MethodWithToken methodWithToken2)
        {
            if (methodWithToken1 == methodWithToken2)
            {
                return true;
            }
            if (methodWithToken1 == null || methodWithToken2 == null)
            {
                return false;
            }
            return Equals(methodWithToken1.Method, methodWithToken2.Method)
                && Equals(methodWithToken1.OwningType, methodWithToken2.OwningType)
                && Equals(methodWithToken1.ConstrainedType, methodWithToken2.ConstrainedType)
                && methodWithToken1.Unboxing == methodWithToken2.Unboxing;
        }

        public static bool Equals(FieldDesc field1, FieldDesc field2)
        {
            if (field1 == null || field2 == null)
            {
                return field1 == null && field2 == null;
            }
            return field1.Name == field2.Name &&
                RuntimeDeterminedTypeHelper.Equals(field1.OwningType, field2.OwningType) &&
                RuntimeDeterminedTypeHelper.Equals(field1.FieldType, field2.FieldType);
        }

        public static int GetHashCode(Instantiation instantiation)
        {
            int hashcode = unchecked(instantiation.Length << 24);
            for (int typeArgIndex = 0; typeArgIndex < instantiation.Length; typeArgIndex++)
            {
                hashcode = unchecked(hashcode * 73 + GetHashCode(instantiation[typeArgIndex]));
            }
            return hashcode;

        }

        public static int GetHashCode(TypeDesc type)
        {
            if (type == null)
            {
                return 0;
            }
            if (type is RuntimeDeterminedType runtimeDeterminedType)
            {
                return runtimeDeterminedType.RuntimeDeterminedDetailsType.Index ^
                    ((int)runtimeDeterminedType.RuntimeDeterminedDetailsType.Kind << 30);
            }
            return type.GetTypeDefinition().GetHashCode() ^ GetHashCode(type.Instantiation);
        }

        public static int GetHashCode(MethodDesc method)
        {
            if (method == null)
            {
                return 0;
            }
            return unchecked(GetHashCode(method.OwningType) + 97 * (
                method.GetTypicalMethodDefinition().GetHashCode() + 31 * GetHashCode(method.Instantiation)));
        }

        public static int GetHashCode(MethodWithToken method)
        {
            if (method == null)
            {
                return 0;
            }
            return unchecked(GetHashCode(method.Method) + 31 * GetHashCode(method.OwningType) + 97 * GetHashCode(method.ConstrainedType));
        }

        public static int GetHashCode(FieldDesc field)
        {
            if (field == null)
            {
                return 0;
            }
            return unchecked(GetHashCode(field.OwningType) + 97 * GetHashCode(field.FieldType) + 31 * field.Name.GetHashCode());
        }
    }
}
