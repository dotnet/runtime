// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Type name formatting functionality that relies on metadata.
    public partial class ExceptionTypeNameFormatter
    {
        private static string GetTypeName(DefType type)
        {
            return type.Name;
        }

        private static string GetTypeNamespace(DefType type)
        {
            return type.Namespace;
        }
    }
}
