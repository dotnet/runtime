// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal static class GenericServices
    {
        internal static IList<Type> GetPureGenericParameters(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type.IsGenericType && type.ContainsGenericParameters)
            {
                List<Type> pureGenericParameters = new List<Type>();
                TraverseGenericType(type, (Type t) =>
                {
                    if (t.IsGenericParameter)
                    {
                        pureGenericParameters.Add(t);
                    }
                });
                return pureGenericParameters;
            }
            else
            {
                return Type.EmptyTypes;
            }
        }

        internal static int GetPureGenericArity(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            int genericArity = 0;
            if (type.IsGenericType && type.ContainsGenericParameters)
            {
                TraverseGenericType(type, (Type t) =>
                {
                    if (t.IsGenericParameter)
                    {
                        genericArity++;
                    }
                });
            }
            return genericArity;
        }

        private static void TraverseGenericType(Type type, Action<Type> onType)
        {
            if (type.IsGenericType)
            {
                foreach (Type genericArgument in type.GetGenericArguments())
                {
                    TraverseGenericType(genericArgument, onType);
                }
            }
            onType(type);
        }

        public static int[] GetGenericParametersOrder(Type type)
        {
            return type.GetPureGenericParameters().Select(parameter => parameter.GenericParameterPosition).ToArray();
        }

        public static string GetGenericName(string originalGenericName, int[] genericParametersOrder, int genericArity)
        {
            string[] genericFormatArgs = new string[genericArity];
            for (int i = 0; i < genericParametersOrder.Length; i++)
            {
                genericFormatArgs[genericParametersOrder[i]] = $"{{{i}}}";
            }
            return string.Format(CultureInfo.InvariantCulture, originalGenericName, genericFormatArgs);
        }

        public static T[] Reorder<T>(T[] original, int[] genericParametersOrder)
        {
            T[] genericSpecialization = new T[genericParametersOrder.Length];
            for (int i = 0; i < genericParametersOrder.Length; i++)
            {
                genericSpecialization[i] = original[genericParametersOrder[i]];
            }
            return genericSpecialization;
        }

        [return: NotNullIfNotNull("types")]
        public static IEnumerable<Type>? CreateTypeSpecializations(this Type[]? types, Type[] specializationTypes)
        {
            if (types == null)
            {
                return null;
            }
            else
            {
                return types.Select(type => type.CreateTypeSpecialization(specializationTypes));
            }
        }

        public static Type CreateTypeSpecialization(this Type type, Type[] specializationTypes)
        {
            if (!type.ContainsGenericParameters)
            {
                return type;
            }

            if (type.IsGenericParameter)
            {
                // the only case when MakeGenericType won't work is when the 'type' represents a "naked" generic type
                // in this case we simply grab the type with the proper index from the specializtion
                return specializationTypes[type.GenericParameterPosition];
            }
            else
            {
                Type[] typeGenericArguments = type.GetGenericArguments();
                Type[] subSpecialization = new Type[typeGenericArguments.Length];

                for (int i = 0; i < typeGenericArguments.Length; i++)
                {
                    Type typeGenericArgument = typeGenericArguments[i];
                    subSpecialization[i] = typeGenericArgument.IsGenericParameter ?
                        specializationTypes[typeGenericArgument.GenericParameterPosition] : typeGenericArgument;

                }

                // and "close" the generic
                return type.GetGenericTypeDefinition().MakeGenericType(subSpecialization);
            }

        }

        public static bool CanSpecialize(Type? type, IEnumerable<Type>? constraints, GenericParameterAttributes attributes)
        {
            return CanSpecialize(type, constraints) && CanSpecialize(type!, attributes);
        }

        public static bool CanSpecialize(Type? type, IEnumerable<Type>? constraintTypes)
        {
            if (constraintTypes == null)
            {
                return true;
            }

            // where T : IFoo
            // a part of where T : struct is also handled here as T : ValueType
            foreach (Type constraintType in constraintTypes)
            {
                if ((constraintType != null) && !constraintType.IsAssignableFrom(type))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CanSpecialize(Type type, GenericParameterAttributes attributes)
        {
            if (attributes == GenericParameterAttributes.None)
            {
                return true;
            }

            // where T : class
            if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                if (type.IsValueType)
                {
                    return false;
                }
            }

            // where T : new
            if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                // value types always have default constructors
                if (!type.IsValueType && (type.GetConstructor(Type.EmptyTypes) == null))
                {
                    return false;
                }
            }

            // where T : struct
            if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                // must be a value type
                if (!type.IsValueType)
                {
                    return false;
                }

                // Make sure that the type is not nullable
                // this is salways guaranteed in C#, but other languages may be different
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    return false;
                }
            }

            // all other fals indicate variance and don't place any actual restrictions on the generic parameters
            // but rather how they should be used by the compiler
            return true;
        }
    }
}
