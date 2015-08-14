// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public partial class SafePipeHandle : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get { return default(bool); } }
    }
}
namespace System.IO.Pipes
{
    // It needs to be defined manually because we cannot rely on Stream's implementation due to lack of APM virtual methods
    public partial class PipeStream
    {
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
}

