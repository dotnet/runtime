// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Pipes
{
    public sealed partial class AnonymousPipeServerStream
    {
        private static unsafe void CreatePipe(HandleInheritability inheritability, int* fdsptr)
        {
            int flags = (inheritability & HandleInheritability.Inheritable) == 0 ?
                (int)Interop.libc.OpenFlags.O_CLOEXEC : 0;
            while (Interop.CheckIo(Interop.libc.pipe2(fdsptr, flags))) ;
        }

    }
}
