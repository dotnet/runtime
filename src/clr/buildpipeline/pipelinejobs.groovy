// Import the utility functionality.

import jobs.generation.JobReport;
import jobs.generation.Utilities;
import org.dotnet.ci.pipelines.Pipeline

// The input project name (e.g. dotnet/coreclr)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName

// **************************
// Define innerloop testing. Any configuration in ForPR will run for every PR but all other configurations
// will have a trigger that can be
// **************************

def linmuslPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/linux-musl.groovy')

def configurations = [
    ['TGroup':"netcoreapp", 'Pipeline':linmuslPipeline, 'Name':'Linux-musl' ,'ForPR':"Debug-x64", 'Arch':['x64']],
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
