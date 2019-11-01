#!/usr/bin/env python3

# This script is a simulation of what perf.groovy does in CI. It is not currently used for any official purpose, but I wanted
# to check it in hopes it might be useful to others.

import ctypes
import json
import argparse
import os
import shutil
import subprocess
import sys
import glob
from sys import version_info

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to simulate CI perf jobs'

parser = argparse.ArgumentParser(description=description)

#job arguments
parser.add_argument('-job', dest='job', default=None, required=True, choices=['CoreCLR-Scenarios'])
parser.add_argument('-isPR', dest='isPR', default=True, choices=[True, False])
parser.add_argument('-arch', dest='arch', default='x64', choices=['x64', 'x86'])
parser.add_argument('-os', dest='operatingSystem', default='Windows_NT', choices=['Windows_NT', 'Ubuntu16.04', 'Ubuntu14.04', 'OSX'])
parser.add_argument('-optLevel', dest='optLevel', default='full_opt', choices=['full_opt', 'min_opt', 'tiered'])
parser.add_argument('-jitName', dest='jitName', default='ryujit', choices=['ryujit'])
parser.add_argument('-benchviewCommitName', dest='benchviewCommitName', default='FAKE_BV_COMMIT_NAME')

#ambient jenkins properties
parser.add_argument('-workspace', dest='workspace', default=os.path.abspath(os.getcwd()))
parser.add_argument('-gitBranchWithoutOrigin', dest='gitBranchWithoutOrigin', default='FAKE_BRANCHNAME')
parser.add_argument('-gitBranch', dest='gitBranch', default='origin/FAKE_BRANCHNAME')
parser.add_argument('-gitCommit', dest='gitCommit', default='FAKE_GIT_COMMIT')



##########################################################################
# Helper Functions
##########################################################################

def log(message):
    """ Print logging information
    Args:
        message (str): message to be printed
    """

    print('[%s]: %s' % (sys.argv[0], message))

def is_supported_version() -> bool:
    """ Checkes that the version of python is at least 3.5
    Returns:
        bool : true if python version is 3.5 or higher
    """
    return version_info.major > 2 and version_info.minor > 4

def run_command(runArgs, environment):
    """ Run command specified by runArgs
    Args:
        runargs (str[]): list of arguments for subprocess
        environment(str{}): dict of environment variable
    """
    log('RUNNING COMMAND: ' + runArgs)
    subprocess.check_call(runArgs, env=environment)

##########################################################################
# CI jobs
##########################################################################

class jenkins_properties:
    def __init__(self, workspace, git_branch_without_origin, git_branch, git_commit):
        self.workspace = workspace
        self.git_branch_without_origin = git_branch_without_origin
        self.git_branch = git_branch
        self.git_commit = git_commit


class coreclr_scenarios_args:
    def __init__(self, is_pr, arch, os, jit_name, opt_level, benchview_commit_name):
        self.is_pr = is_pr
        self.arch = arch
        self.os = os
        self.jit_name = jit_name
        self.opt_level = opt_level
        self.benchview_commit_name = benchview_commit_name

def coreclr_scenarios(args, props):
    architecture = args.arch
    testEnv = ''
    configuration = 'Release'
    runType = 'private' if args.is_pr else 'rolling'
    benchViewName = 'CoreCLR-Scenarios private ' + args.benchview_commit_name if args.is_pr else 'CoreCLR-Scenarios rolling ' + props.git_branch_without_origin + ' ' + props.git_commit
    uploadString = ''
    
    run_command('powershell -NoProfile wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"' + props.workspace + '\\nuget.exe\"', os.environ)
    if os.path.isdir(props.workspace + '\\Microsoft.BenchView.JSONFormat'):
        shutil.rmtree(props.workspace + '\\Microsoft.BenchView.JSONFormat')
    run_command('\"' + props.workspace + '\\nuget.exe\" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"' + props.workspace + '\" -Prerelease -ExcludeVersion', os.environ)
    run_command('py \"' + props.workspace + '\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"' + benchViewName + '\" --user-email \"dotnet-bot@microsoft.com\"', os.environ)
    run_command('py \"' + props.workspace + '\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch ' + props.git_branch_without_origin + ' --type ' + runType, os.environ)
    run_command('py \"' + props.workspace + '\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"', os.environ)
    run_command('cmd.exe /c set __TestIntermediateDir=int&&build.cmd ' + configuration + ' ' + architecture, os.environ)
    run_command('tests\\runtest.cmd ' + configuration + ' ' + architecture + ' GenerateLayoutOnly', os.environ)
    
    runXUnitPerfCommonArgs = '-arch ' + args.arch + ' -configuration ' + configuration + ' -os ' + args.os + ' -generateBenchviewData \"' + props.workspace + '\\Microsoft.Benchview.JSONFormat\\tools\"' + uploadString + ' -runtype ' + runType + ' ' + testEnv + ' -optLevel ' + args.opt_level + ' -jitName ' + args.jit_name + ' -outputdir \"' + props.workspace + '\\bin\\sandbox_logs\" -stabilityPrefix \"START \\\"CORECLR_PERF_RUN\\\" /B /WAIT /HIGH\" -scenarioTest'
    
    # Profile=Off
    run_command('py tests\\scripts\\run-xunit-perf.py ' + runXUnitPerfCommonArgs + ' -testBinLoc bin\\tests\\' + args.os + '.' + architecture + '.' + configuration + '\\performance\\Scenario\\JitBench -group CoreCLR-Scenarios', os.environ)
    
    # Profile=On
    if(args.opt_level != 'min_opt'):
        run_command('py tests\\scripts\\run-xunit-perf.py ' + runXUnitPerfCommonArgs + ' -testBinLoc bin\\tests\\' + args.os + '.' + architecture + '.' + configuration + '\\performance\\Scenario\\JitBench -group CoreCLR-Scenarios -collectionFlags BranchMispredictions+CacheMisses+InstructionRetired', os.environ)
    

##########################################################################
# Main
##########################################################################

def main(args):
    if not is_supported_version():
        log("Python 3.5 or newer is required")
        return 1

    cwd = os.getcwd()
    if(os.path.abspath(args.workspace) != cwd):
        log('ERROR: the working directory must be the same as the workspace')
        return 2
    if(not os.path.isfile(os.path.join(args.workspace, 'perf.groovy'))):
        log('ERROR: the workspace must point to the root of the repo')
        return 3
    if(not ctypes.windll.shell32.IsUserAnAdmin()):
        log('ERROR: this script must be run as an admin because perf jobs use ETW')
        return 4

    log("NOTICE: This script is a simulation of what CI runs, but it is never actually invoked by CI. The real logic for CI currently resides in perf.groovy. " +
    "This script is only provided in hopes it might be useful to repro CI issues outside of CI")
    log("")
    
    log('== ARGUMENTS ==')
    log('job:                       %s' % args.job)

    # Jenkins properties available in all jobs
    log('workspace:                 %s' % args.workspace)
    log('git_branch_without_origin: %s' % args.gitBranchWithoutOrigin)
    log('git_branch:                %s' % args.gitBranch)
    log('git_commit:                %s' % args.gitCommit)
    props = jenkins_properties(args.workspace, args.gitBranchWithoutOrigin, args.gitBranch, args.gitCommit)


    if(args.job == 'CoreCLR-Scenarios'):
        log('isPR:                      %s' % args.isPR)
        log('arch:                      %s' % args.arch)
        log('operatingSystem:           %s' % args.operatingSystem)
        log('jitName:                   %s' % args.jitName)
        log('optLevel:                  %s' % args.optLevel)
        log('benchviewCommitName:       %s' % args.benchviewCommitName)
        log('')
        log('== RUNNING JOB ==')
        job_args = coreclr_scenarios_args(args.isPR, args.arch, args.operatingSystem, args.jitName, args.optLevel, args.benchviewCommitName)
        coreclr_scenarios(job_args, props)

    else:
        log('Unrecognized job argument: ' + args.job)

    return 0



if __name__ == "__main__":
    Args = parser.parse_args(sys.argv[1:])
    sys.exit(main(Args))
