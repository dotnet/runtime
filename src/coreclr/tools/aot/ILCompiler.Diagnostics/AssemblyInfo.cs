// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.Diagnostics
{
    public struct AssemblyInfo
    {
        public readonly string Name;
        public readonly Guid Mvid;

        public AssemblyInfo(string name, Guid mvid)
        {
            Name = name;
            Mvid = mvid;
        }
    }
}
