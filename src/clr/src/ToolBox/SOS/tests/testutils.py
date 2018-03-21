# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import print_function
import lldb
import re
import inspect
import sys
import os
import importlib

summary_file = ''
fail_flag = ''

failed = False


def assertCommon(passed, fatal):
    global failed
    with open(summary_file, 'a+') as summary:
        print(bool(passed), file=summary)
        if (not passed):
            failed = True
            print('!!! test failed:', file=summary)
            for s in inspect.stack()[2:]:
                print("!!!  %s:%i" % (s[1], s[2]), file=summary)
                print("!!! %s" % s[4][0], file=summary)
                if re.match('\W*t_\w+\.py$', s[1]):
                    break
            print('!!! ', file=summary)

            if fatal:
                exit(1)


def assertTrue(x, fatal=True):
    passed = bool(x)
    assertCommon(passed, fatal)


def assertFalse(x, fatal=True):
    passed = not bool(x)
    assertCommon(passed, fatal)


def assertEqual(x, y, fatal=True):
    passed = (x == y)
    if not passed:
        print(str(x), ' != ', str(y))
    assertCommon(passed, fatal)


def assertNotEqual(x, y, fatal=True):
    passed = (x != y)
    if not passed:
        print(str(x), ' == ', str(y))
    assertCommon(passed, fatal)


def checkResult(res):
    if not res.Succeeded():
        print(res.GetOutput())
        print(res.GetError())
        exit(1)


def is_hexnum(s):
    try:
        int(s, 16)
        return True
    except ValueError:
        return False


def exec_and_find(commandInterpreter, cmd, regexp):
    res = lldb.SBCommandReturnObject()
    commandInterpreter.HandleCommand(cmd, res)
    checkResult(res)

    expr = re.compile(regexp)
    addr = None

    print(res.GetOutput())
    lines = res.GetOutput().splitlines()
    for line in lines:
        match = expr.match(line)
        if match:
            addr = match.group(1)
            break

    print("Found addr: " + str(addr))
    return addr


def stop_in_main(debugger, assembly):
    ci = debugger.GetCommandInterpreter()
    target = debugger.GetSelectedTarget()
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()

    # Process must be stopped here while libcoreclr loading.
    # This test usually fails on release version of coreclr
    # since we depend on 'LoadLibraryExW' symbol present.
    assertEqual(process.GetState(), lldb.eStateStopped)

    # The reason of this stop must be a breakpoint
    assertEqual(process.GetSelectedThread().GetStopReason(),
                lldb.eStopReasonBreakpoint)

    ci.HandleCommand("bpmd " + assembly + " Test.Main", res)
    out_msg = res.GetOutput()
    err_msg = res.GetError()
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    assertTrue(res.Succeeded())

    # Output is not empty
    # Should be at least 'Adding pending breakpoints...'
    assertTrue(len(out_msg) > 0)

    # Error message is empty
    assertTrue(len(err_msg) == 0)

    process.Continue()
    # Process must be stopped here if bpmd works at all
    assertEqual(process.GetState(), lldb.eStateStopped)

    # The reason of this stop must be a breakpoint
    assertEqual(process.GetSelectedThread().GetStopReason(),
                lldb.eStopReasonBreakpoint)


def exit_lldb(debugger, assembly):
    ci = debugger.GetCommandInterpreter()
    target = debugger.GetSelectedTarget()
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()

    ci.HandleCommand("breakpoint delete --force", res)
    out_msg = res.GetOutput()
    err_msg = res.GetError()
    print(out_msg)
    print(err_msg)
    # Interpreter must have this command and able to run it
    # assertTrue(res.Succeeded())

    process.Continue()
    # Process must exit
    assertEqual(process.GetState(), lldb.eStateExited)

    # Process must exit with zero code
    assertEqual(process.GetExitStatus(), 0)


def get_methoddesc(debugger, assembly, funcname):
    ci = debugger.GetCommandInterpreter()
    target = debugger.GetSelectedTarget()
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()

    ci.HandleCommand("name2ee %s %s" % (assembly, funcname), res)
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    assertTrue(res.Succeeded())

    output = res.GetOutput()
    # Output is not empty
    assertTrue(len(output) > 0)

    match = re.search('MethodDesc:\s+([0-9a-fA-F]+)', output)
    # Line matched
    assertTrue(match)

    groups = match.groups()
    # Match has a single subgroup
    assertEqual(len(groups), 1)

    md_addr = groups[0]
    # Address must be a hex number
    assertTrue(is_hexnum(md_addr))

    return md_addr


def run(assembly, module):
    with open(summary_file, 'a+') as summary:
        print('new_suite: %s' % module, file=summary)

    debugger = lldb.debugger

    debugger.SetAsync(False)
    target = lldb.target

    debugger.HandleCommand("breakpoint set --one-shot --name coreclr_execute_assembly")
    debugger.HandleCommand("process launch")

    # run the scenario
    print("starting scenario...")
    i = importlib.import_module(module)
    scenarioResult = i.runScenario(os.path.basename(assembly), debugger,
                                   target)

    if (target.GetProcess().GetExitStatus() == 0) and not failed:
        os.unlink(fail_flag)

    with open(summary_file, 'a+') as summary:
        print('Completed!', file=summary)
