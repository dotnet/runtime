// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#if HOST_MODEL
namespace Microsoft.NET.HostModel.Win32Resources
#else
namespace ILCompiler.Win32Resources
#endif
{
    public unsafe partial class ResourceData
    {
        private readonly SortedDictionary<ushort, ResType> _resTypeHeadID = new SortedDictionary<ushort, ResType>();
        private readonly SortedDictionary<string, ResType> _resTypeHeadName = new SortedDictionary<string, ResType>(StringComparer.Ordinal);

        private sealed class ResLanguage
        {
            public ResLanguage(byte[] data)
            {
                DataEntry = data;
            }

            public uint DataSize => (uint)DataEntry.Length;
            public byte[] DataEntry;
        }

        private sealed class ResName
        {
            public SortedDictionary<ushort, ResLanguage> Languages = new SortedDictionary<ushort, ResLanguage>();
        }

        private sealed class ResType
        {
            public SortedDictionary<string, ResName> NameHeadName = new SortedDictionary<string, ResName>(StringComparer.Ordinal);
            public SortedDictionary<ushort, ResName> NameHeadID = new SortedDictionary<ushort, ResName>();
        }

    }
}
