// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public struct NonFrozenStruct
{
    let a : Int32;
    let b : Int32;
    let c : Int32;
}

public func ReturnNonFrozenStruct(a: Int32, b: Int32, c: Int32) -> NonFrozenStruct {
    return NonFrozenStruct(a: a, b: b, c: c)
}

public func SumReturnedNonFrozenStruct(f: () -> NonFrozenStruct) -> Int32 {
    let s = f()
    return s.a + s.b + s.c
}