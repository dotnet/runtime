# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

import lldb
import re
import testutils as test


def runScenario(assembly, debugger, target):
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()
    ci = debugger.GetCommandInterpreter()

    # Run debugger, wait until libcoreclr is loaded,
    # set breakpoint at Test.Main and stop there
    test.stop_in_main(debugger, assembly)

    ci.HandleCommand("histclear", res)
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    output = res.GetOutput()
    # Output is not empty
    test.assertTrue(len(output) > 0)

    # Specific string must be in the output
    test.assertNotEqual(output.find("Completed successfully."), -1)

    # TODO: test other use cases

    # Continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
