// Import the utility functionality.

import jobs.generation.*;

// The input project name (e.g. dotnet/corefx)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName
// Folder that the project jobs reside in (project/branch)
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

// Globals

// Map of os -> osGroup.
def osGroupMap = ['Ubuntu14.04':'Linux',
                  'Ubuntu16.04':'Linux',
                  'Debian8.4':'Linux',
                  'OSX':'OSX',
                  'Windows_NT':'Windows_NT',
                  'CentOS7.1': 'Linux',
                  'OpenSUSE13.2': 'Linux',
                  'RHEL7.2': 'Linux',
                  'LinuxARMEmulator': 'Linux']

// Map of os -> nuget runtime
def targetNugetRuntimeMap = ['OSX' : 'osx.10.10-x64',
                             'Ubuntu14.04' : 'ubuntu.14.04-x64',
                             'Ubuntu16.04' : 'ubuntu.16.04-x64',
                             'Debian8.4' : 'debian.8-x64',
                             'CentOS7.1' : 'centos.7-x64',
                             'OpenSUSE13.2' : 'opensuse.13.2-x64',
                             'RHEL7.2': 'rhel.7-x64']

def osShortName = ['Windows 10': 'win10',
                   'Windows 7' : 'win7',
                   'Windows_NT' : 'windows_nt',
                   'Ubuntu14.04' : 'ubuntu14.04',
                   'OSX' : 'osx',
                   'Windows Nano 2016' : 'winnano16',
                   'Ubuntu16.04' : 'ubuntu16.04',
                   'CentOS7.1' : 'centos7.1',
                   'Debian8.4' : 'debian8.4',
                   'OpenSUSE13.2' : 'opensuse13.2',
                   'RHEL7.2' : 'rhel7.2']


// **************************
// Define perf testing.  These tests should be run daily, and will run on Jenkins not in Helix
// **************************
[true, false].each { isPR ->
    ['Release'].each { configurationGroup ->
        ['Windows_NT', 'Ubuntu16.04'].each { os ->
            def osGroup = osGroupMap[os]
            def newJobName = "perf_${os.toLowerCase()}_${configurationGroup.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                if (os == 'Windows_NT') {
                    label('windows_server_2016_clr_perf')
                }
                else {
                    label('ubuntu_1604_clr_perf')
                }

                wrappers {
                    credentialsBinding {
                        string('BV_UPLOAD_SAS_TOKEN', 'CoreFX Perf BenchView Sas')
                    }
                }

                if (isPR)
                {
                    parameters
                    {
                        stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
                    }
                }

                parameters {
                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enought to get a good sample')
                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enought to get a good sample')
                }

                def configuration = 'Release'
                def runType = isPR ? 'private' : 'rolling'

                if (os == 'Windows_NT') {
                    def benchViewName = isPR ? 'corefx private %BenchviewCommitName%' : 'corefx rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'
                    steps {
                        //We need to specify the max cpu count to be one as we do not want to be executing performance tests in parallel
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -release")
                        batchFile("C:\\Tools\\nuget.exe install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\\Tools\" -Prerelease -ExcludeVersion")
                        //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                        //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                        batchFile("if [%GIT_BRANCH:~0,7%] == [origin/] (set GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%) else (set GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%)\n" +
                        "py \"%WORKSPACE%\\Tools\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name " + "\"" + benchViewName + "\"" + " --user-email " + "\"dotnet-bot@microsoft.com\"\n" +
                        "py \"%WORKSPACE%\\Tools\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type " + runType)
                        batchFile("py \"%WORKSPACE%\\Tools\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build-managed.cmd -release -tests -- /p:Performance=true /p:TargetOS=${osGroup} /m:1 /p:LogToBenchview=true /p:BenchviewRunType=${runType}")
                    }
                }
                else {
                    def benchViewName = isPR ? 'corefx private \$BenchviewCommitName' : 'corefx rolling \$GIT_BRANCH_WITHOUT_ORIGIN \$GIT_COMMIT'
                    steps {
                        //We need to specify the max cpu count to be one as we do not want to be executing performance tests in parallel
                        shell("./build.sh -release")
                        shell("find . -type f -name dotnet | xargs chmod u+x")
                        shell("curl \"http://benchviewtestfeed.azurewebsites.net/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'\" | grep \"content type\" | sed \"\$ s/.*src=\\\"\\([^\\\"]*\\)\\\".*/\\1/;tx;d;:x\" | xargs curl -o benchview.zip")
                        shell("unzip -q -o benchview.zip -d \"\${WORKSPACE}/Tools/Microsoft.BenchView.JSONFormat\"")

                        //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                        //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                        shell("GIT_BRANCH_WITHOUT_ORIGIN=\$(echo \$GIT_BRANCH | sed \"s/[^/]*\\/\\(.*\\)/\\1 /\")\n" +
                        "python3.5 \"\${WORKSPACE}/Tools/Microsoft.BenchView.JSONFormat/tools/submission-metadata.py\" --name " + "\"" + benchViewName + "\"" + " --user-email " + "\"dotnet-bot@microsoft.com\"\n" +
                        "python3.5 \"\${WORKSPACE}/Tools/Microsoft.BenchView.JSONFormat/tools/build.py\" git --branch \$GIT_BRANCH_WITHOUT_ORIGIN --type " + runType)
                        shell("python3.5 \"\${WORKSPACE}/Tools/Microsoft.BenchView.JSONFormat/tools/machinedata.py\"")
                        shell("bash ./build-managed.sh -release -tests -- /p:Performance=true /p:TargetOS=${osGroup} /m:1 /p:LogToBenchview=true /p:BenchviewRunType=${runType}")
                    }
                }
            }

            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            //Set timeout to non-default
            newJob.with {
                wrappers {
                    timeout {
                        absolute(240)
                    }
                }
            }
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/**/Perf-*.xml')
            def archiveContents = "msbuild.log"

            // Add archival for the built data.
            Utilities.addArchival(newJob, archiveContents)
            // Set up triggers
            if (isPR) {
                TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                builder.setGithubContext("${os} Perf Tests")
                builder.triggerOnlyOnComment()
                builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+perf.*")
                builder.triggerForBranch(branch)
                builder.emitTrigger(newJob)
            }
            else {
                // Set a push trigger
                TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
                builder.emitTrigger(newJob)
            }
        }
    }
}