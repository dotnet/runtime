// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
