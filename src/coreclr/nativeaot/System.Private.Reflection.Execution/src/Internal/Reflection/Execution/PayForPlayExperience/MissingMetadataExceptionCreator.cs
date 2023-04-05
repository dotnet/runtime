// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Text;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;

using global::Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Execution.PayForPlayExperience
{
    public static class MissingMetadataExceptionCreator
    {
        internal static NotSupportedException Create(Type? pertainant)
        {
            return CreateFromMetadataObject(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant);
        }

        private static NotSupportedException CreateFromString(string? pertainant)
        {
            if (pertainant == null)
                return new NotSupportedException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, "<unavailable>"));
            else
                return new NotSupportedException(SR.Format(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant));
        }

        internal static NotSupportedException CreateMissingArrayTypeException(Type elementType, bool isMultiDim, int rank)
        {
            Debug.Assert(rank == 1 || isMultiDim);
            string s = CreateArrayTypeStringIfAvailable(elementType, rank);
            return CreateFromString(s);
        }

        internal static NotSupportedException CreateMissingConstructedGenericTypeException(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            string s = CreateConstructedGenericTypeStringIfAvailable(genericTypeDefinition, genericTypeArguments);
            return CreateFromString(s);
        }

        internal static NotSupportedException CreateFromMetadataObject(string resourceId, Type? pertainant)
        {
            if (pertainant == null)
                return new NotSupportedException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, "<unavailable>"));

            string usefulPertainant = pertainant.ToDisplayStringIfAvailable();
            if (usefulPertainant == null)
                return new NotSupportedException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, pertainant.ToString()));
            else
                return new NotSupportedException(SR.Format(resourceId, usefulPertainant));
        }

        public static string ComputeUsefulPertainantIfPossible(MemberInfo memberInfo)
        {
            {
                StringBuilder friendlyName = new StringBuilder(memberInfo.DeclaringType.ToDisplayStringIfAvailable());
                friendlyName.Append('.');
                friendlyName.Append(memberInfo.Name);
                if (memberInfo is MethodBase method)
                {
                    bool first;

                    // write out generic parameters
                    if (method.IsConstructedGenericMethod)
                    {
                        first = true;
                        friendlyName.Append('[');
                        foreach (Type genericParameter in method.GetGenericArguments())
                        {
                            if (!first)
                                friendlyName.Append(',');

                            first = false;
                            friendlyName.Append(genericParameter.ToDisplayStringIfAvailable());
                        }
                        friendlyName.Append(']');
                    }

                    // write out actual parameters
                    friendlyName.Append('(');
                    first = true;
                    foreach (ParameterInfo parameter in method.GetParametersNoCopy())
                    {
                        if (!first)
                            friendlyName.Append(',');

                        first = false;
                        friendlyName.Append(parameter.ParameterType.ToDisplayStringIfAvailable());
                    }
                    friendlyName.Append(')');
                }

                return friendlyName.ToString();
            }
        }

        internal static string ToDisplayStringIfAvailable(this Type type)
        {
            RuntimeTypeHandle runtimeTypeHandle = ReflectionCoreExecution.ExecutionDomain.GetTypeHandleIfAvailable(type);
            bool hasRuntimeTypeHandle = !runtimeTypeHandle.Equals(default(RuntimeTypeHandle));

            if (type.HasElementType)
            {
                if (type.IsArray)
                {
                    // Multidim arrays. This is the one case where GetElementType() isn't pay-for-play safe so
                    // talk to the diagnostic mapping tables directly if possible or give up.
                    if (!hasRuntimeTypeHandle)
                        return null;

                    int rank = type.GetArrayRank();
                    return CreateArrayTypeStringIfAvailable(type.GetElementType(), rank);
                }
                else
                {
                    string s = type.GetElementType().ToDisplayStringIfAvailable();
                    if (s == null)
                        return null;
                    return s + (type.IsPointer ? "*" : "&");
                }
            }
            else if (((hasRuntimeTypeHandle && RuntimeAugments.IsGenericType(runtimeTypeHandle)) || type.IsConstructedGenericType))
            {
                Type genericTypeDefinition;
                Type[] genericTypeArguments;
                if (hasRuntimeTypeHandle)
                {
                    RuntimeTypeHandle genericTypeDefinitionHandle;
                    RuntimeTypeHandle[] genericTypeArgumentHandles;

                    genericTypeDefinitionHandle = RuntimeAugments.GetGenericInstantiation(runtimeTypeHandle, out genericTypeArgumentHandles);
                    genericTypeDefinition = Type.GetTypeFromHandle(genericTypeDefinitionHandle);
                    genericTypeArguments = new Type[genericTypeArgumentHandles.Length];
                    for (int i = 0; i < genericTypeArguments.Length; i++)
                        genericTypeArguments[i] = Type.GetTypeFromHandle(genericTypeArgumentHandles[i]);
                }
                else
                {
                    genericTypeDefinition = type.GetGenericTypeDefinition();
                    genericTypeArguments = type.GenericTypeArguments;
                }

                return CreateConstructedGenericTypeStringIfAvailable(genericTypeDefinition, genericTypeArguments);
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            else if (hasRuntimeTypeHandle)
            {
                string s;
                if (!DiagnosticMappingTables.TryGetDiagnosticStringForNamedType(runtimeTypeHandle, out s))
                    return null;

                return s;
            }
            else
            {
                return type.FullName;
            }
        }

        private static string CreateArrayTypeStringIfAvailable(Type elementType, int rank)
        {
            string s = elementType.ToDisplayStringIfAvailable();
            if (s == null)
                return null;

            return s + "[" + new string(',', rank - 1) + "]";  // This does not bother to display multidims of rank 1 correctly since we bail on that case in the prior statement.
        }

        private static string CreateConstructedGenericTypeStringIfAvailable(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            string genericTypeDefinitionString = genericTypeDefinition.ToDisplayStringIfAvailable();

            if (genericTypeDefinitionString == null)
                return null;

            StringBuilder genericTypeName = new StringBuilder(genericTypeDefinitionString);
            genericTypeName.Append('[');
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                if (i > 0)
                    genericTypeName.Append(", ");
                genericTypeName.Append(genericTypeArguments[i].ToDisplayStringIfAvailable());
            }
            genericTypeName.Append(']');

            return genericTypeName.ToString();
        }
    }
}
