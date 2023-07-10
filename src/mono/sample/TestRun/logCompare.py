import sys
import os
import argparse
import re

def printFunc(data):
    stack = data[7][1:-1]
    action = data[13]
    signature = data[19][2:-1]
    print(stack + ' ' + action + ': ' + signature + ('' if action != 'ENTER' else ' (' + data[20][:-1]))
    pass

def prinfDifference(baseline, logfile, base_linenum, log_linenum):
    print('Got difference:')
    print('Base line '+str(base_linenum)+':\t', end='')
    printFunc(baseline)
    print('Log file '+str(log_linenum)+':\t', end='')
    printFunc(logfile)
    pass

def compareTrace(baseline, logfile):
    baseline_count = 1
    logfile_count = 1
    baseline_line = baseline.readline()
    logfile_line = logfile.readline()

    tracePattern = re.compile(r'(\[0x\w{10})|( [0-9]+])|(LEAVE|ENTER)|(c .*\)\()')
    last_op = ''
    while baseline_line and logfile_line:
        baseline_data = tracePattern.split(baseline_line)
        logfile_data = tracePattern.split(logfile_line)
        getDifference = True

        if len(logfile_data) == 21 and len(baseline_data) == 21:
            getDifference = False
            # compare stack num
            getDifference = getDifference or (baseline_data[7] != logfile_data[7])

            #compare function action (ENTER or LEAVE)
            getDifference = getDifference or (baseline_data[13] != logfile_data[13])

            #compare function signature
            getDifference = getDifference or (baseline_data[19] != logfile_data[19])

            #compare function param
            # if baseline_data[13] == 'ENTER':
            #     getDifference = getDifference or (baseline_data[20] != logfile_data[20])
            if getDifference:
                prinfDifference(baseline_data, logfile_data, baseline_count, logfile_count)
        if len(logfile_data) != 21:
            print("Unhandled logfile_line line\t"+str(logfile_count)+": "+ logfile_line)
        if len(baseline_data) != 21:
            print("Unhandled baseline_line line\t"+str(baseline_count)+": "+ baseline_line)

        if getDifference:
            op = input('Please input op\n')
            if op == '':
                op = last_op
            else:
                last_op = op

            match op:
                case 'q':
                    break
                case 'nb':
                    baseline_count+=1
                    baseline_line = baseline.readline()
                case 'nl':
                    logfile_count+=1
                    logfile_line = logfile.readline()
                case 'n':
                    baseline_count+=1
                    baseline_line = baseline.readline()
                    logfile_count+=1
                    logfile_line = logfile.readline()
        else:
            baseline_count+=1
            baseline_line = baseline.readline()
            logfile_count+=1
            logfile_line = logfile.readline()
    pass

if __name__=='__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument("--base",'-b', help="Baseline log file", required=True)
    parser.add_argument("logfile")
    arg = parser.parse_args()

    with open(arg.base, 'r') as baseline:
        with open(arg.logfile, 'r') as logfile:
            compareTrace(baseline, logfile)