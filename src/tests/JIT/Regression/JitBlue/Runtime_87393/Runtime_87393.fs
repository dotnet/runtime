// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_87393

open System.Runtime.CompilerServices

[<AbstractClass>]
type Foo() =
    abstract M<'a> : 'a -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int -> int

type Bar() as this =
    inherit Foo()

    [<DefaultValue>]
    static val mutable private _f : Foo

    do
        Bar._f <- this

    override this.M<'a> (a0 : 'a) num acc _ _ _ _ _ _ _ _ _ _ _ _ _ =
        if num <= 0 then
            acc
        else
            Bar.M2 a0 (num - 1) (acc + num) 0 0 0 0 0 0 0 0 0 0 0 0 0

    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member M2 (a0 : 'a) (num : int) (acc : int) a3 a4 a5 a6 a7 a8 a9 a10 a11 a12 a13 a14 a15 =
        Bar._f.M a0 num acc a3 a4 a5 a6 a7 a8 a9 a10 a11 a12 a13 a14 a15

module Main =

    [<EntryPoint>]
    let main _argv =
        let f : Foo = Bar()
        let v = f.M 0 65000 0 0 0 0 0 0 0 0 0 0 0 0 0 0
        if v = 2112532500 then
            printfn "PASS"
            100
        else
            printfn "FAIL: Result was %A" v
            -1

