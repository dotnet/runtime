// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Internal.Runtime.Binder
{
    internal enum AssemblyIdentityFlags
    {
        IDENTITY_FLAG_EMPTY = 0x000,
        IDENTITY_FLAG_SIMPLE_NAME = 0x001,
        IDENTITY_FLAG_VERSION = 0x002,
        IDENTITY_FLAG_PUBLIC_KEY_TOKEN = 0x004,
        IDENTITY_FLAG_PUBLIC_KEY = 0x008,
        IDENTITY_FLAG_CULTURE = 0x010,
        IDENTITY_FLAG_PROCESSOR_ARCHITECTURE = 0x040,
        IDENTITY_FLAG_RETARGETABLE = 0x080,
        IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL = 0x100,
        IDENTITY_FLAG_CONTENT_TYPE = 0x800,
        IDENTITY_FLAG_FULL_NAME = IDENTITY_FLAG_SIMPLE_NAME | IDENTITY_FLAG_VERSION
    }

    internal class AssemblyIdentity
    {
        public string SimpleName = string.Empty;
        public AssemblyVersion Version = new AssemblyVersion();
        public string CultureOrLanguage = string.Empty;
        public byte[] PublicKeyOrTokenBLOB = Array.Empty<byte>();
        public ProcessorArchitecture ProcessorArchitecture;
        public AssemblyContentType ContentType;
        public AssemblyIdentityFlags IdentityFlags;
    }
}
