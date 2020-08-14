// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Interface:  ICustomFormatter
**
**
** Purpose: Marks a class as providing special formatting
**
**
===========================================================*/

namespace System
{
    public interface ICustomFormatter
    {
        string Format(string? format, object? arg, IFormatProvider? formatProvider);
    }
}
