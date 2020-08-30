// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.TypeLoading.Ecma
{
    internal unsafe struct InternalManifestResourceInfo
    {
        public bool Found;
        public string FileName;
        public Assembly ReferencedAssembly;
        public byte* PointerToResource;
        public uint SizeOfResource;
        public ResourceLocation ResourceLocation;
    }
}
