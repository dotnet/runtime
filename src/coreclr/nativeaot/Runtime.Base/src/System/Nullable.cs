// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System
{
    internal struct Nullable<T> where T : struct
    {
#pragma warning disable 169 // The field 'blah' is never used
        private readonly bool _hasValue;
        private T _value;
#pragma warning restore 0169
    }
}
