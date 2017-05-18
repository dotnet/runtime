// Import the utility functionality.

import jobs.generation.JobReport;
import org.dotnet.ci.pipelines.Pipeline

// The input project name (e.g. dotnet/corefx)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName

// **************************
// Define innerloop testing.  These jobs run on every merge and a subset of them run on every PR, the ones
// that don't run per PR can be requested via a magic phrase.
// **************************
def linuxPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/portable-linux.groovy')

['netcoreapp'].each { targetGroup ->
	['Debug', 'Release'].each { configurationGroup ->
		['Linux x64'].each { osName ->
            def parameters = ['Config':configurationGroup, 'OuterLoop':false]
            linuxPipeline.triggerPipelineOnGithubPRComment("${osName} ${configurationGroup} Build", "(?i).*test\\W+portable\\W+linux\\W+${configurationGroup}\\W+pipeline.*", parameters)
		}
	}
}

// Create a pipeline for portable windows
def windowsPipeline = Pipeline.createPipelineForGithub(this, project, branch, 'buildpipeline/portable-windows.groovy')
['netcoreapp'].each { targetGroup ->
	['Debug', 'Release'].each { configurationGroup ->
		['Windows x64'].each { osName ->
            def parameters = ['Config':configurationGroup, 'OuterLoop':false]
            windowsPipeline.triggerPipelineOnGithubPRComment("${osName} ${configurationGroup} Build", "(?i).*test\\W+portable\\W+windows\\W+${configurationGroup}\\W+pipeline.*", parameters)
		}
	}
}

JobReport.Report.generateJobReport(out)