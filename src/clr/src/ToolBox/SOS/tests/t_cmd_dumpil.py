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

    md_addr = test.get_methoddesc(debugger, assembly, "Test.DumpIL")

    ci.HandleCommand("dumpil " + md_addr, res)
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    insts = res.GetOutput()
    print(insts)
    # Function must have some instructions
    test.assertTrue(len(insts) > 0)

    match = re.search(r'IL_\w{4}:\sldstr.*test\sdumpil.*' +
                      r'IL_\w{4}:\scall.*System\.Console::WriteLine.*' +
                      r'IL_\w{4}:\sret',
                      insts.replace('\n', ' '))
    # Must have ldstr, call and ret instructions
    test.assertTrue(match)

    # TODO: test other use cases

    # Continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
