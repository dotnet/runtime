// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    internal interface IDeflater : IDisposable
    {
        [Pure]
        bool NeedsInput();
        void SetInput(byte[] inputBuffer, int startIndex, int count);
        int GetDeflateOutput(byte[] outputBuffer);
        bool Finish(byte[] outputBuffer, out int bytesRead);
    }
}
