// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.CodeSign.Requirements
{
    public enum ExpressionMatchType : int
    {
        Exists,
        Equal,
        Contains,
        BeginsWith,
        EndsWith,
        LessThan,
        GreaterThan,
        LessEqual,
        GreaterEqual,
        On,
        Before,
        After,
        OnOrBefore,
        OnOrAfter,
        Absent,
    }
}