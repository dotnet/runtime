// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.Schema
{
    internal static class SchemaHelper
    {
        internal static string GetCollectionNamespace(string elementNs)
        {
            return IsBuiltInNamespace(elementNs) ? Globals.CollectionsNamespace : elementNs;
        }

        internal static string GetDataContractNamespaceFromUri(string uriString)
        {
            return uriString.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal) ? uriString.Substring(Globals.DataContractXsdBaseNamespace.Length) : uriString;
        }

        internal static string GetDefaultStableNamespace(Type type)
        {
            if (type.IsGenericParameter)
                return "{ns}";
            return GetDefaultStableNamespace(type.Namespace);
        }

        internal static string GetDefaultStableNamespace(string? clrNs)
        {
            if (clrNs == null) clrNs = string.Empty;
            return new Uri(Globals.DataContractXsdBaseNamespaceUri, clrNs).AbsoluteUri;
        }

        internal static bool IsBuiltInNamespace(string ns)
        {
            return (ns == Globals.SchemaNamespace || ns == Globals.SerializationNamespace);
        }
    }
}
