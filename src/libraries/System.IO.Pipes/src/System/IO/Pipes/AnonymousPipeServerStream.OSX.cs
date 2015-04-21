// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Pipes
{
    public sealed partial class AnonymousPipeServerStream
    {
        private static unsafe void CreatePipe(HandleInheritability inheritability, int* fdsptr)
        {
            // Ignore inheritability, as on OSX pipe2 isn't available
            while (Interop.CheckIo(Interop.libc.pipe(fdsptr))) ;
        }
    }
}
