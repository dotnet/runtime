// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient
{
    internal sealed partial class TdsParser
    {
        internal static void FillGuidBytes(Guid guid, Span<byte> buffer) => guid.TryWriteBytes(buffer);

        internal static void FillDoubleBytes(double value, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, value);

        internal static void FillFloatBytes(float v, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, v);
    }
}
