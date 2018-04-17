// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text
{
    public partial class StringBuilder
    {
        private int GetReplaceBufferCapacity(int requiredCapacity)
        {
            // This function assumes that required capacity will be less 
            // than the max capacity of the StringBuilder
            Diagnostics.Debug.Assert(requiredCapacity <= m_MaxCapacity);

            int newCapacity = Capacity;
            // Round the current capacity to the nearest multiple of 2
            // that is larger than the required capacity.
            if (newCapacity < requiredCapacity)
            {
                newCapacity = (requiredCapacity + 1) & ~1;
            }
            return newCapacity;
        }

        internal unsafe void ReplaceBufferInternal(char* newBuffer, int newLength)
        {
            if (newLength > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);

            // Leave space for a NUL character at the end, the previous implementation did this
            m_ChunkChars = new char[GetReplaceBufferCapacity(newLength + 1)];
            new Span<char>(newBuffer, newLength).CopyTo(m_ChunkChars);
            m_ChunkLength = newLength;
            m_ChunkPrevious = null;
            m_ChunkOffset = 0;
        }

        internal unsafe void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength)
        {
            if (newLength > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);

            // Leave space for a NUL character at the end, the previous implementation did this
            m_ChunkChars = new char[GetReplaceBufferCapacity(newLength + 1)];

            int convertedChars;

            fixed (char* pChunkChars = m_ChunkChars)
            {
                Diagnostics.Debug.Assert(newLength < m_ChunkChars.Length);

                // The incoming string buffer is supposed to have been populated by the 
                // P/Invoke-called native function but there's no way to know if that really
                // happened, the native function might populate the buffer only in certain
                // circumstances (e.g. only if the function succeeds).
                //
                // As such, the buffer might contain bogus characters that cannot be converted 
                // to Unicode and in that case conversion should not result in exceptions that
                // the managed caller does not expect. Instead, the caller is expected to know
                // when the resulting string is not valid and not use it.
                //
                // Both MultiByteToWideChar and the UTF8Encoding instance used on Unix-like
                // platforms default to replacing invalid characters with the Unicode replacement
                // character U+FFFD.
#if PLATFORM_UNIX
                convertedChars = Encoding.UTF8.GetChars((byte*)newBuffer, newLength, pChunkChars, newLength);
#else
                convertedChars = Interop.Kernel32.MultiByteToWideChar(
                    Interop.Kernel32.CP_ACP,
                    Interop.Kernel32.MB_PRECOMPOSED,
                    (byte*)newBuffer,
                    newLength,
                    pChunkChars,
                    newLength);
#endif
            }

            m_ChunkOffset = 0;
            m_ChunkLength = convertedChars;
            m_ChunkPrevious = null;
        }

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
