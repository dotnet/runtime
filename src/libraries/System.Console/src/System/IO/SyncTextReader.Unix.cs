// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.IO
{
    /* SyncTextReader intentionally locks on itself rather than a private lock object.
     * This is done to synchronize different console readers(Issue#2855).
     */
    internal sealed partial class SyncTextReader : TextReader
    {
        public ConsoleKeyInfo ReadKey()
        {
            lock (this)
            {
                Debug.Assert(_in is StdInStreamReader);

                return ((StdInStreamReader)_in).ReadKey();
            }
        }
    }
}
