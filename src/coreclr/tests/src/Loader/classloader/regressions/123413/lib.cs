// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class Base<A,B>
{
    public virtual void FV(ref MethodsFired pMF) {
      pMF |= MethodsFired.Base;
    }
}

[Flags]
public enum MethodsFired{
  None = 0x0000,
  Leaf = 0x0001,
  Interior = 0x0002,
  Base = 0x0004,
  All = Leaf | Interior | Base
}
