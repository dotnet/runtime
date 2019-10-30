// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Net.Mime
{
    internal interface IEncodableStream
    {
        int DecodeBytes(byte[] buffer, int offset, int count);
        int EncodeBytes(byte[] buffer, int offset, int count);
        string GetEncodedString();
    }
}
