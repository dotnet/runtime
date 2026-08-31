// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.Diagnostics
{
    public struct MethodInfo
    {
        public string AssemblyName;
        public uint MethodToken;
        public uint HotRVA;
        public uint HotLength;
        public string Name;
        public uint ColdRVA;
        public uint ColdLength;
    }
}
