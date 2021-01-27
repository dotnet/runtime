// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private void AddResource(object type, object name, ushort language, byte[] data)
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
            if (type is string)
            {
                _resTypeHeadName.TryGetValue((string)type, out resType);
            }

            if (resType == null)
                return null;

            ResName resName = null;

            if (name is ushort)
            {
                resType.NameHeadID.TryGetValue((ushort)name, out resName);
            }
            if (name is string)
            {
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
