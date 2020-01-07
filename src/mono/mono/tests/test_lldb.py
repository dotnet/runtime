#!/usr/bin/env python3

import os
import sys

import unittest
import lldb

if len (sys.argv) == 1:
    sys.stderr.write ('Usage: test_lldb.py <mono executable>\n')
    sys.exit (1)
mono_exe = sys.argv [1]
test_exe = 'test-lldb.exe'

class TestLldb(unittest.TestCase):

    def setUp (self):
        self.dbg = lldb.SBDebugger.Create ()

        self.dbg.SetAsync (False)
        self.target = self.dbg.CreateTargetWithFileAndArch (mono_exe, lldb.LLDB_ARCH_DEFAULT)
        self.process = None
        #self.dbg.HandleCommand ('log enable lldb jit')

    def tearDown (self):
        if self.process != None:
            self.process.Kill ()

    def test_stacktraces (self):
        bp = self.target.BreakpointCreateByName ('ves_icall_System_Threading_Thread_Sleep_internal')
        self.assertEqual (bp.GetNumLocations (), 1)

        process = self.target.LaunchSimple (['--debug', test_exe], ['MONO_LLDB=1'], os.getcwd())
        self.process = process
        self.assertNotEqual (process, None)

        state = process.GetState ()
        self.assertEqual (state, lldb.eStateStopped)

        # Stopped in the Sleep icall
        findex = 0
        thread = process.GetThreadAtIndex (0)
        frame = thread.GetFrameAtIndex (findex)
        name = frame.GetSymbol().GetName ()
        self.assertEqual (name, 'ves_icall_System_Threading_Thread_Sleep_internal')
        findex += 1

        frame = thread.GetFrameAtIndex (findex)
        name = frame.GetSymbol().GetName ()
        if name == 'ves_icall_System_Threading_Thread_Sleep_internal':
            # inlined
            findex += 1
            frame = thread.GetFrameAtIndex (findex)
            name = frame.GetSymbol().GetName ()
        self.assertEqual (name, '(wrapper managed-to-native) System.Threading.Thread:SleepInternal (int)')

        findex += 1
        frame = thread.GetFrameAtIndex (findex)
        name = frame.GetSymbol().GetName ()
        self.assertEqual (name, 'System.Threading.Thread:Sleep (int)')
        self.assertTrue (str (frame.GetLineEntry ().GetFileSpec()).find ('thread.cs') != -1)
        findex += 1

        frame = thread.GetFrameAtIndex (findex)
        name = frame.GetSymbol().GetName ()
        self.assertEqual (name, 'Tests:Main ()')
        self.assertTrue (str (frame.GetLineEntry ().GetFileSpec()).find ('test-lldb.cs') != -1)

    def test_breakpoints (self):
        bp = self.target.BreakpointCreateByLocation ('test-lldb.cs', 9)

        process = self.target.LaunchSimple (['--debug', test_exe], ['MONO_LLDB=1'], os.getcwd())
        self.process = process
        self.assertNotEqual (process, None)

        state = process.GetState ()
        self.assertEqual (state, lldb.eStateStopped)
        # Stopped in foo ()
        thread = process.GetThreadAtIndex (0)
        frame = thread.GetFrameAtIndex (0)
        name = frame.GetSymbol().GetName ()
        self.assertEqual (name, 'Tests:Main ()')

if __name__ == '__main__':
    suite = unittest.TestLoader().loadTestsFromTestCase(TestLldb)
    unittest.TextTestRunner(verbosity=2).run(suite)

