using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel;

namespace MultiProjectValidator
{
    public static class ProjectContextExtensions
    {
        private static readonly string s_desktopTfmPrefix = ".NETFramework";
        public static bool TargetIsDesktop(this ProjectContext context)
        {
            var targetFramework = context.TargetFramework.ToString();

            return targetFramework.StartsWith(s_desktopTfmPrefix);
        }
    }
}
