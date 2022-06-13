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
        internal static MissingMetadataException Create(string resourceId, MemberInfo? pertainant)
        {
            return CreateFromMetadataObject(resourceId, pertainant);
        }

        internal static MissingMetadataException Create(TypeInfo? pertainant)
        {
            return CreateFromMetadataObject(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant);
        }

        internal static MissingMetadataException Create(TypeInfo? pertainant, string nestedTypeName)
        {
            if (pertainant == null)
                return new MissingMetadataException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, "<unavailable>"));

            string usefulPertainant = ComputeUsefulPertainantIfPossible(pertainant);
            if (usefulPertainant == null)
                return new MissingMetadataException(Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, pertainant.ToString()));
            else
            {
                usefulPertainant = usefulPertainant + "." + DiagnosticMappingTables.ConvertBackTickNameToNameWithReducerInputFormat(nestedTypeName, null);
                return new MissingMetadataException(Format(SR.Reflection_InsufficientMetadata_EdbNeeded, usefulPertainant));
            }
        }

        internal static MissingMetadataException Create(Type? pertainant)
        {
            return CreateFromMetadataObject(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant);
        }

        internal static MissingMetadataException Create(RuntimeTypeHandle pertainant)
        {
            return CreateFromMetadataObject(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant);
        }

        private static MissingMetadataException CreateFromString(string? pertainant)
        {
            if (pertainant == null)
                return new MissingMetadataException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, "<unavailable>"));
            else
                return new MissingMetadataException(Format(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant));
        }

        internal static MissingMetadataException CreateMissingArrayTypeException(Type elementType, bool isMultiDim, int rank)
        {
            Debug.Assert(rank == 1 || isMultiDim);
            string s = CreateArrayTypeStringIfAvailable(elementType, rank);
            return CreateFromString(s);
        }

        internal static MissingMetadataException CreateMissingConstructedGenericTypeException(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            string s = CreateConstructedGenericTypeStringIfAvailable(genericTypeDefinition, genericTypeArguments);
            return CreateFromString(s);
        }

        internal static MissingMetadataException CreateFromMetadataObject(string resourceId, object? pertainant)
        {
            if (pertainant == null)
                return new MissingMetadataException(SR.Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, "<unavailable>"));

            string usefulPertainant = ComputeUsefulPertainantIfPossible(pertainant);
            if (usefulPertainant == null)
                return new MissingMetadataException(Format(SR.Reflection_InsufficientMetadata_NoHelpAvailable, pertainant.ToString()));
            else
                return new MissingMetadataException(Format(resourceId, usefulPertainant));
        }

        public static string ComputeUsefulPertainantIfPossible(object pertainant)
        {
            {
                Type type = null;

                if (pertainant is TypeInfo)
                    type = ((TypeInfo)pertainant).AsType();
                else if (pertainant is Type)
                    type = (Type)pertainant;
                else if (pertainant is RuntimeTypeHandle)
                    type = Type.GetTypeFromHandle((RuntimeTypeHandle)pertainant);

                if (type != null)
                    return type.ToDisplayStringIfAvailable(null);
            }

            if (pertainant is MemberInfo)
            {
                MemberInfo memberInfo = (MemberInfo)pertainant;

                StringBuilder friendlyName = new StringBuilder(memberInfo.DeclaringType.ToDisplayStringIfAvailable(null));
                friendlyName.Append('.');
                friendlyName.Append(memberInfo.Name);
                if (pertainant is MethodBase)
                {
                    MethodBase method = (MethodBase)pertainant;
                    bool first;

                    // write out generic parameters
                    if (method.IsConstructedGenericMethod)
                    {
                        first = true;
                        friendlyName.Append('<');
                        foreach (Type genericParameter in method.GetGenericArguments())
                        {
                            if (!first)
                                friendlyName.Append(',');

                            first = false;
                            friendlyName.Append(genericParameter.ToDisplayStringIfAvailable(null));
                        }
                        friendlyName.Append('>');
                    }

                    // write out actual parameters
                    friendlyName.Append('(');
                    first = true;
                    foreach (ParameterInfo parameter in method.GetParametersNoCopy())
                    {
                        if (!first)
                            friendlyName.Append(',');

                        first = false;
                        if (parameter.IsOut && parameter.IsIn)
                        {
                            friendlyName.Append("ref ");
                        }
                        else if (parameter.IsOut)
                        {
                            friendlyName.Append("out ");
                        }

                        Type parameterType = parameter.ParameterType;
                        if (parameterType.IsByRef)
                        {
                            parameterType = parameterType.GetElementType();
                        }

                        friendlyName.Append(parameter.ParameterType.ToDisplayStringIfAvailable(null));
                    }
                    friendlyName.Append(')');
                }

                return friendlyName.ToString();
            }

            return null;  //Give up
        }

        internal static string ToDisplayStringIfAvailable(this Type type, List<int> genericParameterOffsets)
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
                    string s = type.GetElementType().ToDisplayStringIfAvailable(null);
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
                if (!DiagnosticMappingTables.TryGetDiagnosticStringForNamedType(runtimeTypeHandle, out s, genericParameterOffsets))
                    return null;

                return s;
            }
            else
            {
                // First, see if Type.Name is available. If Type.Name is available, then we can be reasonably confident that it is safe to call Type.FullName.
                // We'll still wrap the call in a try-catch as a failsafe.
                string s = type.InternalNameIfAvailable;
                if (s == null)
                    return null;

                try
                {
                    s = type.FullName;
                }
                catch (MissingMetadataException)
                {
                }

                // Insert commas so that CreateConstructedGenericTypeStringIfAvailable can fill the blanks.
                // This is not strictly correct for types nested under generic types, but at this point we're doing
                // best effort within reason.
                if (type.IsGenericTypeDefinition)
                {
                    s += "[";
                    int genericArgCount = type.GetGenericArguments().Length;
                    while (genericArgCount-- > 0)
                    {
                        genericParameterOffsets.Add(s.Length);
                        if (genericArgCount > 0)
                            s = s + ",";
                    }
                    s += "]";
                }

                return s;
            }
        }

        private static string CreateArrayTypeStringIfAvailable(Type elementType, int rank)
        {
            string s = elementType.ToDisplayStringIfAvailable(null);
            if (s == null)
                return null;

            return s + "[" + new string(',', rank - 1) + "]";  // This does not bother to display multidims of rank 1 correctly since we bail on that case in the prior statement.
        }

        private static string CreateConstructedGenericTypeStringIfAvailable(Type genericTypeDefinition, Type[] genericTypeArguments)
        {
            List<int> genericParameterOffsets = new List<int>();
            string genericTypeDefinitionString = genericTypeDefinition.ToDisplayStringIfAvailable(genericParameterOffsets);

            if (genericTypeDefinitionString == null)
                return null;

            // If we found too many generic arguments to insert things, strip out the excess. This is wrong, but also, nothing is right.
            if (genericTypeArguments.Length < genericParameterOffsets.Count)
            {
                genericParameterOffsets.RemoveRange(genericTypeArguments.Length, genericParameterOffsets.Count - genericTypeArguments.Length);
            }
            // Similarly, if we found too few, add them at the end.
            while (genericTypeArguments.Length > genericParameterOffsets.Count)
            {
                genericTypeDefinitionString = genericTypeDefinitionString + ",";
                genericParameterOffsets.Add(genericTypeDefinitionString.Length);
            }

            // Ensure the list is sorted in ascending order
            genericParameterOffsets.Sort();

            // The s string Now contains a string like "Namespace.MoreNamespace.TypeName.NestedGenericType<,,>.MoreNestedGenericType<>"
            // where the generic parameters locations are recorded in genericParameterOffsets
            // Walk backwards through the generic parameter locations, filling in as needed.
            StringBuilder genericTypeName = new StringBuilder(genericTypeDefinitionString);
            for (int i = genericParameterOffsets.Count - 1; i >= 0; --i)
            {
                genericTypeName.Insert(genericParameterOffsets[i], genericTypeArguments[i].ToDisplayStringIfAvailable(null));
            }

            return genericTypeName.ToString();
        }

        //
        // This is a workaround to prevent crucial information being lost when compiling console apps using the retail ILC.
        // This combination turns rich error messages from the framework into resource keys without the substitution strings.
        // We'll detect this case here and append the substitution string manually.
        //
        private static string Format(string resourceMessage, object parameter)
        {
            if (resourceMessage.Contains("{0}"))
                return SR.Format(resourceMessage, parameter);

            // If the rich exception message was eaten by the IL2IL transform, make sure the resulting message
            // has a link pointing the user towards the .NET Native debugging guide. These get normally appended
            // to the restricted message by the transform, but the pattern here is not recognized by the rewriter.
            // At this point we know the message doesn't come from resources (because message == resource key), so
            // we can't do much to make this localizable.
            return resourceMessage + ": " + parameter + ". For more information, visit http://go.microsoft.com/fwlink/?LinkId=623485";
        }
    }
}
