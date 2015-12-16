using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;

namespace MultiProjectValidator
{
    public interface IAnalysisRule
    {
        AnalysisResult Evaluate(List<ProjectContext> projectContexts);
    }
}
