// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class ElfUtils
    {
        // The Linux Headers are copied from elf.h

#pragma warning disable 0649
        struct ElfHeader
        {
            byte EI_MAG0;
            byte EI_MAG1;
            byte EI_MAG2;
            byte EI_MAG3;

            public bool IsValid()
            {
                return EI_MAG0 == 0x7f &&
                       EI_MAG1 == 0x45 &&
                       EI_MAG2 == 0x4C &&
                       EI_MAG3 == 0x46;
            }
        }
#pragma warning restore 0649

        unsafe public static bool IsElfImage(string filePath)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, mapName: null, capacity: 0, MemoryMappedFileAccess.Read))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    byte* file = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref file);
                        ElfHeader* header = (ElfHeader*)file;

                        return header->IsValid();
                    }
                    finally
                    {
                        if (file != null)
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }
    }
}

