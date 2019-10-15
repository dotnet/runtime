// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum BinOpKind
    {
        Add,
        Sub,
        Mul,
        Shift,
        Equal,
        Compare,
        Bitwise,
        BitXor,
        Logical,
        Lim
    }

    [Flags]
    internal enum BinOpMask
    {
        None = 0,
        Add = 1 << BinOpKind.Add,
        Sub = 1 << BinOpKind.Sub,
        Mul = 1 << BinOpKind.Mul,
        Shift = 1 << BinOpKind.Shift,
        Equal = 1 << BinOpKind.Equal,
        Compare = 1 << BinOpKind.Compare,
        Bitwise = 1 << BinOpKind.Bitwise,
        BitXor = 1 << BinOpKind.BitXor,
        Logical = 1 << BinOpKind.Logical,
        // The different combinations needed in operators.cs
        Integer = Add | Sub | Mul | Equal | Compare | Bitwise | BitXor,
        Real = Add | Sub | Mul | Equal | Compare,
        BoolNorm = Equal | BitXor,
        // These are special ones.
        Delegate = Add | Sub | Equal,
        Enum = Sub | Equal | Compare | Bitwise | BitXor,
        EnumUnder = Add | Sub,
        UnderEnum = Add,
    }
}
