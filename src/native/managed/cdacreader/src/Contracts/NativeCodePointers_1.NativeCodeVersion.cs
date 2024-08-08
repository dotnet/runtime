// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

using MapUnit = uint;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


#pragma warning disable SA1121 // Use built in alias
internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{
    internal struct NativeCodeVersionContract
    {
        private readonly Target _target;

        public NativeCodeVersionContract(Target target)
        {
            _target = target;
        }
    }
}
