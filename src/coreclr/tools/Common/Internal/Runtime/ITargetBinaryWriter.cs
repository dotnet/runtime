// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    /// <summary>
    /// Abstraction to emit byte streams of data in a format expected by the target platform.
    /// </summary>
    public interface ITargetBinaryWriter
    {
        /// <summary>
        /// Gets the number of bytes written.
        /// </summary>
        int CountBytes { get; }

        /// <summary>
        /// Gets the size, in bytes, of a pointer on the target architecture.
        /// </summary>
        int TargetPointerSize { get; }

        /// <summary>
        /// Emits an integer that has a natural size on the target platform (e.g. 64 bits on 64 bit platforms).
        /// </summary>
        void EmitNaturalInt(int emit);

        /// <summary>
        /// Emits an integer that has half of the natural size on the target platform (e.g. 32 bits on 64 bit platforms).
        /// </summary>
        void EmitHalfNaturalInt(short emit);
    }
}
