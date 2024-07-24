// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

#if HOST_MODEL
namespace Microsoft.NET.HostModel.Win32Resources
#else
namespace ILCompiler.Win32Resources
#endif
{
    public unsafe partial class ResourceData
    {
        private void AddResourceInternal(object name, object type, ushort language, byte[] data)
        {
            ResType resType;

            if (type is ushort)
            {
                if (!_resTypeHeadID.TryGetValue((ushort)type, out resType))
                {
                    resType = new ResType();
                    _resTypeHeadID[(ushort)type] = resType;
                }
            }
            else
            {
                Debug.Assert(type is string);
                // Undocumented semantic for Win32 Resource APIs
                type = ((string)type).ToUpperInvariant();
                if (!_resTypeHeadName.TryGetValue((string)type, out resType))
                {
                    resType = new ResType();
                    _resTypeHeadName[(string)type] = resType;
                }
            }

            ResName resName;

            if (name is ushort)
            {
                if (!resType.NameHeadID.TryGetValue((ushort)name, out resName))
                {
                    resName = new ResName();
                    resType.NameHeadID[(ushort)name] = resName;
                }
            }
            else
            {
                Debug.Assert(name is string);
                // Undocumented semantic for Win32 Resource APIs
                name = ((string)name).ToUpperInvariant();
                if (!resType.NameHeadName.TryGetValue((string)name, out resName))
                {
                    resName = new ResName();
                    resType.NameHeadName[(string)name] = resName;
                }
            }

            resName.Languages[language] = new ResLanguage(data);
        }

        private byte[] FindResourceInternal(object name, object type, ushort language)
        {
            ResType resType = null;

            if (type is ushort)
            {
                _resTypeHeadID.TryGetValue((ushort)type, out resType);
            }
            else
            {
                Debug.Assert(type is string);
                // Undocumented semantic for Win32 Resource APIs
                type = ((string)type).ToUpperInvariant();
                _resTypeHeadName.TryGetValue((string)type, out resType);
            }

            if (resType == null)
                return null;

            ResName resName = null;

            if (name is ushort)
            {
                resType.NameHeadID.TryGetValue((ushort)name, out resName);
            }
            else
            {
                Debug.Assert(name is string);
                // Undocumented semantic for Win32 Resource APIs
                name = ((string)name).ToUpperInvariant();
                resType.NameHeadName.TryGetValue((string)name, out resName);
            }

            if (resName == null)
                return null;

            if (!resName.Languages.TryGetValue(language, out ResLanguage resLanguage))
                return null;

            return (byte[])resLanguage.DataEntry.Clone();
        }
    }
}
