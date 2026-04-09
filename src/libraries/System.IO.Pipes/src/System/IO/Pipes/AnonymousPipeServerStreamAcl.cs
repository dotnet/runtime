// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipes
{
    public static class AnonymousPipeServerStreamAcl
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AnonymousPipeServerStream" /> class with the specified pipe direction, inheritability mode, buffer size, and pipe security.
        /// </summary>
        /// <param name="direction">One of the enumeration values that determines the direction of the pipe. Anonymous pipes can only be in one direction, so direction cannot be set to <see cref="PipeDirection.InOut" />.</param>
        /// <param name="inheritability">One of the enumeration values that determines whether the underlying handle can be inherited by child processes.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than or equal to 0.</param>
        /// <param name="pipeSecurity">An object that determines the access control and audit security for the pipe.</param>
        /// <returns>A new anonymous pipe server stream instance.</returns>
        /// <remarks>Setting <paramref name="pipeSecurity" /> to <see langword="null" /> is equivalent to calling the `System.IO.Pipes.AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize)` constructor directly.</remarks>
        /// <exception cref="NotSupportedException"><paramref name="direction" /> is <see cref="PipeDirection.InOut" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inheritability" /> is not set to a valid <see cref="HandleInheritability" /> enum value.</exception>
        public static AnonymousPipeServerStream Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize, PipeSecurity? pipeSecurity)
        {
            return new AnonymousPipeServerStream(direction, inheritability, bufferSize, pipeSecurity);
        }
    }
}
