// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Debug = System.Diagnostics.Debug;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace ILCompiler.Metadata
{
    // Support for stripping the names of types from the metadata (see IMetadataPolicy.GeneratesNames).
    //
    // Names that are stripped are replaced with the empty string, which the metadata reader users treat as
    // "name not available" (ECMA-335 doesn't allow empty names for types).
    // Records that only differ by their name would no longer be distinguishable by the writer, so the records get
    // a discriminator that is derived from the metadata token of the type.
    //
    // Members are not affected: a member only gets metadata if the compiler found that it is a reflection target,
    // so its name is kept to keep lookups by name working.
    internal partial class Transform<TPolicy>
    {
        private ConstantStringValue HandleName(string name, bool stripName)
            => HandleString(stripName ? string.Empty : name);

        // Types from modules that strip names are all placed into the root namespace.
        private NamespaceDefinition HandleNamespaceDefinition(Cts.MetadataType type)
            => HandleNamespaceDefinition(type.Module, StripsName(type) ? string.Empty : type.GetNamespace());

        private NamespaceReference HandleNamespaceReference(Cts.MetadataType type)
            => HandleNamespaceReference(type.Module, StripsName(type) ? string.Empty : type.GetNamespace());

        private bool StripsName(Cts.MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);
            return !_policy.GeneratesNames(type.Module);
        }

        // The discriminator only needs to be unique within the types that can be compared by the writer, and be
        // stable across compilations, so types are distinguished by their row number in the module.
        private static int GetDiscriminator(Cts.MetadataType type)
            => type is Cts.Ecma.EcmaType ecmaType ? MetadataTokens.GetRowNumber(ecmaType.Handle) : 0;
    }
}
