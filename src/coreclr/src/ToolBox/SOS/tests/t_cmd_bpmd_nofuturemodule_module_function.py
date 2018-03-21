# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

import lldb
import re
import testutils as test

# bpmd -nofuturemodule <module name> <managed function name>


def runScenario(assembly, debugger, target):
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()
    ci = debugger.GetCommandInterpreter()

    # Process must be stopped here while libcoreclr loading.
    # This test usually fails on release version of coreclr
    # since we depend on 'LoadLibraryExW' symbol present.
    test.assertEqual(process.GetState(), lldb.eStateStopped)

    # The reason of this stop must be a breakpoint
    test.assertEqual(process.GetSelectedThread().GetStopReason(),
                     lldb.eStopReasonBreakpoint)

    ci.HandleCommand("bpmd -nofuturemodule " + assembly + " Test.Main", res)
    out_msg = res.GetOutput()
    err_msg = res.GetError()
    print(out_msg)
    print(err_msg)
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    # Output is not empty
    # Should be at least 'Adding pending breakpoints...'
    test.assertTrue(len(out_msg) == 0)

    # Error message is empty
    test.assertTrue(len(err_msg) == 0)

    process.Continue()
    # Process must be exited because of -nofuturemodule flag
    test.assertEqual(process.GetState(), lldb.eStateExited)

    # The reason of this stop must be a breakpoint
    test.assertEqual(process.GetSelectedThread().GetStopReason(),
                     lldb.eStopReasonNone)

    #

    # Delete all breakpoints, continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
