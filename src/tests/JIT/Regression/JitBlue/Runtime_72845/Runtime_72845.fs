// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// OSR method with modifiable this

[<EntryPoint>]
let main _ =
    let l1 = [0 .. 100000]
    let l2 = [0 .. 100000]
    let eq = l1 = l2
    printfn $"Lists equal: {eq}"
    if eq then 100 else -1
