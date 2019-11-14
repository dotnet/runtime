// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ILCompiler.DependencyAnalysis.X86
{
    public enum AddrModeSize
    {
        Int8 = 1,
        Int16 = 2,
        Int32 = 4
    }

    public struct AddrMode
    {
        public readonly Register BaseReg;
        public readonly Register? IndexReg;
        public readonly int Offset;
        public readonly byte Scale;
        public readonly AddrModeSize Size;

        public AddrMode(Register baseRegister, Register? indexRegister, int offset, byte scale, AddrModeSize size)
        {
            BaseReg = baseRegister;
            IndexReg = indexRegister;
            Offset = offset;
            Scale = scale;
            Size = size;
        }
    }
}
