using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public partial class BuildFailureException : Exception
    {
        public BuildTarget Target { get; }

        public BuildFailureException(BuildTarget target) : base($"The '{target.Name}' target failed")
        {
            Target = target;
        }

        public BuildFailureException(BuildTarget target, Exception innerException) : base($"The '{target.Name}' target failed", innerException)
        {
            Target = target;
        }

        public BuildFailureException(string message, BuildTarget target) : base(message)
        {
            Target = target;
        }

        public BuildFailureException(string message, Exception innerException, BuildTarget target) : base(message, innerException)
        {
            Target = target;
        }
    }
}