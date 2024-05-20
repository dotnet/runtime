// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class info.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf">
///    [MS-NRBF] 2.3.1.1
///   </see>
///  </para>
/// </remarks>
[DebuggerDisplay("{Name}")]
internal sealed class ClassInfo
{
    private ClassInfo(int objectId, TypeName name, Dictionary<string, int> memberNames)
    {
        ObjectId = objectId;
        Name = name;
        MemberNames = memberNames;
    }

    internal int ObjectId { get; }

    internal TypeName Name { get; }

    internal Dictionary<string, int> MemberNames { get; }

    internal static ClassInfo Parse(BinaryReader reader, PayloadOptions payloadOptions)
    {
        int objectId = reader.ReadInt32();
        TypeName typeName = reader.ReadTypeName(payloadOptions);
        int memberCount = reader.ReadInt32();

        // The attackers could create an input with MANY member names.
        // If we were storing them in a list, then searching for the index
        // of given member name (done by ClassRecord indexer) would take
        // O(m * n), where m = memberCount and n = memberNameLength.
        // To prevent this from happening, we are using a Dictionary instead,
        // which has O(1) lookup time.
        Dictionary<string, int> memberNames = new(StringComparer.Ordinal);
        for (int i = 0; i < memberCount; i++)
        {
            // The NRBF specification does not prohibit multiple members with the same names,
            // however it's impossible to get such output with BinaryFormatter,
            // so we prohibit that on purpose (Add is going to throw on duplicates).
            memberNames.Add(reader.ReadString(), i);
        }

        return new(objectId, typeName, memberNames);
    }
}
