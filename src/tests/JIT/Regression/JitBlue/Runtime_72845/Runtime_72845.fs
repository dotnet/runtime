// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// OSR method with modifiable this

module Main

open Xunit
open Microsoft.DotNet.XUnitExtensions

[<Fact>]
[<SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)>]
let main () =
    let l1 = [0 .. 100000]
    let l2 = [0 .. 100000]
    let eq = l1 = l2
    Assert.True(eq)
