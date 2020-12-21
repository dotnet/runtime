// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public interface Int<A,B>
{
    void FV(ref MethodsFired pMF);
}

[Flags]
public enum MethodsFired{
  None = 0x0000,
  ExplicitInt = 0x0001,
  Public = 0x0004
}
