// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    partial class ExceptionTypeNameFormatter
    {
        private string GetTypeName(DefType type)
        {
            if (type is NoMetadata.NoMetadataType)
                return ((NoMetadata.NoMetadataType)type).NameForDiagnostics;

            return type.Name;
        }

        private string GetTypeNamespace(DefType type)
        {
            if (type is NoMetadata.NoMetadataType)
                return ((NoMetadata.NoMetadataType)type).NamespaceForDiagnostics;

            return type.Namespace;
        }
    }
}
