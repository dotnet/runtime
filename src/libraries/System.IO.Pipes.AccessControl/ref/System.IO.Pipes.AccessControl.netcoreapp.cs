// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Pipes
{
    public static class AnonymousPipeServerStreamAcl
    {
        public static System.IO.Pipes.AnonymousPipeServerStream Create(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize, System.IO.Pipes.PipeSecurity? pipeSecurity) { throw null; }
    }
    public static class NamedPipeServerStreamAcl
    {
        public static System.IO.Pipes.NamedPipeServerStream Create(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options, int inBufferSize, int outBufferSize, System.IO.Pipes.PipeSecurity? pipeSecurity, System.IO.HandleInheritability inheritability = System.IO.HandleInheritability.None, System.IO.Pipes.PipeAccessRights additionalAccessRights = default) { throw null; }
    }
}
