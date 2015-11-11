// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO.Compression
{
    public partial class DeflateStream : Stream
    {
        private static WorkerType GetDeflaterType()
        {
            try
            {
                if (Interop.zlib.IsZLibAvailable())
                {
                    return WorkerType.ZLib;
                }
            }
            catch
            {
                // Otherwise, fallback to managed implementation if zlib isn't available
                Debug.Fail("zlib unavailable");
            }

            return WorkerType.Managed;
        }
    }
}
