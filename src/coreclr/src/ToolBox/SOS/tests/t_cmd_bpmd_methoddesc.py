import lldb
import re
import testutils as test

# bpmd -md <MethodDesc pointer>


def runScenario(assembly, debugger, target):
    process = target.GetProcess()
    res = lldb.SBCommandReturnObject()
    ci = debugger.GetCommandInterpreter()

    # Run debugger, wait until libcoreclr is loaded,
    # set breakpoint at Test.Main and stop there
    test.stop_in_main(debugger, assembly)

    md_addr = test.get_methoddesc(debugger, assembly, "Test.UnlikelyInlined")

    ci.HandleCommand("bpmd -md %s" % md_addr, res)
    out_msg = res.GetOutput()
    err_msg = res.GetError()
    print(out_msg)
    print(err_msg)
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    # Output is not empty
    # Should be at least 'Adding pending breakpoints...'
    test.assertTrue(len(out_msg) > 0)

    # Error message is empty
    test.assertTrue(len(err_msg) == 0)

    process.Continue()
    # Process must be stopped at UnlinkelyInlined
    test.assertEqual(process.GetState(), lldb.eStateStopped)

    # The reason of this stop must be a breakpoint
    test.assertEqual(process.GetSelectedThread().GetStopReason(),
                     lldb.eStopReasonBreakpoint)

    #

    # Continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
