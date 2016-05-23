using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using System.Text;
using MultiProjectValidator.AnalysisRules.DependencyMismatch;

namespace MultiProjectValidator.AnalysisRules
{
    public class DependencyMismatchRule : IAnalysisRule
    {
        public AnalysisResult Evaluate(IEnumerable<ProjectContext> projectContexts)
        {
            var filteredContexts = FilterContextList(projectContexts);
            var targetGroupedContexts = GroupContextsByTarget(filteredContexts);

            var failureMessages = EvaluateProjectContextTargetGroups(targetGroupedContexts);
            var pass = failureMessages.Count() == 0;

            var result = new AnalysisResult(failureMessages, pass);
            return result;
        }

        private IEnumerable<ProjectContext> FilterContextList(IEnumerable<ProjectContext> projectContexts)
        {
            return projectContexts.Where(context=> !context.TargetIsDesktop());
        }

        private IEnumerable<string> EvaluateProjectContextTargetGroups(Dictionary<string, List<ProjectContext>> targetGroupedProjectContexts)
        {
            var failureMessages = new List<string>();

            foreach (var target in targetGroupedProjectContexts.Keys)
            {
                var targetProjectContextGroup = targetGroupedProjectContexts[target];

                var groupFailureMessages = EvaluateProjectContextTargetGroup(targetProjectContextGroup);

                if (groupFailureMessages.Count > 0)
                {
                    string aggregateFailureMessage = $"Failures for Target {target} {Environment.NewLine}{Environment.NewLine}"
                                                 + string.Join("", groupFailureMessages);

                    failureMessages.Add(aggregateFailureMessage);
                }
            }

            return failureMessages;
        }

        private List<string> EvaluateProjectContextTargetGroup(IEnumerable<ProjectContext> targetProjectContextGroup)
        {
            var failedMessages = new List<string>();

            var dependencyGroups = CreateDependencyGroups(targetProjectContextGroup);

            foreach (var dependencyGroup in dependencyGroups)
            {
                if(dependencyGroup.HasConflict)
                {
                    failedMessages.Add(GetDependencyGroupConflictMessage(dependencyGroup));
                }
            }

            return failedMessages;
        }

        private string GetDependencyGroupConflictMessage(DependencyGroup dependencyGroup)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"Conflict for {dependencyGroup.DependencyName} in projects:{Environment.NewLine}");

            foreach (var version in dependencyGroup.VersionDependencyInfoMap.Keys)
            {
                var dependencyInfoList = dependencyGroup.VersionDependencyInfoMap[version];

                foreach (var dependencyInfo in dependencyInfoList)
                {
                    sb.Append($"Version: {dependencyInfo.Version} Path: {dependencyInfo.ProjectPath} {Environment.NewLine}");
                }
            }
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        private Dictionary<string, List<ProjectContext>> GroupContextsByTarget(IEnumerable<ProjectContext> projectContexts)
        {
            var targetContextMap = new Dictionary<string, List<ProjectContext>>();
            foreach (var context in projectContexts)
            {
                var target = context.TargetFramework + context.RuntimeIdentifier;

                if (targetContextMap.ContainsKey(target))
                {
                    targetContextMap[target].Add(context);
                }
                else
                {
                    targetContextMap[target] = new List<ProjectContext>()
                    {
                        context
                    };
                }
            }

            return targetContextMap;
        }

        private List<DependencyGroup> CreateDependencyGroups(IEnumerable<ProjectContext> projectContexts)
        {
            var libraryNameDependencyGroupMap = new Dictionary<string, DependencyGroup>();

            foreach (var projectContext in projectContexts)
            {
                var libraries = projectContext.LibraryManager.GetLibraries();

                foreach (var library in libraries)
                {
                    var dependencyInfo = DependencyInfo.Create(projectContext, library);

                    if (libraryNameDependencyGroupMap.ContainsKey(dependencyInfo.Name))
                    {
                        var dependencyGroup = libraryNameDependencyGroupMap[dependencyInfo.Name];

                        dependencyGroup.AddEntry(dependencyInfo);
                    }
                    else
                    {
                        var dependencyGroup = DependencyGroup.CreateWithEntry(dependencyInfo);

                        libraryNameDependencyGroupMap[dependencyInfo.Name] = dependencyGroup;
                    }
                }
            }

            return libraryNameDependencyGroupMap.Values.ToList();
        }
    }
}
