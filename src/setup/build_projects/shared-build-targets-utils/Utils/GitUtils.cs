using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public static class GitUtils
    {
        public static int GetCommitCount()
        {
            return int.Parse(ExecuteGitCommand("rev-list", "--count", "HEAD"));
        }

        public static string GetCommitHash()
        {
            return ExecuteGitCommand("rev-parse", "HEAD");
        }

        private static string ExecuteGitCommand(params string[] args)
        {
            var gitResult = Cmd("git", args)
                .CaptureStdOut()
                .Execute();
            gitResult.EnsureSuccessful();

            return gitResult.StdOut.Trim();
        }
    }
}
