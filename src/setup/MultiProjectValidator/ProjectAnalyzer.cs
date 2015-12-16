using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;
using MultiProjectValidator.AnalysisRules;

namespace MultiProjectValidator
{
    public class ProjectAnalyzer
    {

        public static ProjectAnalyzer Create(List<ProjectContext> projectContexts)
        {
            // Any Additional rules would be added here
            var rules = new List<IAnalysisRule>
            {
                new DependencyMismatchRule()
            };

            return new ProjectAnalyzer(rules, projectContexts);
        }

        private List<ProjectContext> projectContexts;
        private List<IAnalysisRule> rules;
        
        private ProjectAnalyzer(List<IAnalysisRule> rules, List<ProjectContext> projectContexts)
        {
            this.rules = rules;
            this.projectContexts = projectContexts;
        }

        public List<AnalysisResult> DoAnalysis()
        {
            var results = new List<AnalysisResult>();

            foreach(var rule in rules)
            {
                results.Add(rule.Evaluate(projectContexts));
            }

            return results;
        }

    }
}
