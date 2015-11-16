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
