// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

////////////////////////////////////////////////////////////////////////////////
//
// Notes:
//
// This test will Stack overflow when built in debug. It relies on the FSharp
// compiler optimizing the tail calls to loops.
//
////////////////////////////////////////////////////////////////////////////////

open System

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// 16 byte struct
[<Struct>]
type Point2D(x: double, y: double) =
    member _.X = x
    member _.Y = y

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// F# compiler will optimize the call away
let first() =
    let callee firstArg secondArg thirdArg =
        if firstArg = 10 then firstArg * secondArg * thirdArg
        else firstArg + secondArg + thirdArg

    let retVal = callee 10 20 30

    if retVal <> 6000 then
        printfn "Method -- Failed, expected result: 6000, calculated: %d" retVal

        -1
    else
        0

// F# Compiler will optimize the call away and treat this as a loop
let second() =
    let rec secondCallee(iteration, firstArg, secondArg, thirdArg) = 
        if iteration = 0 then
            firstArg
        else
            let mutable retVal =
                if firstArg = 10 then 
                    firstArg * secondArg * thirdArg
                else 
                    firstArg + secondArg + thirdArg

            secondCallee(iteration - 1, retVal, secondArg, thirdArg)

    let retVal = secondCallee(100, 10, 20, 30)

    if retVal <> 10950 then
        printfn "Method -- Failed, expected result: 10950, calculated: %d" retVal
        -2
    else
        0

// F# Compiler will optimize the call away and treat this as a loop
let third() = 
    let rec thirdCallee(iteration, firstArg, secondArg, thirdArg, point: Point2D, secondPoint: Point2D, thirdPoint: Point2D) =
        if point.X <> 10.0 then -100
        else if point.Y <> 20.0 then -101
        else if secondPoint.X <> 30.0 then -102
        else if secondPoint.Y <> 40.0 then -102
        else if thirdPoint.X <> 30.0 then -103
        else if thirdPoint.Y <> 40.0 then -103
        else if iteration = 0 then
            firstArg
        else
            let mutable retVal =
                if firstArg = 10 then 
                    firstArg * secondArg * thirdArg
                else 
                    firstArg + secondArg + thirdArg
                    
            if retVal > 5000 then
                thirdCallee(iteration - 1, retVal, secondArg, thirdArg, point, secondPoint, thirdPoint)
            else
                thirdCallee(iteration - 1, secondArg, thirdArg, thirdArg, point, secondPoint, thirdPoint)

    let point = Point2D(10.0, 20.0)
    let secondPoint = Point2D(30.0, 40.0)

    let retVal = thirdCallee(100, 10, 20, 30, point, secondPoint, secondPoint)

    if retVal <> 10950 then
        printfn "Method -- Failed, expected result: 10950, calculated: %d" retVal
        -3
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a fast tail call on unix x64 as there is no stack usage
let fourth() =
    let rec fourthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D) = 
        if firstArg.X <> 10.0 then -100
        else if firstArg.Y <> 20.0 then -101
        else if secondArg.X <> 30.0 then -102
        else if secondArg.Y <> 40.0 then -103
        else if thirdArg.X <> 10.0 then -104
        else if thirdArg.Y <> 20.0 then -105
        else if fourthArg.X <> 30.0 then -106
        else if fourthArg.Y <> 40.0 then -107
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            fourthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
        else
            fourthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)

    and fourthMethodSecondCallee(iterationCount, firstArg, secondArg, thirdArg, fourthArg) =
        if firstArg.X <> 10.0 then -150
        else if firstArg.Y <> 20.0 then -151
        else if secondArg.X <> 30.0 then -152
        else if secondArg.Y <> 40.0 then -153
        else if thirdArg.X <> 10.0 then -154
        else if thirdArg.Y <> 20.0 then -155
        else if fourthArg.X <> 30.0 then -156
        else if fourthArg.Y <> 40.0 then -157
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            fourthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
        else
            fourthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
    
    let point = Point2D(10.0, 20.0)
    let secondPoint = Point2D(30.0, 40.0)

    let retVal = fourthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -4
    else
        0
    
// Will create a tail il instruction and force a tail call. This is will become
// a fast tail call on unix x64 as the caller and callee have equal stack size
let fifth() =
    let rec fifthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D, fifthArg: Point2D) = 
        if firstArg.X <> 10.0 then -100
        else if firstArg.Y <> 20.0 then -101
        else if secondArg.X <> 30.0 then -102
        else if secondArg.Y <> 40.0 then -103
        else if thirdArg.X <> 10.0 then -104
        else if thirdArg.Y <> 20.0 then -105
        else if fourthArg.X <> 30.0 then -106
        else if fourthArg.Y <> 40.0 then -107
        else if fifthArg.X <> 10.0 then -108
        else if fifthArg.Y <> 20.0 then -109
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            fifthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)
        else
            fifthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

    and fifthMethodSecondCallee(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg) =
        if firstArg.X <> 10.0 then -150
        else if firstArg.Y <> 20.0 then -151
        else if secondArg.X <> 30.0 then -152
        else if secondArg.Y <> 40.0 then -153
        else if thirdArg.X <> 10.0 then -154
        else if thirdArg.Y <> 20.0 then -155
        else if fourthArg.X <> 30.0 then -156
        else if fourthArg.Y <> 40.0 then -157
        else if fifthArg.X <> 10.0 then -158
        else if fifthArg.Y <> 20.0 then -159
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            fifthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)
        else
            fifthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

    let point = Point2D(10.0, 20.0)
    let secondPoint = Point2D(30.0, 40.0)

    let retVal = fifthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint, point)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -5
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a tail call via helper on unix x64 as the caller has less available incoming
// arg size than the callee
let sixth() =
    let rec sixthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D, fifthArg: Point2D) = 
        if firstArg.X <> 10.0 then -100
        else if firstArg.Y <> 20.0 then -101
        else if secondArg.X <> 30.0 then -102
        else if secondArg.Y <> 40.0 then -103
        else if thirdArg.X <> 10.0 then -104
        else if thirdArg.Y <> 20.0 then -105
        else if fourthArg.X <> 30.0 then -106
        else if fourthArg.Y <> 40.0 then -107
        else if fifthArg.X <> 10.0 then -108
        else if fifthArg.Y <> 20.0 then -109
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            sixthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
        else
            sixthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

    and sixthMethodSecondCallee(iterationCount, firstArg, secondArg, thirdArg, fourthArg) =
        if firstArg.X <> 10.0 then -150
        else if firstArg.Y <> 20.0 then -151
        else if secondArg.X <> 30.0 then -152
        else if secondArg.Y <> 40.0 then -153
        else if thirdArg.X <> 10.0 then -154
        else if thirdArg.Y <> 20.0 then -155
        else if fourthArg.X <> 30.0 then -156
        else if fourthArg.Y <> 40.0 then -157
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            sixthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
        else
            sixthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, thirdArg)

    let point = new Point2D(10.0, 20.0)
    let secondPoint = new Point2D(30.0, 40.0)

    let retVal = sixthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint, point)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -6
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a tail call via helper on unix x64 as the caller has less available incoming
// arg size than the callee
let seventh() =
    let rec seventhMethodFirstCallee(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if seventhArg <> 7 then -106
        else if eighthArg <> 8 then -107
        else if ninethArg <> 9 then -108
        else if tenthArg <> 10 then -109
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCallee(iterationCount)
        else
            seventhMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg)

    and seventhMethodSecondCallee(iterationCount) = 
        if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCallee(iterationCount - 1)
        else
            seventhMethodFirstCallee(iterationCount - 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

    let retVal = seventhMethodFirstCallee(1000000, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -7
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a fast tail call as the caller and callee have the incoming arg size
let seventhFastTailCall() =
    let rec seventhMethodFirstCalleeFastTailCall(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if seventhArg <> 7 then -106
        else if eighthArg <> 8 then -107
        else if ninethArg <> 9 then -108
        else if tenthArg <> 10 then -109
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg)
        else
            seventhMethodFirstCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg)

    and seventhMethodSecondCalleeFastTailCall(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg) = 
        if firstArg <> 1 then -110
        else if secondArg <> 2 then -111
        else if thirdArg <> 3 then -112
        else if fourthArg <> 4 then -113
        else if fifthArg <> 5 then -114
        else if sixthArg <> 6 then -115
        else if seventhArg <> 7 then -116
        else if eighthArg <> 8 then -117
        else if ninethArg <> 9 then -118
        else if tenthArg <> 10 then -119
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg)
        else
            seventhMethodFirstCalleeFastTailCall(iterationCount - 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

    let retVal = seventhMethodFirstCalleeFastTailCall(1000000, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -8
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a fast tail call as the caller and callee have the incoming arg size
let seventhFastTailCallReversed() =
    let rec seventhMethodFirstCalleeFastTailCallReversed(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if seventhArg <> 7 then -106
        else if eighthArg <> 8 then -107
        else if ninethArg <> 9 then -108
        else if tenthArg <> 10 then -109
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCalleeFastTailCallReversed(iterationCount - 1, tenthArg, ninethArg, eighthArg, seventhArg, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)
        else
            seventhMethodFirstCalleeFastTailCallReversed(iterationCount - 1, tenthArg, ninethArg, eighthArg, seventhArg, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)

    and seventhMethodSecondCalleeFastTailCallReversed(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg, seventhArg, eighthArg, ninethArg, tenthArg) = 
        if firstArg <> 10 then -110
        else if secondArg <> 9 then -111
        else if thirdArg <> 8 then -112
        else if fourthArg <> 7 then -113
        else if fifthArg <> 6 then -114
        else if sixthArg <> 5 then -115
        else if seventhArg <> 4 then -116
        else if eighthArg <> 3 then -117
        else if ninethArg <> 2 then -118
        else if tenthArg <> 1 then -119
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            seventhMethodSecondCalleeFastTailCallReversed(iterationCount - 1, tenthArg, ninethArg, eighthArg, seventhArg, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)
        else
            seventhMethodFirstCalleeFastTailCallReversed(iterationCount - 1, tenthArg, ninethArg, eighthArg, seventhArg, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)

    let retVal = seventhMethodFirstCalleeFastTailCallReversed(1000000, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -8
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a tail call via helper on unix x64 as the caller has less available incoming
// arg size than the callee
let eight() =
    let rec eightMethodFirstCallee(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            eightMethodSecondCallee(iterationCount)
        else
            eightMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg)

    and eightMethodSecondCallee(iterationCount) = 
        if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            eightMethodSecondCallee(iterationCount - 1)
        else
            eightMethodFirstCallee(iterationCount - 1, 1, 2, 3, 4, 5, 6)

    let retVal = eightMethodFirstCallee(1000000, 1, 2, 3, 4, 5, 6)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -9
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a tail call via helper on unix x64 as the caller has less available incoming
// arg size than the callee
let eightFastTailCall() =
    let rec eightMethodFirstCalleeFastTailCall(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            eightMethodSecondCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg)
        else
            eightMethodFirstCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg)

    and eightMethodSecondCalleeFastTailCall(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg) = 
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            eightMethodSecondCalleeFastTailCall(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg)
        else
            eightMethodFirstCalleeFastTailCall(iterationCount - 1, 1, 2, 3, 4, 5, 6)

    let retVal = eightMethodFirstCalleeFastTailCall(1000000, 1, 2, 3, 4, 5, 6)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -10
    else
        0

// Will create a tail il instruction and force a tail call. This is will become
// a tail call via helper on unix x64 as the caller has less available incoming
// arg size than the callee
//
// Reversing the call arguments is a simple way to stress LowerFastTailCall
let eightFastTailCallReversed() =
    let rec eightMethodFirstCalleeFastTailCallReversed(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg) =
        if firstArg <> 1 then -100
        else if secondArg <> 2 then -101
        else if thirdArg <> 3 then -102
        else if fourthArg <> 4 then -103
        else if fifthArg <> 5 then -104
        else if sixthArg <> 6 then -105
        else if iterationCount = 0 then
            100
        else if iterationCount % 2 = 0 then
            eightMethodSecondCalleeFastTailCallReversed(iterationCount - 1, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)
        else
            eightMethodFirstCalleeFastTailCallReversed(iterationCount - 1, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)

    and eightMethodSecondCalleeFastTailCallReversed(iterationCount, firstArg, secondArg, thirdArg, fourthArg, fifthArg, sixthArg) = 
        if firstArg <> 6 then -100
        else if secondArg <> 5 then -101
        else if thirdArg <> 4 then -102
        else if fourthArg <> 3 then -103
        else if fifthArg <> 2 then -104
        else if sixthArg <> 1 then -105
        else if iterationCount = 0 then
            101
        else if iterationCount % 2 = 0 then
            eightMethodSecondCalleeFastTailCallReversed(iterationCount - 1, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)
        else
            eightMethodFirstCalleeFastTailCallReversed(iterationCount - 1, sixthArg, fifthArg, fourthArg, thirdArg, secondArg, firstArg)

    let retVal = eightMethodFirstCalleeFastTailCallReversed(1000000, 1, 2, 3, 4, 5, 6)

    if retVal <> 100 && retVal <> 101 then
        printfn "Method -- Failed, expected result: 100 or 101, calculated: %d" retVal
        -11
    else
        0

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

type Driver() = 
    // Notes:
    //
    // Drive all of the different methods that will be called and account for
    // timing.
    member _.Start() =
        let rec runMethod(iterationCount: int, cb: _ -> int) = 
            if iterationCount > 0 then
                let retVal = cb()

                if retVal <> 0 then
                    retVal
                else
                    runMethod(iterationCount - 1, cb)
            else
                0

        let runMethodWithTiming(methodName: string, worksOnlyOnNetCore5: bool, iterationCount: int, cb: _ -> int) = 
            let canRun = (worksOnlyOnNetCore5 && System.Environment.Version.Major > 3) || worksOnlyOnNetCore5 = false

            if canRun then
                let startTime = DateTime.Now
                let retVal = runMethod(iterationCount, cb)
                let endTime = DateTime.Now

                let elapsedTime = (endTime - startTime).TotalMilliseconds

                if retVal <> 0 then
                    failwith "Incorrect method passed"

                printfn "[%s] - %fms" methodName elapsedTime
            else
                printfn "[%s] - skipped. Only works on .Net Core 5.0 and above" methodName

        runMethodWithTiming("FirstMethod", false, 100000, first)
        runMethodWithTiming("SecondMethod", false, 100000, second)

        // ThirdMethod will SO in debug
        runMethodWithTiming("ThirdMethod", false, 100000, third)

        // FourthMethod will SO in debug
        runMethodWithTiming("FourthMethod", false, 10, fourth)

        // The rest of the methods require generic tailcall helper therefore
        // will only work on >3.1

        // All the following SO in debug

        runMethodWithTiming("FifthMethod", true, 10, fifth)
        runMethodWithTiming("SixthMethod", true, 10, sixth)
        runMethodWithTiming("SeventhMethod", true, 10, seventh)
        runMethodWithTiming("SeventhMethodFastTailCall", false, 10, seventhFastTailCall)
        runMethodWithTiming("SeventhMethodFastTailCallReversed", false, 10, seventhFastTailCallReversed)
        runMethodWithTiming("EigthhMethod", true, 10, eight)
        runMethodWithTiming("EigthMethodFastTailCall", false, 10, eightFastTailCall)
        runMethodWithTiming("EigthMethodFastTailCallReversed", false, 10, eightFastTailCallReversed)

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

[<EntryPoint>]
let main argv =
    let driver = Driver()
    driver.Start()

    // If we have gotten to this point we have not StackOverflowed. Therefore 
    // consider this a passing test
    100
