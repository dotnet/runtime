// Import the utility functionality.

import jobs.generation.JobReport;
import jobs.generation.Utilities;
import org.dotnet.ci.pipelines.Pipeline

// The input project name (e.g. dotnet/corefx)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName

// **************************
// Define innerloop testing. Any configuration in ForPR will run for every PR but all other configurations
// will have a trigger that can be
// **************************

def linPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/linux.groovy')
def centos6Pipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/centos.6.groovy')
def alpine36Pipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/alpine.3.6.groovy')
def osxPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/osx.groovy')
def winPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/windows.groovy')

def configurations = [
    ['TGroup':"netcoreapp", 'Pipeline':linPipeline, 'Name':'Linux' ,'ForPR':"Release-x64", 'Arch':['x64']],
    ['TGroup':"netcoreapp", 'Pipeline':centos6Pipeline, 'Name':'CentOS.6' ,'ForPR':"", 'Arch':['x64']],
    ['TGroup':"netcoreapp", 'Pipeline':alpine36Pipeline, 'Name':'Alpine.3.6' ,'ForPR':"Debug-x64", 'Arch':['x64']],
    ['TGroup':"netcoreapp", 'Pipeline':osxPipeline, 'Name':'OSX', 'ForPR':"Debug-x64", 'Arch':['x64']],
    ['TGroup':"netcoreapp", 'Pipeline':winPipeline, 'Name':'Windows' , 'ForPR':"Debug-x64|Release-x86"],
    ['TGroup':"netfx",      'Pipeline':winPipeline, 'Name':'NETFX', 'ForPR':"Release-x86"],
    ['TGroup':"uap",        'Pipeline':winPipeline, 'Name':'UWP CoreCLR', 'ForPR':"Debug-x64"],
    ['TGroup':"uapaot",     'Pipeline':winPipeline, 'Name':'UWP NETNative', 'ForPR':"Release-x86"],
    ['TGroup':"all",        'Pipeline':winPipeline, 'Name':'Packaging All Configurations', 'ForPR':"Debug-x64"],
]

configurations.each { config ->
 ['Debug', 'Release'].each { configurationGroup ->
  (config.Arch ?: ['x64', 'x86']).each { archGroup ->
    def triggerName = "${config.Name} ${archGroup} ${configurationGroup} Build"

    def pipeline = config.Pipeline
    def params = ['TGroup':config.TGroup,
                  'CGroup':configurationGroup,
                  'AGroup':archGroup,
                  'TestOuter': false]

    // Add default PR triggers for particular configurations but manual triggers for all
    if (config.ForPR.contains("${configurationGroup}-${archGroup}")) {
        pipeline.triggerPipelineOnEveryGithubPR(triggerName, params)
    }
    else {
        pipeline.triggerPipelineOnGithubPRComment(triggerName, params)
    }

    // Add trigger for all configurations to run on merge
    pipeline.triggerPipelineOnGithubPush(params)

    // Add optional PR trigger for Outerloop test runs
    params.TestOuter = true
    pipeline.triggerPipelineOnGithubPRComment("Outerloop ${triggerName}", params)
}}}

JobReport.Report.generateJobReport(out)

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
