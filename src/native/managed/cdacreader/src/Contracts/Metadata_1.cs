// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Metadata_1 : IMetadata
{
    private readonly Target _target;

    internal Metadata_1(Target target)
    {
        _target = target;
    }
}
