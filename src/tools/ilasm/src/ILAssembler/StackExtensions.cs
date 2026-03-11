// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ILAssembler
{
    internal static class StackExtensions
    {
        public static T? PeekOrDefault<T>(this Stack<T> stack) => stack.Count == 0 ? default : stack.Peek();
    }
}
