// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly repro { }

// Small repro for ilasm/ildasm roundtrip issue with duplicated constraints
// This IL represents:
// public interface IComp<TSelf> : IEq<TSelf> where TSelf : IComp<TSelf>? { }
// public interface IEq<TSelf> where TSelf : IEq<TSelf>? { }

.class interface public abstract auto ansi beforefieldinit IEq`1<(class IEq`1<!TSelf>) TSelf>
{
}

.class interface public abstract auto ansi beforefieldinit IComp`1<(class IComp`1<!TSelf>) TSelf>
       implements class IEq`1<!TSelf>
{
}
