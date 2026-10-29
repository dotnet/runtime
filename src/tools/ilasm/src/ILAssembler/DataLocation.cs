// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler;

internal enum DataSection : byte
{
    Data,
    Tls,
    Cil,
}

internal readonly record struct DataLocation(DataSection Section, int Offset);
