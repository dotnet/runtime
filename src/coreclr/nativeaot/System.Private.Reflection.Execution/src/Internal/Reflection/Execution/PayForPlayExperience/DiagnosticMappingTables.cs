// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Text;
using global::System.Collections.Generic;

using global::Internal.Metadata.NativeFormat;

using global::Internal.Runtime.Augments;

using System.Reflection.Runtime.General;

namespace Internal.Reflection.Execution.PayForPlayExperience
{
    internal static partial class DiagnosticMappingTables
    {
        // Get the diagnostic name string for a type. This attempts to reformat the string into something that is essentially human readable.
        //  Returns true if the function is successful.
        //  runtimeTypeHandle represents the type to get a name for
        //  diagnosticName is the name that is returned
        //
        // the genericParameterOffsets list is an optional parameter that contains the list of the locations of where generic parameters may be inserted
        // to make the string represent an instantiated generic.
        //
        // For example for Dictionary<K,V>, metadata names the type Dictionary`2, but this function will return Dictionary<,>
        // For consumers of this function that will be inserting generic arguments, the genericParameterOffsets list is used to find where to insert the generic parameter name.
        //
        // That isn't all that interesting for Dictionary, but it becomes substantially more interesting for nested generic types, or types which are compiler named as
        // those may contain embedded <> pairs and such.
        public static bool TryGetDiagnosticStringForNamedType(RuntimeTypeHandle runtimeTypeHandle, out string diagnosticName, List<int> genericParameterOffsets)
        {
            diagnosticName = null;
            ExecutionEnvironmentImplementation executionEnvironment = ReflectionExecution.ExecutionEnvironment;

            MetadataReader reader;
            TypeReferenceHandle typeReferenceHandle;
            if (executionEnvironment.TryGetTypeReferenceForNamedType(runtimeTypeHandle, out reader, out typeReferenceHandle))
            {
                diagnosticName = GetTypeFullNameFromTypeRef(typeReferenceHandle, reader, genericParameterOffsets);
                return true;
            }

            QTypeDefinition qTypeDefinition;
            if (executionEnvironment.TryGetMetadataForNamedType(runtimeTypeHandle, out qTypeDefinition))
            {
                TryGetFullNameFromTypeDefEcma(qTypeDefinition, genericParameterOffsets, ref diagnosticName);
                if (diagnosticName != null)
                    return true;

                if (qTypeDefinition.IsNativeFormatMetadataBased)
                {
                    TypeDefinitionHandle typeDefinitionHandle = qTypeDefinition.NativeFormatHandle;
                    diagnosticName = GetTypeFullNameFromTypeDef(typeDefinitionHandle, qTypeDefinition.NativeFormatReader, genericParameterOffsets);
                    return true;
                }
            }
            return false;
        }

        static partial void TryGetFullNameFromTypeDefEcma(QTypeDefinition qTypeDefinition, List<int> genericParameterOffsets, ref string result);

        private static string GetTypeFullNameFromTypeRef(TypeReferenceHandle typeReferenceHandle, MetadataReader reader, List<int> genericParameterOffsets)
        {
            TypeReference typeReference = typeReferenceHandle.GetTypeReference(reader);
            string s = typeReference.TypeName.GetString(reader);
            Handle parentHandle = typeReference.ParentNamespaceOrType;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.TypeReference)
            {
                string containingTypeName = GetTypeFullNameFromTypeRef(parentHandle.ToTypeReferenceHandle(reader), reader, genericParameterOffsets);
                s = containingTypeName + "." + s;
            }
            else if (parentHandleType == HandleType.NamespaceReference)
            {
                NamespaceReferenceHandle namespaceReferenceHandle = parentHandle.ToNamespaceReferenceHandle(reader);
                for (;;)
                {
                    NamespaceReference namespaceReference = namespaceReferenceHandle.GetNamespaceReference(reader);
                    string namespacePart = namespaceReference.Name.GetStringOrNull(reader);
                    if (namespacePart == null)
                        break; // Reached the root namespace.
                    s = namespacePart + "." + s;
                    if (namespaceReference.ParentScopeOrNamespace.HandleType != HandleType.NamespaceReference)
                        break; // Should have reached the root namespace first but this helper is for ToString() - better to
                    // return partial information than crash.
                    namespaceReferenceHandle = namespaceReference.ParentScopeOrNamespace.ToNamespaceReferenceHandle(reader);
                }
            }
            else
            {
                // If we got here, the metadata is illegal but this helper is for ToString() - better to
                // return something partial than throw.
            }
            return ConvertBackTickNameToNameWithReducerInputFormat(s, genericParameterOffsets);
        }

        public static string ConvertBackTickNameToNameWithReducerInputFormat(string typename, List<int> genericParameterOffsets)
        {
            int indexOfBackTick = typename.LastIndexOf('`');
            if (indexOfBackTick != -1)
            {
                string typeNameSansBackTick = typename.Substring(0, indexOfBackTick);
                if ((indexOfBackTick + 1) < typename.Length)
                {
                    string textAfterBackTick = typename.Substring(indexOfBackTick + 1);
                    int genericParameterCount;
                    if (int.TryParse(textAfterBackTick, out genericParameterCount) && (genericParameterCount > 0))
                    {
                        // Replace the `Number with <,,,> where the count of ',' is one less than Number.
                        StringBuilder genericTypeName = new StringBuilder();
                        genericTypeName.Append(typeNameSansBackTick);
                        genericTypeName.Append('<');
                        genericParameterOffsets?.Add(genericTypeName.Length);
                        for (int i = 1; i < genericParameterCount; i++)
                        {
                            genericTypeName.Append(',');
                            genericParameterOffsets?.Add(genericTypeName.Length);
                        }
                        genericTypeName.Append('>');
                        return genericTypeName.ToString();
                    }
                }
            }
            return typename;
        }

        private static string GetTypeFullNameFromTypeDef(TypeDefinitionHandle typeDefinitionHandle, MetadataReader reader, List<int> genericParameterOffsets)
        {
            string s;

            TypeDefinition typeDefinition = typeDefinitionHandle.GetTypeDefinition(reader);
            s = typeDefinition.Name.GetString(reader);

            TypeDefinitionHandle enclosingTypeDefHandle = typeDefinition.EnclosingType;
            if (!enclosingTypeDefHandle.IsNull(reader))
            {
                string containingTypeName = GetTypeFullNameFromTypeDef(enclosingTypeDefHandle, reader, genericParameterOffsets);
                s = containingTypeName + "." + s;
            }
            else
            {
                NamespaceDefinitionHandle namespaceHandle = typeDefinition.NamespaceDefinition;
                for (;;)
                {
                    NamespaceDefinition namespaceDefinition = namespaceHandle.GetNamespaceDefinition(reader);
                    string namespacePart = namespaceDefinition.Name.GetStringOrNull(reader);
                    if (namespacePart == null)
                        break; // Reached the root namespace.
                    s = namespacePart + "." + s;
                    if (namespaceDefinition.ParentScopeOrNamespace.HandleType != HandleType.NamespaceDefinition)
                        break; // Should have reached the root namespace first but this helper is for ToString() - better to
                    // return partial information than crash.
                    namespaceHandle = namespaceDefinition.ParentScopeOrNamespace.ToNamespaceDefinitionHandle(reader);
                }
            }
            return ConvertBackTickNameToNameWithReducerInputFormat(s, genericParameterOffsets);
        }

        public static bool TryGetArrayTypeElementType(RuntimeTypeHandle arrayTypeHandle, out RuntimeTypeHandle elementTypeHandle)
        {
            elementTypeHandle = RuntimeAugments.GetRelatedParameterTypeHandle(arrayTypeHandle);
            return true;
        }

        public static bool TryGetPointerTypeTargetType(RuntimeTypeHandle pointerTypeHandle, out RuntimeTypeHandle targetTypeHandle)
        {
            targetTypeHandle = RuntimeAugments.GetRelatedParameterTypeHandle(pointerTypeHandle);
            return true;
        }
    }
}
