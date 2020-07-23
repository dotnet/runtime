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

// This is a 16 byte struct
type Point2D = struct
    val X: double
    val Y: double

    new(x: double, y:double) = { X = x; Y = y}

    end

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

type DriverMethods() =
    static member FirstMethodCallee(firstArg: int, secondArg: int, thirdArg: int) = 
        if firstArg = 10 then firstArg * secondArg * thirdArg
        else firstArg + secondArg + thirdArg

    static member FirstMethod() = 
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        DriverMethods.FirstMethodCallee(firstArg, secondArg, thirdArg)

    static member SecondMethodCallee(iteration: int, firstArg: int, secondArg: int, thirdArg: int) = 
        if iteration = 0 then
            firstArg
        else
            let mutable retVal = 0
            if firstArg = 10 then 
                retVal <- firstArg * secondArg * thirdArg
            else 
                retVal <- firstArg + secondArg + thirdArg

            let newIteration = iteration - 1
            DriverMethods.SecondMethodCallee(newIteration, retVal, secondArg, thirdArg)

    static member SecondMethod() = 
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        DriverMethods.SecondMethodCallee(100, firstArg, secondArg, thirdArg)

    static member ThirdMethodCallee(iteration: int, 
                                    firstArg: int, 
                                    secondArg: int, 
                                    thirdArg: int,
                                    point: Point2D,
                                    secondPoint: Point2D,
                                    thirdPoint: Point2D) = 
        if iteration > 0 then
            let mutable retVal = 0
            if firstArg = 10 then 
                retVal <- firstArg * secondArg * thirdArg
            else 
                retVal <- firstArg + secondArg + thirdArg

            let newIteration = iteration - 1

            if retVal > 10000 then
                DriverMethods.ThirdMethodCallee(newIteration, 
                                                retVal, 
                                                secondArg, 
                                                thirdArg,
                                                point,
                                                secondPoint,
                                                thirdPoint)
            else
                DriverMethods.ThirdMethodCallee(newIteration, 
                                                secondArg, 
                                                thirdArg, 
                                                thirdArg,
                                                point,
                                                secondPoint,
                                                thirdPoint)
        else
            firstArg


    static member ThirdMethod() = 
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        let point = new Point2D(10.0, 20.0)
        let secondPoint = new Point2D(30.0, 40.0)

        DriverMethods.ThirdMethodCallee(100, 
                                        firstArg, 
                                        secondArg, 
                                        thirdArg,
                                        point,
                                        secondPoint,
                                        secondPoint)

    static member FourthMethod() = 
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        let point = new Point2D(10.0, 20.0)
        let secondPoint = new Point2D(30.0, 40.0)

        let rec fourthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D) = 
            if iterationCount = 0 then
                100
            else if iterationCount % 2 = 0 then
                fourthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
            else
                fourthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)

        and fourthMethodSecondCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D) =
            if iterationCount = 0 then
                101
            else if iterationCount % 2 = 0 then
                fourthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
            else
                fourthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)

        fourthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint)

    static member FifthMethod() = 
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        let point = new Point2D(10.0, 20.0)
        let secondPoint = new Point2D(30.0, 40.0)

        let rec fifthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D, fifthArg: Point2D) = 
            if iterationCount = 0 then
                100
            else if iterationCount % 2 = 0 then
                fifthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)
            else
                fifthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

        and fifthMethodSecondCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D, fifthArg:Point2D) =
            if iterationCount = 0 then
                101
            else if iterationCount % 2 = 0 then
                fifthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)
            else
                fifthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

        fifthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint, point)

    static member SixthMethod() =
        let firstArg = 10
        let secondArg = 20
        let thirdArg = 30

        let point = new Point2D(10.0, 20.0)
        let secondPoint = new Point2D(30.0, 40.0)

        let rec sixthMethodFirstCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D, fifthArg: Point2D) = 
            if iterationCount = 0 then
                100
            else if iterationCount % 2 = 0 then
                sixthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
            else
                sixthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fifthArg)

        and sixthMethodSecondCallee(iterationCount, firstArg: Point2D, secondArg: Point2D, thirdArg: Point2D, fourthArg: Point2D) =
            if iterationCount = 0 then
                101
            else if iterationCount % 2 = 0 then
                sixthMethodSecondCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg)
            else
                sixthMethodFirstCallee(iterationCount - 1, firstArg, secondArg, thirdArg, fourthArg, fourthArg)

        sixthMethodFirstCallee(1000000, point, secondPoint, point, secondPoint, point)

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

type Driver() = 

    // Notes:
    //
    // Drive all of the different methods that will be called and account for
    // timing.
    member this.Start() =
        let rec runMethod(iterationCount: int, cb: _ -> int) = 
            if iterationCount > 0 then
                cb() |> ignore
                runMethod(iterationCount - 1, cb)

        let runMethodWithTiming(methodName: string, worksOnlyOnNetCore5: bool, iterationCount: int, cb: _ -> int) = 
            let mutable canRun = true
            if worksOnlyOnNetCore5 = true then
                canRun <- System.Environment.Version.Major > 3

            if canRun = true then
                let startTime = DateTime.Now
                runMethod(iterationCount, cb) |> ignore
                let endTime = DateTime.Now

                let elapsedTime = (endTime - startTime).TotalMilliseconds

                printfn "[%s] - %fms" methodName elapsedTime
            else
                printfn "[%s] - skipped. Only works on .Net Core 5.0 and above" methodName

        runMethodWithTiming("FirstMethod", false, 100000, DriverMethods.FirstMethod)
        runMethodWithTiming("SecondMethod", false, 100000, DriverMethods.SecondMethod)

#if (RELEASE)
        // ThirdMethod will SO in debug
        runMethodWithTiming("ThirdMethod", false, 100000, DriverMethods.ThirdMethod)

        // FourthMethod will SO in debug
        runMethodWithTiming("FourthMethod", false, 10, DriverMethods.FourthMethod)
#endif

        // Fifth method will only work on >.NET Core 3.1
        runMethodWithTiming("FifthMethod", true, 10, DriverMethods.FifthMethod)

#if (RELEASE)
        runMethodWithTiming("SixthMethod", false, 10, DriverMethods.SixthMethod)
#endif

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

[<EntryPoint>]
let main argv =
    let driver = new Driver()
    driver.Start()

    // If we have gotten to this point we have not StackOverflowed. Therefore 
    // consider this a passing test
    100
