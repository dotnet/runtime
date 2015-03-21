// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq.Expressions.Interpreter;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace System.Linq.Expressions.Interpreter
{
    internal interface ILightCallSiteBinder
    {
        bool AcceptsArgumentArray { get; }
    }
}
