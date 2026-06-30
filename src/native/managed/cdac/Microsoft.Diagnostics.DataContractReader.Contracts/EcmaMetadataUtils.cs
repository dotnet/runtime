// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader;

public static class EcmaMetadataUtils
{
    internal const int RowIdBitCount = 24;
    internal const uint RIDMask = (1 << RowIdBitCount) - 1;

    public static uint GetRowId(uint token) => token & RIDMask;

    internal static uint MakeToken(uint rid, uint table) => rid | (table << RowIdBitCount);

    // ECMA-335 II.22
    // Metadata table index is the most significant byte of the 4-byte token
    public enum TokenType : uint
    {
        mdtTypeRef = 0x01 << 24,
        mdtTypeDef = 0x02 << 24,
        mdtFieldDef = 0x04 << 24,
        mdtMethodDef = 0x06 << 24,
        mdtSignature = 0x11 << 24,
        mdtAssemblyRef = 0x23 << 24,
    }

    public const uint TokenTypeMask = 0xff000000;

    public static uint CreateMethodDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtMethodDef | tokenParts;
    }

    public static uint CreateFieldDef(uint tokenParts)
    {
        Debug.Assert((tokenParts & 0xff000000) == 0, $"Token type should not be set in {nameof(tokenParts)}");
        return (uint)TokenType.mdtFieldDef | tokenParts;
    }

    public static bool TryFindTopLevelTypeDef(MetadataReader reader, string @namespace, string name, out TypeDefinitionHandle result)
    {
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            if (!typeDef.GetDeclaringType().IsNil)
                continue;
            if (reader.StringComparer.Equals(typeDef.Name, name) && reader.StringComparer.Equals(typeDef.Namespace, @namespace))
            {
                result = handle;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static bool TryFindNestedTypeDef(MetadataReader reader, TypeDefinitionHandle declaringType, string name, out TypeDefinitionHandle result)
    {
        TypeDefinition declaring = reader.GetTypeDefinition(declaringType);
        foreach (TypeDefinitionHandle handle in declaring.GetNestedTypes())
        {
            TypeDefinition nested = reader.GetTypeDefinition(handle);
            if (reader.StringComparer.Equals(nested.Name, name))
            {
                result = handle;
                return true;
            }
        }

        result = default;
        return false;
    }
}
