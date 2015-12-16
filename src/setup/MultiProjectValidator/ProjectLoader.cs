using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;

namespace MultiProjectValidator
{
    public class ProjectLoader
    {
        private static readonly string PROJECT_FILENAME = "project.json";

        public static List<ProjectContext> Load(string rootPath, bool recursive=true)
        {
            var projectFiles = DiscoverProjectFiles(rootPath);
            var projectContextList = LoadProjectContexts(projectFiles);

            return projectContextList;
        }

        private static string[] DiscoverProjectFiles(string rootPath)
        {
            return Directory.GetFiles(rootPath, PROJECT_FILENAME, SearchOption.AllDirectories);
        }

        private static List<ProjectContext> LoadProjectContexts(string[] projectFiles)
        {
            var projectContexts = new List<ProjectContext>();

            foreach (var file in projectFiles)
            {
                var fileTargetContexts = ProjectContext.CreateContextForEachTarget(file);

                projectContexts.AddRange(fileTargetContexts);    
            }

            return projectContexts;
        }
    }
}
