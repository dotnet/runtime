// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class ExceptionTypeNameFormatter
    {
        private static string GetTypeName(DefType type)
        {
            if (type is NoMetadata.NoMetadataType)
                return ((NoMetadata.NoMetadataType)type).NameForDiagnostics;

            return type.Name;
        }

        private static string GetTypeNamespace(DefType type)
        {
            if (type is NoMetadata.NoMetadataType)
                return ((NoMetadata.NoMetadataType)type).NamespaceForDiagnostics;

            return type.Namespace;
        }
    }
}
