// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    // Interface for Binary Records.
    internal interface IStreamable
    {
        void Write(BinaryFormatterWriter output);
        void Read(BinaryParser input);
    }
}
