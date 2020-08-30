// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public interface IReturnSpan
{
    ReadOnlySpan<byte> GetSpan();
}