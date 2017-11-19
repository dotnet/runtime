// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System {

    // These sources are taken from corclr repo (src\mscorlib\src\System\Buffer.cs with x64 path removed)
    // The reason for this duplication is that System.Runtime.dll 4.0.10 did not expose Buffer.MemoryCopy,
    // but we need to make this component work with System.Runtime.dll 4.0.10
    // The methods AreOverlapping and SlowCopyBackwards are not from Buffer.cs. Buffer.cs does an internal CLR call for these.
    static class BufferInternal
    {       
        // This method has different signature for x64 and other platforms and is done for performance reasons.
        private static unsafe void Memmove(byte* dest, byte* src, uint len)
        {
            if (AreOverlapping(dest, src, len))
            {
                SlowCopyBackwards(dest, src, len);
                return;
            }

            // This is portable version of memcpy. It mirrors what the hand optimized assembly versions of memcpy typically do.
            switch (len)
            {
                case 0:
                    return;
                case 1:
                    *dest = *src;
                    return;
                case 2:
                    *(short*)dest = *(short*)src;
                    return;
                case 3:
                    *(short*)dest = *(short*)src;
                    *(dest + 2) = *(src + 2);
                    return;
                case 4:
                    *(int*)dest = *(int*)src;
                    return;
                case 5:
                    *(int*)dest = *(int*)src;
                    *(dest + 4) = *(src + 4);
                    return;
                case 6:
                    *(int*)dest = *(int*)src;
                    *(short*)(dest + 4) = *(short*)(src + 4);
                    return;
                case 7:
                    *(int*)dest = *(int*)src;
                    *(short*)(dest + 4) = *(short*)(src + 4);
                    *(dest + 6) = *(src + 6);
                    return;
                case 8:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    return;
                case 9:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(dest + 8) = *(src + 8);
                    return;
                case 10:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(short*)(dest + 8) = *(short*)(src + 8);
                    return;
                case 11:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(short*)(dest + 8) = *(short*)(src + 8);
                    *(dest + 10) = *(src + 10);
                    return;
                case 12:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    return;
                case 13:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    *(dest + 12) = *(src + 12);
                    return;
                case 14:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    *(short*)(dest + 12) = *(short*)(src + 12);
                    return;
                case 15:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    *(short*)(dest + 12) = *(short*)(src + 12);
                    *(dest + 14) = *(src + 14);
                    return;
                case 16:
                    *(int*)dest = *(int*)src;
                    *(int*)(dest + 4) = *(int*)(src + 4);
                    *(int*)(dest + 8) = *(int*)(src + 8);
                    *(int*)(dest + 12) = *(int*)(src + 12);
                    return;
                default:
                    break;
            }

            if ((unchecked((int)dest) & 3) != 0)
            {
                if (((int)dest & 1) != 0)
                {
                    *dest = *src;
                    src++;
                    dest++;
                    len--;
                    if (((int)dest & 2) == 0)
                        goto Aligned;
                }
                *(short*)dest = *(short*)src;
                src += 2;
                dest += 2;
                len -= 2;
            Aligned:;
            }

            uint count = len / 16;
            while (count > 0)
            {
                ((int*)dest)[0] = ((int*)src)[0];
                ((int*)dest)[1] = ((int*)src)[1];
                ((int*)dest)[2] = ((int*)src)[2];
                ((int*)dest)[3] = ((int*)src)[3];
                dest += 16;
                src += 16;
                count--;
            }

            if ((len & 8) != 0)
            {
                ((int*)dest)[0] = ((int*)src)[0];
                ((int*)dest)[1] = ((int*)src)[1];
                dest += 8;
                src += 8;
            }
            if ((len & 4) != 0)
            {
                ((int*)dest)[0] = ((int*)src)[0];
                dest += 4;
                src += 4;
            }
            if ((len & 2) != 0)
            {
                ((short*)dest)[0] = ((short*)src)[0];
                dest += 2;
                src += 2;
            }
            if ((len & 1) != 0)
                *dest = *src;

            return;
        }

        private static unsafe void SlowCopyBackwards(byte* dest, byte* src, uint len)
        {
            Debug.Assert(len <= int.MaxValue);
            if (len == 0) return;

            for(int i=((int)len)-1; i>=0; i--)
            {
                dest[i] = src[i];
            }
        }

        private static unsafe bool AreOverlapping(byte* dest, byte* src, uint len)
        {
            byte* srcEnd = src + len;
            byte* destEnd = dest + len;
            if (srcEnd >= dest && srcEnd <= destEnd)
            {
                return true;
            }
            return false;
        }

        // The attributes on this method are chosen for best JIT performance. 
        // Please do not edit unless intentional.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MemoryCopy(void* source, void* destination, int destinationSizeInBytes, int sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceBytesToCopy));
            }

            Memmove((byte*)destination, (byte*)source, checked((uint)sourceBytesToCopy));
        }
    }
}
