// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.TypeLoading
{
    internal static class Assignability
    {
        public static bool IsAssignableFrom(Type toTypeInfo, Type fromTypeInfo, CoreTypes coreTypes)
        {
            if (toTypeInfo == null)
                throw new NullReferenceException();
            if (fromTypeInfo == null)
                return false;   // It would be more appropriate to throw ArgumentNullException here, but returning "false" is the desktop-compat behavior.

            if (fromTypeInfo.Equals(toTypeInfo))
                return true;

            if (toTypeInfo.IsGenericTypeDefinition)
            {
                // Asking whether something can cast to a generic type definition is arguably meaningless. The .NET Framework CLR Reflection layer converts all
                // generic type definitions to generic type instantiations closed over the formal generic type parameters. The .NET Native framework
                // keeps the two separate. Fortunately, under either interpretation, returning "false" unless the two types are identical is still a
                // defensible behavior. To avoid having the rest of the code deal with the differing interpretations, we'll short-circuit this now.
                return false;
            }

            if (fromTypeInfo.IsGenericTypeDefinition)
            {
                // The .NET Framework CLR Reflection layer converts all generic type definitions to generic type instantiations closed over the formal
                // generic type parameters. The .NET Native framework keeps the two separate. For the purpose of IsAssignableFrom(),
                // it makes sense to unify the two for the sake of backward compat. We'll just make the transform here so that the rest of code
                // doesn't need to know about this quirk.
                fromTypeInfo = fromTypeInfo.GetGenericTypeDefinition().MakeGenericType(fromTypeInfo.GetGenericTypeParameters());
            }

            if (fromTypeInfo.CanCastTo(toTypeInfo, coreTypes))
                return true;

            // .NET Framework compat: IsAssignableFrom() considers T as assignable to Nullable<T> (but does not check if T is a generic parameter.)
            if (!fromTypeInfo.IsGenericParameter)
            {
                if (toTypeInfo.IsConstructedGenericType && toTypeInfo.GetGenericTypeDefinition() == coreTypes[CoreType.NullableT])
                {
                    Type nullableUnderlyingType = toTypeInfo.GenericTypeArguments[0];
                    if (nullableUnderlyingType.Equals(fromTypeInfo))
                        return true;
                }
            }
            return false;
        }

        private static bool CanCastTo(this Type fromTypeInfo, Type toTypeInfo, CoreTypes coreTypes)
        {
            if (fromTypeInfo.Equals(toTypeInfo))
                return true;

            if (fromTypeInfo.IsArray)
            {
                if (toTypeInfo.IsInterface)
                    return fromTypeInfo.CanCastArrayToInterface(toTypeInfo, coreTypes);

                if (fromTypeInfo.IsSubclassOf(toTypeInfo))
                    return true;  // T[] is castable to Array or Object.

                if (!toTypeInfo.IsArray)
                    return false;

                int rank = fromTypeInfo.GetArrayRank();
                if (rank != toTypeInfo.GetArrayRank())
                    return false;

                bool fromTypeIsSzArray = fromTypeInfo.IsSZArray();
                bool toTypeIsSzArray = toTypeInfo.IsSZArray();
                if (fromTypeIsSzArray != toTypeIsSzArray)
                {
                    // T[] is assignable to T[*] but not vice-versa.
                    if (!(rank == 1 && !toTypeIsSzArray))
                    {
                        return false; // T[*] is not castable to T[]
                    }
                }

                Type toElementTypeInfo = toTypeInfo.GetElementType()!;
                Type fromElementTypeInfo = fromTypeInfo.GetElementType()!;
                return fromElementTypeInfo.IsElementTypeCompatibleWith(toElementTypeInfo, coreTypes);
            }

            if (fromTypeInfo.IsByRef)
            {
                if (!toTypeInfo.IsByRef)
                    return false;

                Type toElementTypeInfo = toTypeInfo.GetElementType()!;
                Type fromElementTypeInfo = fromTypeInfo.GetElementType()!;
                return fromElementTypeInfo.IsElementTypeCompatibleWith(toElementTypeInfo, coreTypes);
            }

            if (fromTypeInfo.IsPointer)
            {
                if (toTypeInfo.Equals(coreTypes[CoreType.Object]))
                    return true;  // T* is castable to Object.

                if (toTypeInfo.Equals(coreTypes[CoreType.UIntPtr]))
                    return true;  // T* is castable to UIntPtr (but not IntPtr)

                if (!toTypeInfo.IsPointer)
                    return false;

                Type toElementTypeInfo = toTypeInfo.GetElementType()!;
                Type fromElementTypeInfo = fromTypeInfo.GetElementType()!;
                return fromElementTypeInfo.IsElementTypeCompatibleWith(toElementTypeInfo, coreTypes);
            }

            if (fromTypeInfo.IsGenericParameter)
            {
                //
                // A generic parameter can be cast to any of its constraints, or object, if none are specified, or ValueType if the "struct" constraint is
                // specified.
                //
                // This has to be coded as its own case as TypeInfo.BaseType on a generic parameter doesn't always return what you'd expect.
                //
                if (toTypeInfo.Equals(coreTypes[CoreType.Object]))
                    return true;

                if (toTypeInfo.Equals(coreTypes[CoreType.ValueType]))
                {
                    GenericParameterAttributes attributes = fromTypeInfo.GenericParameterAttributes;
                    if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        return true;
                }

                foreach (Type constraintType in fromTypeInfo.GetGenericParameterConstraints())
                {
                    if (constraintType.CanCastTo(toTypeInfo, coreTypes))
                        return true;
                }

                return false;
            }

            if (toTypeInfo.IsArray || toTypeInfo.IsByRef || toTypeInfo.IsPointer || toTypeInfo.IsGenericParameter)
                return false;

            if (fromTypeInfo.MatchesWithVariance(toTypeInfo, coreTypes))
                return true;

            if (toTypeInfo.IsInterface)
            {
                foreach (Type ifc in fromTypeInfo.GetInterfaces())
                {
                    if (ifc.MatchesWithVariance(toTypeInfo, coreTypes))
                        return true;
                }
                return false;
            }
            else
            {
                // Interfaces are always castable to System.Object. The code below will not catch this as interfaces report their BaseType as null.
                if (toTypeInfo.Equals(coreTypes[CoreType.Object]) && fromTypeInfo.IsInterface)
                    return true;

                Type walk = fromTypeInfo;
                while (true)
                {
                    Type? baseType = walk.BaseType;
                    if (baseType == null)
                        return false;
                    walk = baseType;
                    if (walk.MatchesWithVariance(toTypeInfo, coreTypes))
                        return true;
                }
            }
        }

        //
        // Check a base type or implemented interface type for equivalence (taking into account variance for generic instantiations.)
        // Does not check ancestors recursively.
        //
        private static bool MatchesWithVariance(this Type fromTypeInfo, Type toTypeInfo, CoreTypes coreTypes)
        {
            Debug.Assert(!(fromTypeInfo.IsArray || fromTypeInfo.IsByRef || fromTypeInfo.IsPointer || fromTypeInfo.IsGenericParameter));
            Debug.Assert(!(toTypeInfo.IsArray || toTypeInfo.IsByRef || toTypeInfo.IsPointer || toTypeInfo.IsGenericParameter));

            if (fromTypeInfo.Equals(toTypeInfo))
                return true;

            if (!(fromTypeInfo.IsConstructedGenericType && toTypeInfo.IsConstructedGenericType))
                return false;

            Type genericTypeDefinition = fromTypeInfo.GetGenericTypeDefinition();
            if (!genericTypeDefinition.Equals(toTypeInfo.GetGenericTypeDefinition()))
                return false;

            Type[] fromTypeArguments = fromTypeInfo.GenericTypeArguments;
            Type[] toTypeArguments = toTypeInfo.GenericTypeArguments;
            Type[] genericTypeParameters = genericTypeDefinition.GetGenericTypeParameters();
            for (int i = 0; i < genericTypeParameters.Length; i++)
            {
                Type fromTypeArgumentInfo = fromTypeArguments[i];
                Type toTypeArgumentInfo = toTypeArguments[i];

                GenericParameterAttributes attributes = genericTypeParameters[i].GenericParameterAttributes;
                switch (attributes & GenericParameterAttributes.VarianceMask)
                {
                    case GenericParameterAttributes.Covariant:
                        if (!(fromTypeArgumentInfo.IsGcReferenceTypeAndCastableTo(toTypeArgumentInfo, coreTypes)))
                            return false;
                        break;

                    case GenericParameterAttributes.Contravariant:
                        if (!(toTypeArgumentInfo.IsGcReferenceTypeAndCastableTo(fromTypeArgumentInfo, coreTypes)))
                            return false;
                        break;

                    case GenericParameterAttributes.None:
                        if (!(fromTypeArgumentInfo.Equals(toTypeArgumentInfo)))
                            return false;
                        break;

                    default:
                        throw new BadImageFormatException();  // Unexpected variance value in metadata.
                }
            }
            return true;
        }

        //
        // A[] can cast to B[] if one of the following are true:
        //
        //    A can cast to B under variance rules.
        //
        //    A and B are both integers or enums and have the same reduced type (i.e. represent the same-sized integer, ignoring signed/unsigned differences.)
        //        "char" is not interchangeable with short/ushort. "bool" is not interchangeable with byte/sbyte.
        //
        // For .NET Framework compat, A& and A* follow the same rules.
        //
        private static bool IsElementTypeCompatibleWith(this Type fromTypeInfo, Type toTypeInfo, CoreTypes coreTypes)
        {
            if (fromTypeInfo.IsGcReferenceTypeAndCastableTo(toTypeInfo, coreTypes))
                return true;

            Type reducedFromType = fromTypeInfo.ReducedType(coreTypes);
            Type reducedToType = toTypeInfo.ReducedType(coreTypes);
            if (reducedFromType.Equals(reducedToType))
                return true;

            return false;
        }

        private static Type ReducedType(this Type t, CoreTypes coreTypes)
        {
            if (t.IsEnum)
                t = t.GetEnumUnderlyingType();

            if (t.Equals(coreTypes[CoreType.Byte]))
                return coreTypes[CoreType.SByte] ?? throw new TypeLoadException(SR.Format(SR.CoreTypeNotFound, "System.SByte"));

            if (t.Equals(coreTypes[CoreType.UInt16]))
                return coreTypes[CoreType.Int16] ?? throw new TypeLoadException(SR.Format(SR.CoreTypeNotFound, "System.Int16"));

            if (t.Equals(coreTypes[CoreType.UInt32]))
                return coreTypes[CoreType.Int32] ?? throw new TypeLoadException(SR.Format(SR.CoreTypeNotFound, "System.Int32"));

            if (t.Equals(coreTypes[CoreType.UInt64]))
                return coreTypes[CoreType.Int64] ?? throw new TypeLoadException(SR.Format(SR.CoreTypeNotFound, "System.Int64"));

            return t;
        }

        //
        // Contra/CoVariance.
        //
        // IEnumerable<D> can cast to IEnumerable<B> if D can cast to B and if there's no possibility that D is a value type.
        //
        private static bool IsGcReferenceTypeAndCastableTo(this Type fromTypeInfo, Type toTypeInfo, CoreTypes coreTypes)
        {
            if (fromTypeInfo.Equals(toTypeInfo))
                return true;

            if (fromTypeInfo.ProvablyAGcReferenceType(coreTypes))
                return fromTypeInfo.CanCastTo(toTypeInfo, coreTypes);

            return false;
        }

        //
        // A true result indicates that a type can never be a value type. This is important when testing variance-compatibility.
        //
        private static bool ProvablyAGcReferenceType(this Type t, CoreTypes coreTypes)
        {
            if (t.IsGenericParameter)
            {
                GenericParameterAttributes attributes = t.GenericParameterAttributes;
                if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    return true;   // generic parameter with a "class" constraint.
            }

            return t.ProvablyAGcReferenceTypeHelper(coreTypes);
        }

        private static bool ProvablyAGcReferenceTypeHelper(this Type t, CoreTypes coreTypes)
        {
            if (t.IsArray)
                return true;

            if (t.IsByRef || t.IsPointer)
                return false;

            if (t.IsGenericParameter)
            {
                // We intentionally do not check for a "class" constraint on generic parameter ancestors.
                // That's because this property does not propagate up the constraining hierarchy.
                // (e.g. "class A<S, T> where S : T, where T : class" does not guarantee that S is a class.)

                foreach (Type constraintType in t.GetGenericParameterConstraints())
                {
                    if (constraintType.ProvablyAGcReferenceTypeHelper(coreTypes))
                        return true;
                }
                return false;
            }

            return t.IsClass && !t.Equals(coreTypes[CoreType.Object]) && !t.Equals(coreTypes[CoreType.ValueType]) && !t.Equals(coreTypes[CoreType.Enum]);
        }

        //
        // T[] casts to IList<T>. This could be handled by the normal ancestor-walking code
        // but for one complication: T[] also casts to IList<U> if T[] casts to U[].
        //
        private static bool CanCastArrayToInterface(this Type fromTypeInfo, Type toTypeInfo, CoreTypes coreTypes)
        {
            Debug.Assert(fromTypeInfo.IsArray);
            Debug.Assert(toTypeInfo.IsInterface);

            if (toTypeInfo.IsConstructedGenericType)
            {
                Type[] toTypeGenericTypeArguments = toTypeInfo.GenericTypeArguments;
                if (toTypeGenericTypeArguments.Length != 1)
                    return false;
                Type toElementTypeInfo = toTypeGenericTypeArguments[0];

                Type toTypeGenericTypeDefinition = toTypeInfo.GetGenericTypeDefinition();
                Type? fromElementTypeInfo = fromTypeInfo.GetElementType();
                foreach (Type ifc in fromTypeInfo.GetInterfaces())
                {
                    if (ifc.IsConstructedGenericType)
                    {
                        Type ifcGenericTypeDefinition = ifc.GetGenericTypeDefinition();
                        if (ifcGenericTypeDefinition.Equals(toTypeGenericTypeDefinition))
                        {
                            if (fromElementTypeInfo!.IsElementTypeCompatibleWith(toElementTypeInfo, coreTypes))
                                return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                foreach (Type ifc in fromTypeInfo.GetInterfaces())
                {
                    if (ifc.Equals(toTypeInfo))
                        return true;
                }
                return false;
            }
        }
    }
}
