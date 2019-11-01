// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    /// <summary>
    /// Process - Process input. Encoder may postpone producing output, until it has processed enough input.
    /// Flush - Produce output for all processed input.  Actual flush is performed when input stream is depleted and there is enough space in output stream.
    /// Finish - Finalize the stream. Adding more input data to finalized stream is impossible.
    /// EmitMetadata - Emit metadata block to stream. Stream is soft-flushed before metadata block is emitted. Metadata bloc MUST be no longer than 16MiB.
    /// </summary>
    internal enum BrotliEncoderOperation
    {
        Process,
        Flush,
        Finish,
        EmitMetadata
    }
}
