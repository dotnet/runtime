// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime.Binder
{
    // System.Reflection.TypeLoading.AssemblyNameData
    internal unsafe readonly struct AssemblyNameData
    {
        public readonly void* Name;
        public readonly void* Culture;

        public readonly byte* PublicKeyOrToken;
        public readonly int PublicKeyOrTokenLength;

        public readonly int MajorVersion;
        public readonly int MinorVersion;
        public readonly int BuildNumber;
        public readonly int RevisionNumber;

        public readonly PEKind ProcessorArchitecture;
        public readonly System.Reflection.AssemblyContentType ContentType;

        public readonly AssemblyIdentityFlags IdentityFlags;
    }

    internal abstract class AssemblyBinder
    {
        public abstract ApplicationContext AppContext { get; }
    }
}
