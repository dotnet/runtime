// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text
{
    public partial class StringBuilder
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern unsafe void ReplaceBufferInternal(char* newBuffer, int newLength);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern unsafe void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength);

        /// <summary>
        /// Copies the contents of this builder to the specified buffer.
        /// </summary>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="len">The number of bytes in the destination buffer.</param>
        internal unsafe void InternalCopy(IntPtr dest, int len)
        {
            if (len == 0)
            {
                return;
            }

            bool isLastChunk = true;
            byte* dstPtr = (byte*)dest.ToPointer();
            StringBuilder currentSrc = FindChunkForByte(len);

            do
            {
                int chunkOffsetInBytes = currentSrc.m_ChunkOffset * sizeof(char);
                int chunkLengthInBytes = currentSrc.m_ChunkLength * sizeof(char);
                fixed (char* charPtr = &currentSrc.m_ChunkChars[0])
                {
                    byte* srcPtr = (byte*)charPtr;
                    if (isLastChunk)
                    {
                        isLastChunk = false;
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, len - chunkOffsetInBytes);
                    }
                    else
                    {
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, chunkLengthInBytes);
                    }
                }
                currentSrc = currentSrc.m_ChunkPrevious;
            }
            while (currentSrc != null);
        }
    }
}
