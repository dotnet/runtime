// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private readonly SortedDictionary<ushort, ResType> _resTypeHeadID = new SortedDictionary<ushort, ResType>();
        private readonly SortedDictionary<string, ResType> _resTypeHeadName = new SortedDictionary<string, ResType>(StringComparer.Ordinal);

        private class ResLanguage
        {
            public ResLanguage(byte[] data)
            {
                DataEntry = data;
            }

            public uint DataSize => (uint)DataEntry.Length;
            public byte[] DataEntry;
        }

        private class ResName
        {
            public SortedDictionary<ushort, ResLanguage> Languages = new SortedDictionary<ushort, ResLanguage>();
        }

        private class ResType
        {
            public SortedDictionary<string, ResName> NameHeadName = new SortedDictionary<string, ResName>(StringComparer.Ordinal);
            public SortedDictionary<ushort, ResName> NameHeadID = new SortedDictionary<ushort, ResName>();
        }

    }
}
