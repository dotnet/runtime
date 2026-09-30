// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.PortableCallHelpers
{
    /// <summary>
    /// Formats type names the way <see cref="System.Reflection"/> reports them. The runtime looks
    /// callbacks up by these names and the emitted symbols embed them, so they have to match
    /// reflection rather than the type system, which spells nested types differently.
    /// </summary>
    internal static class TypeNames
    {
        /// <summary>
        /// The name <see cref="System.Type.FullName"/> would report, with nested types joined by '+'.
        /// </summary>
        public static string GetFullName(MetadataType type)
        {
            if (type.ContainingType is MetadataType containingType)
                return $"{GetFullName(containingType)}+{type.Name.ToString()}";

            string name = type.Name.ToString();
            string ns = GetNamespace(type);

            return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
        }

        /// <summary>
        /// The namespace <see cref="System.Type.Namespace"/> would report. The type system stores it
        /// only on the outermost type, while reflection reports the enclosing namespace for nested
        /// types too.
        /// </summary>
        public static string GetNamespace(TypeDesc type)
        {
            if (type is not MetadataType metadataType)
                return string.Empty;

            while (metadataType.ContainingType is MetadataType containingType)
                metadataType = containingType;

            return metadataType.Namespace.ToString();
        }
    }
}
