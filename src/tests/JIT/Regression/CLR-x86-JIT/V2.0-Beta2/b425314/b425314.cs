// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using Xunit;

#if false
Here is the bug text from the bugs that motivated this regression case.  I've annotated
the text to hopefully make it clearer where the type safety violations were occurring.

Note:

    Both of the bugs deal with JIT trees that were invalid because they loaded the same
    expression (e.g., this.m_a) multiple times and implicitly assumed that the same value
    would be loaded each time.

    It is crucial to compile this code using /debug to ensure that these multiple loads
    aren't collapsed into one by the optimizer.

Text from the first bug report:

    Unboxing an object generates code to check if the type of the object is as expected, and
    then to generate the byref by adding to the object pointer.

    These two non-atomic operations expose a race if the object is not a local variable, and
    can be mutated by another thread.

    This is the tree generated for unboxing an int.  Note the two occurrences of "field ref m_obj".


    [[
        If mutation happens, the this.m_obj reload at 0x001A4EC8 will load a different object
        than the one that was validated in the QMARK (loads at 0x001A4D00 and 0x001A4DC8).

        This means 0x001A5160 dereferences a pointer into an unknown object that may not be a
        boxed int.  This is a type safety violation.
    ]]


    // Recover the boxed int by dereferencing the computed pointer to the embedded valuetype
    // content in the boxed object.
    //

    [001A5160] 0x0027 --CXG-----W-               indir     int

            // Load a pointer to the content embedded in this boxed object (at offset 0x4).
            //

             [001A50B8] 0x0024 ------------               const     int    4
          [001A50F0] 0x0025 ---XG-------               +         byref
             [001A4EC8] 0x001C ---XG-------               field     ref    m_obj <--------------------------------------
                [001A4E90] 0x001B ------------               lclVar    ref    V00 (this)

       [001A5128] 0x0026 --CXG-------               comma     byref

                [001A5010] 0x0021 ------------ then          nop       void

             [001A5048] 0x0022 --CXG-------               colon     void

                // This tree runs if the fastpath check fails.  Invoke the unbox helper.
                //

                [001A4FC8] 0x0020 --CXG------- else         call help  void   HELPER.CORINFO_HELP_UNBOX
                   [001A4F10] 0x001D ------------ arg0 on STK   const(h)  int    0x5BA855A0 class
                   [001A4DC8] 0x0018 ---XG------- arg1 on STK   field     ref    m_obj
                      [001A4D90] 0x0017 ------------               lclVar    ref    V00 (this)

          [001A5080] 0x0023 --CXG-------               qmark     void

                // Fastpath check to see if the object's MethodTable is exactly equal to 0x5BA855A0.
                //

                [001A4D48] 0x0016 ------------               const(h)  int    0x5BA855A0 class
             [001A4E58] 0x001A ---XG-------    if         ==        int
                [001A4E10] 0x0019 ---XG-------               indir     int
                   [001A4D00] 0x0015 ---XG-------               field     ref    m_obj <--------------------------------------
                      [001A4CC8] 0x0014 ------------               lclVar    ref    V00 (this)


    This happens because of the use of Compiler::impCloneExpr() to clone the object. The fix
    is to assign the object to a local variable, and to unbox the local variable.

    The second bug exposes a similar race with code generated for calling a generic virtual method.


Text from the second bug report:

    Calling a generic method generates code to look up the function pointer based on the
    "this" argument, and then to call the resulting function pointer and passing it the
    "this" argument.

    However, the value for "this" could be mutated from another thread if it is not a local
    variable.

    These are the 2 trees generated.

    Note that "this.m_a" is the "this" argument for the call, and it can be mutated by
    another thread.

    Hence we will end up calling the resulting function pointer on a mutated value.


    [[
        With this tree, type safety can be violated if this.m_a changes between 0x001A416C and
        0x001A425C.

        Say there is a change and the former loads X while the latter loads Y.

        The call will go to the virtual function implementation F that was found on object X.
        The `this' argument to F will be Y.

        The codegen for F will make all sorts of `this'-relative accesses that are only valid if
        the runtime type of `this' is X or one or X's subclasses.  If the runtime type of Y is
        not equal to (or a subclass of) the runtime type of X, then running F is very likely to
        violate type safety.
    ]]


     [001A4494] 0x000F ------------               stmtExpr  void  (IL 001h...010h)

           [001A43DC] 0x000C --CXG-------              call help  int    HELPER.CORINFO_HELP_VIRTUAL_FUNC_PTR

                // HelperArg1 - this.m_a
                //
              [001A416C] 0x0002 ---XG------- arg0 on STK   field     ref    m_a
                 [001A4134] 0x0001 ------------               lclVar    ref    V00 (this)

                // HelperArg2 - class handle 0x2c111f8
                //
              [001A42A4] 0x0007 ------------ arg1 on STK   const(h)  int    0x2C111F8 class

                // HelperArg3 - token handle 0x2c112e8
                //
              [001A42EC] 0x0008 ----------W- arg2 on STK   const(h)  int    0x2C112E8 token

        [001A445C] 0x000E -ACXG-------               =         int

                // V02 receives the result of the CORINFO_HELP_VIRTUAL_FUNC_PTR call.
                //
           [001A4424] 0x000D D------N----               lclVar    int    V02 (tmp0)



     [001A463C] 0x0016 ------------               stmtExpr  void  (IL FFFh...FFFh)

              [001A455C] 0x0012 ------------               const     int    5

           [001A4594] 0x0013 --CX--------               ==        int

                // Call through the virtual function pointer looked up above.
                //
              [001A4514] 0x0011 --CX--------              call ind   int

                    // `this' ptr passed to looked up target - this.m_a.
                    //
                 [001A425C] 0x0006 ---XG------- this in ECX   field     ref    m_a
                    [001A4224] 0x0005 ------------               lclVar    ref    V00 (this)

                    // Arg1 to looked up target - 0.
                    //
                 [001A41B4] 0x0003 ------------ arg1 on STK   const     int    0

                    // V02 supplies the special "calli tgt" arg that indicates how to find the target address
                    // to use for the call.
                    //
                 [001A44DC] 0x0010 ------------ calli tgt     lclVar    int    V02 (tmp0)

        [001A4604] 0x0015 -ACX--------               =         int

           [001A45CC] 0x0014 D------N----               lclVar    int    V01 (loc0)
#endif

public static class Util
{
    public static Timer MakeOneShotTimer(TimerCallback callback, int dueTimeInMilliseconds)
    {
        return new Timer(callback, null, dueTimeInMilliseconds, Timeout.Infinite);
    }

    internal static void PrintFailureAndAddToTestResults(string format, params object[] args)
    {
        Console.WriteLine(format, args);
        Mutate.RecordFailure(new Exception(String.Format(format, args)));
        return;
    }
}

public enum ScenarioKind
{
    Invalid = 0,
    WithThrowingChecks = 1,
    WithNonThrowingChecks = 2,
}

public class ScenarioMonitor
{
    //
    // These thresholds were chosen more or less arbitrarily.  In rough terms, the idea was to
    // pick thresholds that can be reached in less than 1 billion CPU cycles, meaning they can
    // be reached fairly quickly (within a handful of seconds) even on relatively slow hardware
    // (e.g., CPUs that run at 1GHz or slower) or relatively busy systems.
    //
    // In cases with throwing checks (i.e., scenarios where ~50% of the checks will generate an
    // exception), the cost of throwing and catching these exceptions is very high and drives
    // down the number of iterations that are possible in 1 billion CPU cycles.
    //

    private const long MinimumCheckIterationCount_ForThrowingChecks = 2000L;
    private const long MinimumCheckIterationCount_ForNonThrowingChecks = 50000L;
    private const long MinimumFlipIterationCount = 100000L;


    //
    // Force each scenario to run for at least 3 seconds.  If the scenario doesn't make the
    // required amount of progress within 3 minutes, signal a test failure.
    //
    // The maximum is set very high to try and minimize the chance of seeing a spurious test
    // failure when the test and the CLR are operating fine, just very slowly.  This can
    // happen, e.g., if a test machine is otherwise very busy when this test runs (busy with
    // antivirus activity, busy with other tests in a multiworker run, etc).
    //

    private const int MinimumExecutionTimeInMilliseconds = (3 * 1000);
    private const int MaximumExecutionTimeInMilliseconds = (180 * 1000);


    //
    // Define the volatile fields that will be used (generally using cross-thread accesses) to
    // track whether or not the scenario has hit the different relevant thresholds.
    //

    private volatile bool _fMinimumTimeHasElapsed = false;
    private volatile bool _fMaximumTimeHasElapsed = false;
    private volatile bool _fAllRequiredCheckIterationsHaveOccurred = false;
    private volatile bool _fAllRequiredFlipIterationsHaveOccurred = false;
    private volatile bool _fScenarioHasEnded = false;
    private volatile bool _fScenarioExceededTheMaximumTime = false;


    private void MinimumTimeHasElapsed(object state) { _fMinimumTimeHasElapsed = true; }
    private void MaximumTimeHasElapsed(object state) { _fMaximumTimeHasElapsed = true; }


    private string _caption;
    private long _minimumCheckIterationCount;


    public ScenarioMonitor(string caption, ScenarioKind kind)
    {
        _caption = caption;

        if (kind == ScenarioKind.WithThrowingChecks)
        {
            _minimumCheckIterationCount = ScenarioMonitor.MinimumCheckIterationCount_ForThrowingChecks;
        }
        else
        {
            _minimumCheckIterationCount = ScenarioMonitor.MinimumCheckIterationCount_ForNonThrowingChecks;
        }

        return;
    }


    public void RunScenario(ThreadStart runCheckThread, Action runFlipThread)
    {
        Thread checkThread;
        TimeSpan elapsedTime;
        DateTime endTime;
        DateTime startTime;
        Timer timerForMaximumTime;
        Timer timerForMinimumTime;


        //
        // Start the "check" thread for this scenario.
        //

        startTime = DateTime.Now;
        checkThread = new Thread(runCheckThread);
        checkThread.Start();
        Thread.Sleep(0);


        //
        // Start one-shot timers that will fire after the minimum and maximum execution times
        // elapse, respectively.
        //

        using (timerForMinimumTime = Util.MakeOneShotTimer(this.MinimumTimeHasElapsed, ScenarioMonitor.MinimumExecutionTimeInMilliseconds))
        using (timerForMaximumTime = Util.MakeOneShotTimer(this.MaximumTimeHasElapsed, ScenarioMonitor.MaximumExecutionTimeInMilliseconds))
        {
            // Use this thread to run the "flip" thread (hopefully in parallel with the "check" thread)
            // until the scenario ends (either due to timeout or due to completing the required amount
            // of work).
            //
            runFlipThread();

            // The "flip" thread has exited, meaning the scenario is complete.  Block until the "check"
            // thread terminates.
            //
            checkThread.Join();
        }


        if (_fScenarioHasEnded)
        {
            if (_fScenarioExceededTheMaximumTime)
            {
                Util.PrintFailureAndAddToTestResults(
                    "Timeout: The `{0}' scenario exceeded the time limit ({1}s) without reaching the basic work thresholds.",
                    _caption,
                    (ScenarioMonitor.MaximumExecutionTimeInMilliseconds / 1000)
                );
            }
            else
            {
                endTime = DateTime.Now;
                elapsedTime = (endTime - startTime);

                Console.WriteLine(
                    "{0}: Checks={1}, Flips={2}, Elapsed={3}",
                    _caption,
                    _checkIterations,
                    _flipIterations,
                    elapsedTime.ToString()
                );
            }
        }
        else
        {
            Util.PrintFailureAndAddToTestResults("Internal error: The `{0}' scenario did not end as expected.", _caption);
        }

        return;
    }


    // This non-volatile data and data accessor can only be used on the check thread.
    private long _checkIterations;
    public void RecordCheckIteration()
    {
        _checkIterations += 1;

        if (_checkIterations >= _minimumCheckIterationCount)
        {
            _fAllRequiredCheckIterationsHaveOccurred = true;
        }

        return;
    }


    // This non-volatile data and data accessor can only be used on the flip thread.
    private long _flipIterations;
    public void RecordFlipIteration()
    {
        _flipIterations += 1;

        if (_flipIterations >= ScenarioMonitor.MinimumFlipIterationCount)
        {
            _fAllRequiredFlipIterationsHaveOccurred = true;
        }

        return;
    }


    public bool ScenarioShouldContinueRunning()
    {
        if (_fScenarioHasEnded)
        {
            // Once any call to this function determines that the scenario has ended, all future calls
            // return false and take no other action.  This invariant makes it possible to record
            // better information about the exact reason (timeout or threshold satisfaction) that first
            // moves the scenario from "running" to "ended".
            //
            return false;
        }
        else
        {
            if (_fMaximumTimeHasElapsed)
            {
                // The maximum execution time has elapsed, meaning the scenario needs to end
                // immediately.
                //
                _fScenarioExceededTheMaximumTime = true;
                _fScenarioHasEnded = true;
                return false;
            }
            else
            {
                if (_fMinimumTimeHasElapsed &&
                    _fAllRequiredCheckIterationsHaveOccurred &&
                    _fAllRequiredFlipIterationsHaveOccurred)
                {
                    // Basic requirements have been met for execution time and iteration counts, so the
                    // scenario does not need to continue.
                    //
                    _fScenarioExceededTheMaximumTime = false;
                    _fScenarioHasEnded = true;
                    return false;
                }
                else
                {
                    // Some basic requirement for execution time or iteration count has not been met and
                    // more time is available before hitting the maximum time limit.  Continue running the
                    // scenario until it fulfills the basic requirements or hits the maximum time limit.
                    //
                    return true;
                }
            }
        }
    }
}


//************************************************************************

public class CastClass
{
    private static ScenarioMonitor s_monitor = null;

    private object _obj;
    private volatile object _objVolatile;
    private static object s_obj;
    private volatile static object s_objVolatile;

    private static object[] s_objs;

    static CastClass()
    {
        s_objs = new object[2];
        s_objs[0] = new object();
        s_objs[1] = "Hello";
    }

    public CastClass()
    {
        _obj = s_objs[0];
        _objVolatile = s_objs[1];
        s_obj = s_objs[0];
        s_objVolatile = s_objs[1];
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static public object GetObject()
    {
        return s_obj;
    }


    public static void CheckVal(string str)
    {
        if (str[0] != 'H')
        {
            throw new Exception("Bad string " + str);
        }
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CheckObjects()
    {
        string str = null;
        ulong i;

        try
        {
            for (i = 0; CastClass.s_monitor.ScenarioShouldContinueRunning(); i++)
            {
                CastClass.s_monitor.RecordCheckIteration();

                switch (i % 5)
                {
                    case 0:
                        try { str = (string)_obj; } catch (InvalidCastException) { continue; }
                        break;

                    case 1:
                        try { str = (string)_objVolatile; } catch (InvalidCastException) { continue; }
                        break;

                    case 2:
                        try { str = (string)s_obj; } catch (InvalidCastException) { continue; }
                        break;

                    case 3:
                        try { str = (string)s_objVolatile; } catch (InvalidCastException) { continue; }
                        break;

                    case 4:
                        try { str = (string)CastClass.GetObject(); } catch (InvalidCastException) { continue; }
                        break;

                    default:
                        throw new Exception("We should never get here");
                }


                //
                // No exception occurred during this iteration of the loop, meaning the cast successfully
                // yielded a string object.  Check the computed string value.
                //
                // The race condition cases involve System.Object instances being returned from seemingly
                // successful casts.  In this case, the fields checked by CheckVal will actually be part of
                // a System.Object instance, meaning they are unlikely to match the text pattern that
                // CheckVal expects.
                //
                // In this case, CheckVal throws an exception to signal the fact that a type safety
                // violation has occurred.
                //

                CastClass.CheckVal(str);
            }
        }
        catch (Exception ex)
        {
            Mutate.RecordFailure(ex);
        }

        return;
    }


    private void Flip()
    {
        for (uint i = 0; CastClass.s_monitor.ScenarioShouldContinueRunning(); i++)
        {
            CastClass.s_monitor.RecordFlipIteration();

            // Rotate the current (i % 2) object into the instance member and static member variable
            // sets.

            _objVolatile = _obj;
            _obj = s_objs[i % 2];

            s_objVolatile = s_obj;
            s_obj = s_objs[i % 2];
        }

        return;
    }


    public static void Test()
    {
        CastClass o = new CastClass();
        CastClass.s_monitor = new ScenarioMonitor("CastClass", ScenarioKind.WithThrowingChecks);
        CastClass.s_monitor.RunScenario(o.CheckObjects, o.Flip);
        return;
    }
}


//************************************************************************

public class Unbox
{
    private const int VAL = 0x50005000;

    private static ScenarioMonitor s_monitor = null;

    private object _obj;
    private volatile object _objVolatile;
    private static object s_obj;
    private volatile static object s_objVolatile;

    private static object[] s_objs;

    static Unbox()
    {
        s_objs = new object[2];
        s_objs[0] = (object)VAL;
        s_objs[1] = "Hello";
    }

    public Unbox()
    {
        _obj = s_objs[0];
        _objVolatile = s_objs[1];
        s_obj = s_objs[0];
        s_objVolatile = s_objs[1];
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static public object GetObject()
    {
        return s_obj;
    }


    public static void CheckVal(int val)
    {
        if (val != VAL)
        {
            throw new Exception("Bad value " + val);
        }
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CheckObjects()
    {
        int val = 0;
        ulong i;

        try
        {
            for (i = 0; Unbox.s_monitor.ScenarioShouldContinueRunning(); i++)
            {
                Unbox.s_monitor.RecordCheckIteration();

                switch (i % 5)
                {
                    case 0:
                        try { val = (int)_obj; } catch (InvalidCastException) { continue; }
                        break;

                    case 1:
                        try { val = (int)_objVolatile; } catch (InvalidCastException) { continue; }
                        break;

                    case 2:
                        try { val = (int)s_obj; } catch (InvalidCastException) { continue; }
                        break;

                    case 3:
                        try { val = (int)s_objVolatile; } catch (InvalidCastException) { continue; }
                        break;

                    case 4:
                        try { val = (int)Unbox.GetObject(); } catch (InvalidCastException) { continue; }
                        break;

                    default:
                        throw new Exception("We should never get here");
                }


                //
                // No exception occurred during this iteration of the loop, meaning the cast successfully
                // extracted an int32 value from a boxed int32 object.  Check the computed unboxed value.
                //
                // The race condition cases involve the unboxed data being extracted from a System.String
                // object instead of a boxed System.Int32 object.  In this case, the fields checked by
                // CheckVal will actually be part of a System.String instance, meaning they are unlikely to
                // match the precise VAL that CheckVal expects to see.
                //
                // In this case, CheckVal throws an exception to signal the fact that a type safety
                // violation has occurred.
                //

                Unbox.CheckVal(val);
            }
        }
        catch (Exception ex)
        {
            Mutate.RecordFailure(ex);
        }

        return;
    }


    private void Flip()
    {
        for (uint i = 0; Unbox.s_monitor.ScenarioShouldContinueRunning(); i++)
        {
            Unbox.s_monitor.RecordFlipIteration();

            // Rotate the current (i % 2) object into the instance member and static member variable
            // sets.

            _objVolatile = _obj;
            _obj = s_objs[i % 2];

            s_objVolatile = s_obj;
            s_obj = s_objs[i % 2];
        }
    }


    public static void Test()
    {
        Unbox o = new Unbox();
        Unbox.s_monitor = new ScenarioMonitor("Unbox", ScenarioKind.WithThrowingChecks);
        Unbox.s_monitor.RunScenario(o.CheckObjects, o.Flip);
        return;
    }
}


//************************************************************************

public class GenericMethod1
{
    public const int VAL1 = 0x50005000;

    private int _i1;

    public GenericMethod1(int val)
    {
        // If this GenericMethod1 instance is part of a derived GenericMethod2 object, then `val'
        // will be zero.
        //
        // Otherwise, the `val' argument will always be VAL1.
        _i1 = val;
    }

    public virtual int GetVal<T>()
    {
        // If reached, this function returns either 0 or VAL1 as described above.
        return _i1;
    }
}


public class GenericMethod2 : GenericMethod1
{
    public const int VAL2 = 0x60006000;

    private static ScenarioMonitor s_monitor = null;

    private int _i2;

    public override int GetVal<T>()
    {
        // This is the GetVal<T> implementation for objects of type GenericMethod2.  If this code
        // runs, it will always return VAL2.
        return _i2;
    }


    private GenericMethod1 _obj;
    private volatile GenericMethod1 _objVolatile;
    private static GenericMethod1 s_obj;
    private volatile static GenericMethod1 s_objVolatile;

    private static GenericMethod1[] s_objs;

    static GenericMethod2()
    {
        // Build the s_objs array during class construction.  Bind the GenericMethod1 instance to
        // VAL1 and the GenericMethod2 instance to VAL2.
        s_objs = new GenericMethod1[2];
        s_objs[0] = new GenericMethod1(VAL1);
        s_objs[1] = new GenericMethod2(VAL2);
    }


    public GenericMethod2(int val) : base(0)
    {
        // The `val' argument will always be VAL2.
        _i2 = val;

        _obj = s_objs[0];
        _objVolatile = s_objs[1];
        s_obj = s_objs[0];
        s_objVolatile = s_objs[1];
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static public GenericMethod1 GetObject()
    {
        return s_obj;
    }


    public static void CheckVal(int val)
    {
        if (val != VAL1 && val != VAL2)
        {
            throw new Exception("Bad value " + val);
        }
    }


    /// <summary>
    ///
    /// The idea is to catch cases where the `this' value Y passed to GetVal<string> is different
    /// from the object X that was used when resolving the GetVal<string> function pointer.  There
    /// are four possibilities:
    ///
    ///     (X is GenericMethod1), (Y is GenericMethod1)
    ///
    ///         GenericMethod1.GetVal<string> runs.  The function call returns VAL1.
    ///
    ///     (X is GenericMethod2), (Y is GenericMethod2)
    ///
    ///         GenericMethod2.GetVal<string> runs.  The function call returns VAL2.
    ///
    ///     (X is GenericMethod1), (Y is GenericMethod2)
    ///
    ///         GenericMethod1.GetVal<string> runs.  It assumes that `this' is a GenericMethod1 instance
    ///         and loads this.m_i1.  In reality, `this' is a GenericMethod2 instance.
    ///
    ///         Since GenericMethod2 is a subclass of GenericMethod1, this loads the m_i1 field from the
    ///         GenericMethod1 subobject.  Since GenericMethod2 always passes 0 to the base class
    ///         constructor, m_i1 is set to 0 in this case.
    ///
    ///         As a result, the function call returns 0.
    ///
    ///     (X is GenericMethod2), (Y is GenericMethod1)
    ///
    ///         GenericMethod2.GetVal<string> runs.  It assumes that `this' is a GenericMethod2 instance
    ///         and loads this.m_i2.  In reality, `this' is a GenericMethod1 instance.
    ///
    ///         The m_i2 field in GenericMethod2 lies beyond all of the fields in the GenericMethod1
    ///         subobject.  Therefore, loading m_i2 will almost certainly load a field that lies off the
    ///         end of the object.
    ///
    ///         As a result, the function call might AV but will usually return an unpredictable chunk
    ///         of data loaded from off the end of the GenericMethod1 instance.
    ///
    /// </summary>
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CheckObjects()
    {
        int val = 0;
        ulong i;

        try
        {
            for (i = 0; GenericMethod2.s_monitor.ScenarioShouldContinueRunning(); i++)
            {
                GenericMethod2.s_monitor.RecordCheckIteration();

                // Based on the current iteration number, choose one of the instance or static member
                // objects and call into that object's implementation of the virtual GetVal<string> method.

                switch (i % 5)
                {
                    case 0:
                        val = _obj.GetVal<string>();
                        break;

                    case 1:
                        val = _objVolatile.GetVal<string>();
                        break;

                    case 2:
                        val = s_obj.GetVal<string>();
                        break;

                    case 3:
                        val = s_objVolatile.GetVal<string>();
                        break;

                    case 4:
                        val = GenericMethod2.GetObject().GetVal<string>();
                        break;

                    default:
                        throw new Exception("We should never get here");
                }


                //
                // Check the basic consistency of the int value observed during this iteration.
                //
                // As described above, if either of the race condition cases occur the observed value will
                // be neither VAL1 nor VAL2.  Therefore, races will be detected and promoted to exceptions
                // at this point.
                //

                GenericMethod2.CheckVal(val);
            }
        }
        catch (Exception ex)
        {
            Mutate.RecordFailure(ex);
        }

        return;
    }


    private void Flip()
    {
        for (uint i = 0; GenericMethod2.s_monitor.ScenarioShouldContinueRunning(); i++)
        {
            GenericMethod2.s_monitor.RecordFlipIteration();

            // Rotate the current (i % 2) object into the instance member and static member variable
            // sets.

            _objVolatile = _obj;
            _obj = s_objs[i % 2];

            s_objVolatile = s_obj;
            s_obj = s_objs[i % 2];
        }
    }


    public static void Test()
    {
        GenericMethod2 o = new GenericMethod2(VAL2);
        GenericMethod2.s_monitor = new ScenarioMonitor("GenericMethod", ScenarioKind.WithNonThrowingChecks);
        GenericMethod2.s_monitor.RunScenario(o.CheckObjects, o.Flip);
        return;
    }
}


//************************************************************************

public class Mutate
{
    private static object s_syncRoot = new object();
    private static volatile List<Exception> s_exceptions = new List<Exception>();

    internal static void RecordFailure(Exception ex)
    {
        lock (Mutate.s_syncRoot)
        {
            s_exceptions.Add(ex);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            CastClass.Test();
            Unbox.Test();
            GenericMethod2.Test();

            if (s_exceptions.Count > 0)
            {
                Console.WriteLine("{0} exceptions were thrown", s_exceptions.Count);

                foreach (Exception ex in s_exceptions)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine();
                    Console.WriteLine();
                }

                throw new Exception("Failure");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILED");
            return 101;
        }

        Console.WriteLine("Test SUCCESS");
        return 100;
    }
}
