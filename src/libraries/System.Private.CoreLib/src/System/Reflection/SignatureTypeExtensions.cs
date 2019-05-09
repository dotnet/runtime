// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
#if CORERT
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public // Needs to be public so that Reflection.Core can see it.
#else
    internal
#endif
    static class SignatureTypeExtensions
    {
        /// <summary>
        /// This is semantically identical to 
        /// 
        ///      parameter.ParameterType == pattern.TryResolveAgainstGenericMethod(parameter.Member)
        ///      
        /// but without the allocation overhead of TryResolve.
        /// </summary>
        public static bool MatchesParameterTypeExactly(this Type pattern, ParameterInfo parameter)
        {
            if (pattern is SignatureType signatureType)
                return signatureType.MatchesExactly(parameter.ParameterType);
            else
                return pattern == (object)(parameter.ParameterType);
        }

        /// <summary>
        /// This is semantically identical to 
        /// 
        ///      actual == pattern.TryResolveAgainstGenericMethod(parameterMember)
        ///      
        /// but without the allocation overhead of TryResolve.
        /// </summary>
        internal static bool MatchesExactly(this SignatureType pattern, Type actual)
        {
            if (pattern.IsSZArray)
            {
                return actual.IsSZArray && pattern.ElementType!.MatchesExactly(actual.GetElementType()!);
            }
            else if (pattern.IsVariableBoundArray)
            {
                return actual.IsVariableBoundArray && pattern.GetArrayRank() == actual.GetArrayRank() && pattern.ElementType!.MatchesExactly(actual.GetElementType()!);
            }
            else if (pattern.IsByRef)
            {
                return actual.IsByRef && pattern.ElementType!.MatchesExactly(actual.GetElementType()!);
            }
            else if (pattern.IsPointer)
            {
                return actual.IsPointer && pattern.ElementType!.MatchesExactly(actual.GetElementType()!);
            }
            else if (pattern.IsConstructedGenericType)
            {
                if (!actual.IsConstructedGenericType)
                    return false;
                if (!(pattern.GetGenericTypeDefinition() == actual.GetGenericTypeDefinition()))
                    return false;
                Type[] patternGenericTypeArguments = pattern.GenericTypeArguments;
                Type[] actualGenericTypeArguments = actual.GenericTypeArguments;
                int count = patternGenericTypeArguments.Length;
                if (count != actualGenericTypeArguments.Length)
                    return false;
                for (int i = 0; i < count; i++)
                {
                    Type patternGenericTypeArgument = patternGenericTypeArguments[i];
                    if (patternGenericTypeArgument is SignatureType signatureType)
                    {
                        if (!signatureType.MatchesExactly(actualGenericTypeArguments[i]))
                            return false;
                    }
                    else
                    {
                        if (patternGenericTypeArgument != actualGenericTypeArguments[i])
                            return false;
                    }
                }
                return true;
            }
            else if (pattern.IsGenericMethodParameter)
            {
                if (!actual.IsGenericMethodParameter)
                    return false;
                if (pattern.GenericParameterPosition != actual.GenericParameterPosition)
                    return false;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Translates a SignatureType into its equivalent resolved Type by recursively substituting all generic parameter references
        /// with its corresponding generic parameter definition. This is slow so MatchesExactly or MatchesParameterTypeExactly should be
        /// substituted instead whenever possible. This is only used by the DefaultBinder when its fast-path checks have been exhausted and
        /// it needs to call non-trivial methods like IsAssignableFrom which SignatureTypes will never support.
        /// 
        /// Because this method is used to eliminate method candidates in a GetMethod() lookup, it is entirely possible that the Type
        /// might not be creatable due to conflicting generic constraints. Since this merely implies that this candidate is not
        /// the method we're looking for, we return null rather than let the TypeLoadException bubble up. The DefaultBinder will catch
        /// the null and continue its search for a better candidate.
        /// </summary>
        internal static Type? TryResolveAgainstGenericMethod(this SignatureType signatureType, MethodInfo genericMethod)
        {
            return signatureType.TryResolve(genericMethod.GetGenericArguments());
        }
    
        private static Type? TryResolve(this SignatureType signatureType, Type[] genericMethodParameters)
        {
            if (signatureType.IsSZArray)
            {
                return signatureType.ElementType!.TryResolve(genericMethodParameters)?.TryMakeArrayType();
            }
            else if (signatureType.IsVariableBoundArray)
            {
                return signatureType.ElementType!.TryResolve(genericMethodParameters)?.TryMakeArrayType(signatureType.GetArrayRank());
            }
            else if (signatureType.IsByRef)
            {
                return signatureType.ElementType!.TryResolve(genericMethodParameters)?.TryMakeByRefType();
            }
            else if (signatureType.IsPointer)
            {
                return signatureType.ElementType!.TryResolve(genericMethodParameters)?.TryMakePointerType();
            }
            else if (signatureType.IsConstructedGenericType)
            {
                Type[] genericTypeArguments = signatureType.GenericTypeArguments;
                int count = genericTypeArguments.Length;
                Type?[] newGenericTypeArguments = new Type[count];
                for (int i = 0; i < count; i++)
                {
                    Type genericTypeArgument = genericTypeArguments[i];
                    if (genericTypeArgument is SignatureType signatureGenericTypeArgument)
                    {
                        newGenericTypeArguments[i] = signatureGenericTypeArgument.TryResolve(genericMethodParameters);
                        if (newGenericTypeArguments[i] == null)
                            return null;
                    }
                    else
                    {
                        newGenericTypeArguments[i] = genericTypeArgument;
                    }
                }
                return signatureType.GetGenericTypeDefinition().TryMakeGenericType(newGenericTypeArguments!);
            }
            else if (signatureType.IsGenericMethodParameter)
            {
                int position = signatureType.GenericParameterPosition;
                if (position >= genericMethodParameters.Length)
                    return null;
                return genericMethodParameters[position];
            }
            else
            {
                return null;
            }
        }
    
        private static Type? TryMakeArrayType(this Type type)
        {
            try
            {
                return type.MakeArrayType();
            }
            catch
            {
                return null;
            }
        }
    
        private static Type? TryMakeArrayType(this Type type, int rank)
        {
            try
            {
                return type.MakeArrayType(rank);
            }
            catch
            {
                return null;
            }
        }
    
        private static Type? TryMakeByRefType(this Type type)
        {
            try
            {
                return type.MakeByRefType();
            }
            catch
            {
                return null;
            }
        }
    
        private static Type? TryMakePointerType(this Type type)
        {
            try
            {
                return type.MakePointerType();
            }
            catch
            {
                return null;
            }
        }
    
        private static Type? TryMakeGenericType(this Type type, Type[] instantiation)
        {
            try
            {
                return type.MakeGenericType(instantiation);
            }
            catch
            {
                return null;
            }
        }
    }
}
