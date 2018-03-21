# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import print_function
import unittest
import argparse
import re
import tempfile
import subprocess
import threading
import os
import sys
import inspect

lldb = ''
clrdir = ''
workdir = ''
corerun = ''
sosplugin = ''
assembly = ''
fail_flag = ''
fail_flag_lldb = ''
summary_file = ''
timeout = 0
regex = ''
repeat = 0


def runWithTimeout(cmd):
    p = None

    def run():
        global p
        p = subprocess.Popen(cmd, shell=True)
        p.communicate()

    thread = threading.Thread(target=run)
    thread.start()

    thread.join(timeout)
    if thread.is_alive():
        with open(summary_file, 'a+') as summary:
            print('Timeout!', file=summary)
        p.kill()
        thread.join()


class TestSosCommands(unittest.TestCase):

    def do_test(self, command):
        open(fail_flag, 'a').close()
        try:
            os.unlink(fail_flag_lldb)
        except:
            pass

        cmd = (('%s -b ' % lldb) +
               ("-k \"script open('%s', 'a').close()\" " % fail_flag_lldb) +
               ("-k 'quit' ") +
               ("--no-lldbinit ") +
               ("-O \"plugin load %s \" " % sosplugin) +
               ("-o \"script import testutils as test\" ") +
               ("-o \"script test.fail_flag = '%s'\" " % fail_flag) +
               ("-o \"script test.summary_file = '%s'\" " % summary_file) +
               ("-o \"script test.run('%s', '%s')\" " % (assembly, command)) +
               ("-o \"quit\" ") +
               (" -- %s %s > %s.log 2> %s.log.2" % (corerun, assembly,
                                                    command, command)))

        runWithTimeout(cmd)
        self.assertFalse(os.path.isfile(fail_flag))
        self.assertFalse(os.path.isfile(fail_flag_lldb))

        try:
            os.unlink(fail_flag)
        except:
            pass
        try:
            os.unlink(fail_flag_lldb)
        except:
            pass

    def t_cmd_bpmd_nofuturemodule_module_function(self):
        self.do_test('t_cmd_bpmd_nofuturemodule_module_function')

    def t_cmd_bpmd_module_function(self):
        self.do_test('t_cmd_bpmd_module_function')

    def t_cmd_bpmd_module_function_iloffset(self):
        self.do_test('t_cmd_bpmd_module_function_iloffset')

    def t_cmd_bpmd_methoddesc(self):
        self.do_test('t_cmd_bpmd_methoddesc')

    def t_cmd_bpmd_clearall(self):
        self.do_test('t_cmd_bpmd_clearall')

    def t_cmd_clrstack(self):
        self.do_test('t_cmd_clrstack')

    def t_cmd_clrthreads(self):
        self.do_test('t_cmd_clrthreads')

    def t_cmd_clru(self):
        self.do_test('t_cmd_clru')

    def t_cmd_dumpclass(self):
        self.do_test('t_cmd_dumpclass')

    def t_cmd_dumpheap(self):
        self.do_test('t_cmd_dumpheap')

    def t_cmd_dumpil(self):
        self.do_test('t_cmd_dumpil')

    def t_cmd_dumplog(self):
        self.do_test('t_cmd_dumplog')

    def t_cmd_dumpmd(self):
        self.do_test('t_cmd_dumpmd')

    def t_cmd_dumpmodule(self):
        self.do_test('t_cmd_dumpmodule')

    def t_cmd_dumpmt(self):
        self.do_test('t_cmd_dumpmt')

    def t_cmd_dumpobj(self):
        self.do_test('t_cmd_dumpobj')

    def t_cmd_dumpstack(self):
        self.do_test('t_cmd_dumpstack')

    def t_cmd_dso(self):
        self.do_test('t_cmd_dso')

    def t_cmd_eeheap(self):
        self.do_test('t_cmd_eeheap')

    def t_cmd_eestack(self):
        self.do_test('t_cmd_eestack')

    def t_cmd_gcroot(self):
        self.do_test('t_cmd_gcroot')

    def t_cmd_ip2md(self):
        self.do_test('t_cmd_ip2md')

    def t_cmd_name2ee(self):
        self.do_test('t_cmd_name2ee')

    def t_cmd_pe(self):
        self.do_test('t_cmd_pe')

    def t_cmd_histclear(self):
        self.do_test('t_cmd_histclear')

    def t_cmd_histinit(self):
        self.do_test('t_cmd_histinit')

    def t_cmd_histobj(self):
        self.do_test('t_cmd_histobj')

    def t_cmd_histobjfind(self):
        self.do_test('t_cmd_histobjfind')

    def t_cmd_histroot(self):
        self.do_test('t_cmd_histroot')

    def t_cmd_sos(self):
        self.do_test('t_cmd_sos')

    def t_cmd_soshelp(self):
        self.do_test('t_cmd_soshelp')


def generate_report():
    report = [{'name': 'TOTAL', True: 0, False: 0, 'completed': True}]
    fail_messages = []

    if not os.path.isfile(summary_file):
        print('No summary file to process!')
        return

    with open(summary_file, 'r') as summary:
        for line in summary:
            if line.startswith('new_suite: '):
                report.append({'name': line.split()[-1], True: 0, False: 0,
                               'completed': False, 'timeout': False})
            elif line.startswith('True'):
                report[-1][True] += 1
            elif line.startswith('False'):
                report[-1][False] += 1
            elif line.startswith('Completed!'):
                report[-1]['completed'] = True
            elif line.startswith('Timeout!'):
                report[-1]['timeout'] = True
            elif line.startswith('!!! '):
                fail_messages.append(line.rstrip('\n'))

    for suite in report[1:]:
        report[0][True] += suite[True]
        report[0][False] += suite[False]
        report[0]['completed'] &= suite['completed']

    for line in fail_messages:
        print(line)

    print()
    print('=' * 79)
    print('{:72} {:6}'.format('Test suite', 'Result'))
    print('-' * 79)
    for suite in report[1:]:
        if suite['timeout']:
            result = 'Timeout'
        elif suite[False]:
            result = 'Fail'
        elif not suite['completed']:
            result = 'Crash'
        elif suite[True]:
            result = 'Success'
        else:
            result = 'Please, report'
        print('{:68} {:>10}'.format(suite['name'], result))
    print('=' * 79)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--lldb', default='lldb')
    parser.add_argument('--clrdir', default='.')
    parser.add_argument('--workdir', default='.')
    parser.add_argument('--assembly', default='Test.exe')
    parser.add_argument('--timeout', default=90)
    parser.add_argument('--regex', default='t_cmd_')
    parser.add_argument('--repeat', default=1)
    parser.add_argument('unittest_args', nargs='*')

    args = parser.parse_args()

    lldb = args.lldb
    clrdir = args.clrdir
    workdir = args.workdir
    assembly = args.assembly
    timeout = int(args.timeout)
    regex = args.regex
    repeat = int(args.repeat)
    print("lldb: %s" % lldb)
    print("clrdir: %s" % clrdir)
    print("workdir: %s" % workdir)
    print("assembly: %s" % assembly)
    print("timeout: %i" % timeout)
    print("regex: %s" % regex)
    print("repeat: %i" % repeat)

    corerun = os.path.join(clrdir, 'corerun')
    sosplugin = os.path.join(clrdir, 'libsosplugin.so')
    if os.name != 'posix':
        print('Not implemented: corerun.exe, sosplugin.dll?')
        exit(1)

    print("corerun: %s" % corerun)
    print("sosplugin: %s" % sosplugin)

    fail_flag = os.path.join(workdir, 'fail_flag')
    fail_flag_lldb = os.path.join(workdir, 'fail_flag.lldb')

    print("fail_flag: %s" % fail_flag)
    print("fail_flag_lldb: %s" % fail_flag_lldb)

    summary_file = os.path.join(workdir, 'summary')
    print("summary_file: %s" % summary_file)

    try:
        os.unlink(summary_file)
    except:
        pass

    sys.argv[1:] = args.unittest_args
    suite = unittest.TestSuite()
    all_tests = inspect.getmembers(TestSosCommands, predicate=inspect.ismethod)
    for (test_name, test_func) in all_tests:
        if re.match(regex, test_name):
            suite.addTest(TestSosCommands(test_name))
    unittest.TextTestRunner(verbosity=1).run(suite)

    generate_report()
