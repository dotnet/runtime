// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_136373

open System
open System.Runtime.CompilerServices
open Xunit

type D = delegate of obj * obj * obj * obj * obj * obj * obj * obj -> obj

type Maker() =
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    static member Make(captured: obj) =
        D(fun _ _ _ _ _ _ _ last -> if Object.ReferenceEquals(captured, last) then captured else null)

module Test =
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let private Invoke(value: obj) =
        (Maker.Make value).Invoke(null, null, null, null, null, null, null, value)

    [<Fact>]
    let TestEntryPoint() =
        let value = obj()
        Assert.Same(value, Invoke value)

    [<EntryPoint>]
    let main _ =
        TestEntryPoint()
        100
