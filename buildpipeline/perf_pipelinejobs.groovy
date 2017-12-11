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

def perfPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/perf-pipeline.groovy')

def triggerName = "Perf Build and Test"
def pipeline = perfPipeline

// If we were using parameters for the pipeline job, we would define an array of parameter pairs
// and pass that array as a parameter to the trigger functions. Ie:
// def params = ['CGroup':'Release',
//               'AGroup':'x64',
//               'OGroup':'Windows_NT']
// pipeline.triggerPipelinOnGithubPRComment(triggerName, params)

params = ['XUNIT_PERFORMANCE_MAX_ITERATION':'6',
          'XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED':'6']

// Allow PR jobs on request.
pipeline.triggerPipelineOnGithubPRComment(triggerName, params)

// Disable automatic PR runs until throughput issues are addressed.
// pipeline.triggerPipelineOnEveryGithubPR(triggerName, params)
pipeline.triggerPipelinePeriodically('*/12 * * * *', params)
