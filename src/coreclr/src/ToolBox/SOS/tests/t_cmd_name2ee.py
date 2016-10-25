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

    ci.HandleCommand("name2ee " + assembly + " Test.Main", res)
    print(res.GetOutput())
    print(res.GetError())
    # Interpreter must have this command and able to run it
    test.assertTrue(res.Succeeded())

    output = res.GetOutput()
    # Output is not empty
    test.assertTrue(len(output) > 0)

    match = re.search('Module:\s+[0-9a-fA-F]+', output)
    test.assertTrue(match)
    match = re.search('Assembly:\s+\S+', output)
    test.assertTrue(match)
    match = re.search('Token:\s+[0-9a-fA-F]+', output)
    test.assertTrue(match)
    match = re.search('MethodDesc:\s+[0-9a-fA-F]+', output)
    test.assertTrue(match)
    match = re.search('Name:\s+\S+', output)
    test.assertTrue(match)

    process.Continue()
    # Process must exit
    test.assertEqual(process.GetState(), lldb.eStateExited)

    # Process must exit with zero code
    test.assertEqual(process.GetExitStatus(), 0)

    # TODO: test other use cases

    # Continue current process and checks its exit code
    test.exit_lldb(debugger, assembly)
