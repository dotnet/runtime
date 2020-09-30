// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipes
{
    public static class NamedPipeServerStreamAcl
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NamedPipeServerStream" /> class with the specified pipe name, pipe direction, maximum number of server instances, transmission mode, pipe options, recommended in and out buffer sizes, pipe security, inheritability mode, and pipe access rights.
        /// </summary>
        /// <param name="pipeName">The name of the pipe.</param>
        /// <param name="direction">One of the enumeration values that determines the direction of the pipe.</param>
        /// <param name="maxNumberOfServerInstances">The maximum number of server instances that share the same name. You can pass <see cref="NamedPipeServerStream.MaxAllowedServerInstances" /> for this value.</param>
        /// <param name="transmissionMode">One of the enumeration values that determines the transmission mode of the pipe.</param>
        /// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
        /// <param name="inBufferSize">The input buffer size.</param>
        /// <param name="outBufferSize">The output buffer size.</param>
        /// <param name="pipeSecurity">An object that determines the access control and audit security for the pipe.</param>
        /// <param name="inheritability">One of the enumeration values that determines whether the underlying handle can be inherited by child processes.</param>
        /// <param name="additionalAccessRights">One of the enumeration values that specifies the access rights of the pipe.</param>
        /// <returns>A new named pipe server stream instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pipeName" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="pipeName" /> is empty.</exception>
        /// <exception cref="IOException"><paramref name="options" /> is <see cref="PipeOptions.None" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options" /> contains an invalid flag.
        /// -or-
        /// <paramref name="inBufferSize" /> is a negative number.
        /// -or
        /// <paramref name="maxNumberOfServerInstances" /> is not a valid number: it should be greater or equal than 1 and less or equal than 254, or should be set to the value of <see cref="NamedPipeServerStream.MaxAllowedServerInstances" />.
        /// -or-
        /// <paramref name="inheritability" /> contains an invalid enum value.
        /// -or
        /// <paramref name="pipeName" /> is 'anonymous', which is reserved.</exception>
        /// <remarks>If `options` contains &lt;xref:System.IO.Pipes.PipeOptions.CurrentUserOnly&gt;, the passed `pipeSecurity` is ignored and the returned &lt;xref:System.IO.Pipes.NamedPipeServerStream&gt; object is created using a custom &lt;xref:System.IO.Pipes.PipeSecurity&gt; instance assigned to the current Windows user as its only owner with full control of the pipe.</remarks>
        public static NamedPipeServerStream Create(
            string pipeName,
            PipeDirection direction,
            int maxNumberOfServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions options,
            int inBufferSize,
            int outBufferSize,
            PipeSecurity? pipeSecurity,
            HandleInheritability inheritability = HandleInheritability.None,
            PipeAccessRights additionalAccessRights = default)
        {
            return new NamedPipeServerStream(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, pipeSecurity, inheritability, additionalAccessRights);
        }
    }
}
