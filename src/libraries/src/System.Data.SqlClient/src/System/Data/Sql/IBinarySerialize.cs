// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.Server
{
    // This interface is used by types that want full control over the
    // binary serialization format.
    public interface IBinarySerialize
    {
        // Read from the specified binary reader.
        void Read(BinaryReader r);
        // Write to the specified binary writer.
        void Write(BinaryWriter w);
    }
}