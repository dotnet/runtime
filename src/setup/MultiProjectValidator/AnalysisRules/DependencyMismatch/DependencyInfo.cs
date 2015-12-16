using Microsoft.DotNet.ProjectModel;

namespace ProjectSanity.AnalysisRules.DependencyMismatch
{
    internal class DependencyInfo
    {
        public static DependencyInfo Create(ProjectContext context, LibraryDescription library)
        {
            return new DependencyInfo
            {
                ProjectPath = context.ProjectFile.ProjectFilePath,
                Version = library.Identity.Version.ToString(),
                Name = library.Identity.Name
            };
        }

        public string ProjectPath { get; private set; }
        public string Version { get; private set; }
        public string Name { get; private set; }
    }
}
