// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private readonly List<ResType_Ordinal> _resTypeHeadID = new List<ResType_Ordinal>();
        private readonly List<ResType_Name> _resTypeHeadName = new List<ResType_Name>();

        private class OrdinalName
        {
            public OrdinalName(ushort ordinal) { Ordinal = ordinal; }
            public readonly ushort Ordinal;
        }

        private interface IUnderlyingName<T>
        {
            T Name { get; }
        }

        private class ResName
        {
            public uint DataSize => (uint)DataEntry.Length;
            public byte[] DataEntry;
            public ushort NumberOfLanguages;
            public ushort LanguageId;
        }

        private class ResName_Name : ResName, IUnderlyingName<string>
        {
            public ResName_Name(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private class ResName_Ordinal : ResName, IUnderlyingName<ushort>
        {
            public ResName_Ordinal(ushort name)
            {
                Name = new OrdinalName(name);
            }

            public OrdinalName Name;
            ushort IUnderlyingName<ushort>.Name => Name.Ordinal;
        }

        private class ResType
        {
            public List<ResName_Name> NameHeadName = new List<ResName_Name>();
            public List<ResName_Ordinal> NameHeadID = new List<ResName_Ordinal>();
        }

        private class ResType_Ordinal : ResType, IUnderlyingName<ushort>
        {
            public ResType_Ordinal(ushort type)
            {
                Type = new OrdinalName(type);
            }

            public OrdinalName Type;
            ushort IUnderlyingName<ushort>.Name => Type.Ordinal;
        }

        private class ResType_Name : ResType, IUnderlyingName<string>
        {
            public ResType_Name(string type)
            {
                Type = type;
            }

            public string Type { get; set; }
            string IUnderlyingName<string>.Name => Type;
        }
    }
}
