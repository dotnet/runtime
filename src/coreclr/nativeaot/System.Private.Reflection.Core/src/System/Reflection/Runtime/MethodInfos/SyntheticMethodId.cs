// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal enum SyntheticMethodId
    {
        ArrayCtor = 1,
        ArrayMultiDimCtor = 2,
        ArrayGet = 3,
        ArraySet = 4,
        ArrayAddress = 5,

        // Ids from 0x80000000..0xffffffff are reserved for the jagged array constructors
        // (e.g. a type such as T[][][][] has three such constructors so we need three ID's.
        // We stick the parameter count into the lower bits to generate unique ids.)
        ArrayCtorJagged = unchecked((int)0x80000000),
    }
}
