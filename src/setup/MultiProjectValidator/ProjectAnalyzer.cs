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

        private List<ProjectContext> _projectContexts;
        private List<IAnalysisRule> _rules;
        
        private ProjectAnalyzer(List<IAnalysisRule> rules, List<ProjectContext> projectContexts)
        {
            _rules = rules;
            _projectContexts = projectContexts;
        }

        public List<AnalysisResult> DoAnalysis()
        {
            var results = new List<AnalysisResult>();

            foreach(var rule in _rules)
            {
                results.Add(rule.Evaluate(_projectContexts));
            }

            return results;
        }

    }
}
