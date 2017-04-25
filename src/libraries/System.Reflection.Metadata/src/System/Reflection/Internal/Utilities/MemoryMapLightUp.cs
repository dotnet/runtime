// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class MemoryMapLightUp
    {
        internal static bool IsAvailable => true;

        internal static IDisposable CreateMemoryMap(Stream stream)
        {
            return MemoryMappedFile.CreateFromFile(
                (FileStream)stream, 
                mapName: null,
                capacity: 0, 
                access: MemoryMappedFileAccess.Read, 
                inheritability: HandleInheritability.None, 
                leaveOpen: true);
        }

        internal static IDisposable CreateViewAccessor(object memoryMap, long start, int size)
        {
            try
            {
                return ((MemoryMappedFile)memoryMap).CreateViewAccessor(start, size, MemoryMappedFileAccess.Read);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new IOException(e.Message, e);
            }
        }

        internal static bool TryGetSafeBufferAndPointerOffset(object accessor, out SafeBuffer safeBuffer, out long offset)
        {
            var viewAccessor = (MemoryMappedViewAccessor)accessor;
            safeBuffer = viewAccessor.SafeMemoryMappedViewHandle;
            offset = viewAccessor.PointerOffset;
            return true;
        }
    }
}
