// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Text;

namespace ILDisassembler;

/// <summary>
/// Shows the value blob of a custom attribute.
/// </summary>
internal interface ICustomAttributeDisassembler
{
    // Appends " = " and the value of the custom attribute with the given constructor to line. A value that takes
    // several lines prints all but the last one.
    void AppendValue(Dasm dasm, StringBuilder line, EntityHandle constructor, byte[] blob);
}

/// <summary>
/// Shows the blob of a declarative security permission set.
/// </summary>
internal interface IPermissionSetDisassembler
{
    // Appends the permission set to line, which holds the indentation of its first line. A permission set that takes
    // several lines prints all but the last one.
    void AppendPermissionSet(Dasm dasm, StringBuilder line, BlobHandle permissionSet);
}

// Shows the blobs as bytes.
internal sealed class ByteArrayCustomAttributeDisassembler : ICustomAttributeDisassembler, IPermissionSetDisassembler
{
    public static ByteArrayCustomAttributeDisassembler Instance { get; } = new();

    public void AppendValue(Dasm dasm, StringBuilder line, EntityHandle constructor, byte[] blob)
    {
        line.Append(" = ( ");
        dasm.DumpByteArray(line, blob);
    }

    public void AppendPermissionSet(Dasm dasm, StringBuilder line, BlobHandle permissionSet)
    {
        line.Append(dasm.KEYWORD("bytearray")).Append(" (");
        dasm.DumpByteArray(line, dasm.Blob(permissionSet));
    }
}
