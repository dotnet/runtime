// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public interface I
{
    void M();
}
 
public class A : I
{
    public virtual void M() {}
}

public class B_JIT : A
{
    // Just to make sure that we do not have shared VTable indirection with parent
    public virtual void M2() {}
}
