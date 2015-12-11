using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;

namespace MultiProjectValidator.AnalysisRules
{
    public class DependencyMismatchRule : IAnalysisRule
    {
        public AnalysisResult Evaluate(List<ProjectContext> projectContexts)
        {
            var targetGroupedContexts = GroupContextsByTarget(projectContexts);

            var failureMessages = EvaluateTargetContextGroups(targetGroupedContexts);
            var pass = failureMessages.Count == 0;

            var result = new AnalysisResult(failureMessages, pass);
            return result;
        }

        private List<string> EvaluateTargetContextGroups(Dictionary<string, List<ProjectContext>> targetGroupedContexts)
        {
            var failureMessages = new List<string>();

            foreach (var target in targetGroupedContexts.Keys)
            {
                var targetContexts = targetGroupedContexts[target];

                failureMessages.AddRange(EvaluateTargetContextGroup(targetContexts));
            }

            return failureMessages;
        }

        private List<string> EvaluateTargetContextGroup(List<ProjectContext> targetContexts)
        {
            var failedMessages = new List<string>();
            var assemblyVersionMap = new Dictionary<string, string>();

            foreach(var context in targetContexts)
            {
                var libraries = context.LibraryManager.GetLibraries();

                foreach(var library in libraries)
                {
                    var name = library.Identity.Name;
                    var version = library.Identity.Version.ToString();

                    if (assemblyVersionMap.ContainsKey(name))
                    {
                        var existingVersion = assemblyVersionMap[name];
                        if (!string.Equals(existingVersion, version, StringComparison.OrdinalIgnoreCase))
                        {
                            string message = 
                                $"Dependency mismatch in {context.ProjectFile.ProjectFilePath} for dependency {name}. Versions {version}, {existingVersion}";

                            failedMessages.Add(message);
                        }
                    }
                    else
                    {
                        assemblyVersionMap[name] = version;
                    }
                }
            }

            return failedMessages;
        }

        private Dictionary<string, List<ProjectContext>> GroupContextsByTarget(List<ProjectContext> projectContexts)
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
    }
}
