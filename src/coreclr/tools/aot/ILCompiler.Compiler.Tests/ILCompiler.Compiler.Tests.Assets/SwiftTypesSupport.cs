// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace ILCompiler.Compiler.Tests.Assets.SwiftTypes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ExpectedLoweringAttribute : Attribute
{
    public ExpectedLoweringAttribute()
    {
    }

    public ExpectedLoweringAttribute(Lowered expectedLowering1)
    {
    }

    public ExpectedLoweringAttribute(Lowered expectedLowering1, Lowered expectedLowering2)
    {
    }

    public ExpectedLoweringAttribute(Lowered expectedLowering1, Lowered expectedLowering2, Lowered expectedLowering3)
    {
    }

    public ExpectedLoweringAttribute(Lowered expectedLowering1, Lowered expectedLowering2, Lowered expectedLowering3, Lowered expectedLowering4)
    {
    }

    public enum Lowered
    {
        Float,
        Double,
        Int8,
        Int16,
        Int32,
        Int64
    }
}
