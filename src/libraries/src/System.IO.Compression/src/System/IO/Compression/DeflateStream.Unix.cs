// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    public partial class DeflateStream : Stream
    {
        private static WorkerType GetDeflaterType()
        {
            return WorkerType.Managed; // TODO: Switch to ZLib once interop is worked out
        }
    }
}
