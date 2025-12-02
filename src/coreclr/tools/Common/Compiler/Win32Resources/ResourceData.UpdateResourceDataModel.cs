// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;

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
            ResType resType;

            if (type is ushort)
            {
                _resTypeHeadID.TryGetValue((ushort)type, out resType);
            }
            else
            {
                Debug.Assert(type is string);
                _resTypeHeadName.TryGetValue((string)type, out resType);
            }

            if (resType == null)
                return null;

            ResName resName;

            if (name is ushort)
            {
                resType.NameHeadID.TryGetValue((ushort)name, out resName);
            }
            else
            {
                Debug.Assert(name is string);
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
