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

    ci.HandleCommand("dso", res)
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    output = res.GetOutput()
    # Output is not empty
    test.assertTrue(len(output) > 0)

    # Get all objects
    objects = []
    for line in output.split('\n'):
        match = re.match('([0-9a-fA-F]+)\s+([0-9a-fA-F]+)\s', line)
        # Not all lines list objects
        if match:
            groups = match.groups()
            # Match has exactly two subgroups
            test.assertEqual(len(groups), 2)

            obj_addr = groups[1]
            # Address must be a hex number
            test.assertTrue(test.is_hexnum(obj_addr))

            objects.append(obj_addr)

    # There must be at least one object
    test.assertTrue(len(objects) > 0)

    for obj in objects:
        ci.HandleCommand("gcroot " + obj, res)
        print(res.GetOutput())
        print(res.GetError())
        # Interpreter must have this command and able to run it
        test.assertTrue(res.Succeeded())

        output = res.GetOutput()
        # Output is not empty
        test.assertTrue(len(output) > 0)

        match = re.search('Found', output)
        test.assertTrue(match)

    # TODO: test other use cases

    # Continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
