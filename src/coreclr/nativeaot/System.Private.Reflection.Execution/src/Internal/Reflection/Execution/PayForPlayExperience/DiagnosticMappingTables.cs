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
        // Get the diagnostic name string for a type.
        //  Returns true if the function is successful.
        //  runtimeTypeHandle represents the type to get a name for
        //  diagnosticName is the name that is returned
        public static bool TryGetDiagnosticStringForNamedType(RuntimeTypeHandle runtimeTypeHandle, out string diagnosticName)
        {
            diagnosticName = null;
            ExecutionEnvironmentImplementation executionEnvironment = ReflectionExecution.ExecutionEnvironment;

            MetadataReader reader;
            TypeReferenceHandle typeReferenceHandle;
            if (executionEnvironment.TryGetTypeReferenceForNamedType(runtimeTypeHandle, out reader, out typeReferenceHandle))
            {
                diagnosticName = GetTypeFullNameFromTypeRef(typeReferenceHandle, reader);
                return true;
            }

            QTypeDefinition qTypeDefinition;
            if (executionEnvironment.TryGetMetadataForNamedType(runtimeTypeHandle, out qTypeDefinition))
            {
                TryGetFullNameFromTypeDefEcma(qTypeDefinition, ref diagnosticName);
                if (diagnosticName != null)
                    return true;

                if (qTypeDefinition.IsNativeFormatMetadataBased)
                {
                    TypeDefinitionHandle typeDefinitionHandle = qTypeDefinition.NativeFormatHandle;
                    diagnosticName = GetTypeFullNameFromTypeDef(typeDefinitionHandle, qTypeDefinition.NativeFormatReader);
                    return true;
                }
            }
            return false;
        }

        static partial void TryGetFullNameFromTypeDefEcma(QTypeDefinition qTypeDefinition, ref string result);

        private static string GetTypeFullNameFromTypeRef(TypeReferenceHandle typeReferenceHandle, MetadataReader reader)
        {
            TypeReference typeReference = typeReferenceHandle.GetTypeReference(reader);
            string s = typeReference.TypeName.GetString(reader);
            Handle parentHandle = typeReference.ParentNamespaceOrType;
            HandleType parentHandleType = parentHandle.HandleType;
            if (parentHandleType == HandleType.TypeReference)
            {
                string containingTypeName = GetTypeFullNameFromTypeRef(parentHandle.ToTypeReferenceHandle(reader), reader);
                s = containingTypeName + "+" + s;
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
            return s;
        }

        private static string GetTypeFullNameFromTypeDef(TypeDefinitionHandle typeDefinitionHandle, MetadataReader reader)
        {
            string s;

            TypeDefinition typeDefinition = typeDefinitionHandle.GetTypeDefinition(reader);
            s = typeDefinition.Name.GetString(reader);

            TypeDefinitionHandle enclosingTypeDefHandle = typeDefinition.EnclosingType;
            if (!enclosingTypeDefHandle.IsNull(reader))
            {
                string containingTypeName = GetTypeFullNameFromTypeDef(enclosingTypeDefHandle, reader);
                s = containingTypeName + "+" + s;
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
            return s;
        }
    }
}
