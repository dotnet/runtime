using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;

namespace MultiProjectValidator
{
    public interface IAnalysisRule
    {
        AnalysisResult Evaluate(List<ProjectContext> projectContexts);
    }
}
