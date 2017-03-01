// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  ICustomFormatter
**
**
** Purpose: Marks a class as providing special formatting
**
**
===========================================================*/

using System;

namespace System
{
    public interface ICustomFormatter
    {
        // Interface does not need to be marked with the serializable attribute
        String Format(String format, Object arg, IFormatProvider formatProvider);
    }
}
