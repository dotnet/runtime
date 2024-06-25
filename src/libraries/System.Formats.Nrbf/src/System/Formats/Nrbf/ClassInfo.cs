// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

/// <summary>
/// Class info that provides type and member names.
/// </summary>
/// <remarks>
/// ClassInfo structures are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf">[MS-NRBF] 2.3.1.1</see>.
/// </remarks>
[DebuggerDisplay("{TypeName}")]
internal sealed class ClassInfo
{
    private readonly string _rawName;
    private TypeName? _typeName;

    private ClassInfo(SerializationRecordId id, string rawName, Dictionary<string, int> memberNames)
    {
        Id = id;
        _rawName = rawName;
        MemberNames = memberNames;
    }

    internal SerializationRecordId Id { get; }

    internal TypeName TypeName
    {
        get
        {
            Debug.Assert(_typeName is not null);
            return _typeName;
        }
    }

    internal Dictionary<string, int> MemberNames { get; }

    internal static ClassInfo Decode(BinaryReader reader)
    {
        SerializationRecordId id = SerializationRecordId.Decode(reader);
        string typeName = reader.ReadString();
        int memberCount = reader.ReadInt32();

        // Use Dictionary instead of List so that searching for member IDs by name
        // is O(n) instead of O(m * n), where m = memberCount and n = memberNameLength,
        // in degenerate cases.
        Dictionary<string, int> memberNames = new(StringComparer.Ordinal);
        for (int i = 0; i < memberCount; i++)
        {
            // The NRBF specification does not prohibit multiple members with the same names,
            // however it's impossible to get such output with BinaryFormatter,
            // so we prohibit that on purpose.
            string memberName = reader.ReadString();
#if NET
            if (memberNames.TryAdd(memberName, i))
            {
                continue;
            }
#else
            if (!memberNames.ContainsKey(memberName))
            {
                memberNames.Add(memberName, i);
                continue;
            }
#endif
            throw new SerializationException(SR.Format(SR.Serialization_DuplicateMemberName, memberName));
        }

        return new ClassInfo(id, typeName, memberNames);
    }

    internal void LoadTypeName(BinaryLibraryRecord libraryRecord, PayloadOptions payloadOptions)
        => _typeName = _rawName.ParseNonSystemClassRecordTypeName(libraryRecord, payloadOptions);

    internal void LoadTypeName(PayloadOptions payloadOptions)
        => _typeName = _rawName.ParseSystemRecordTypeName(payloadOptions);
}
