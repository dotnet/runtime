using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildFailureException : Exception
    {
        public BuildTarget Target { get; }

        public BuildFailureException()
        {
        }

        public BuildFailureException(BuildTarget target) : base($"The '{target.Name}' target failed")
        {
            Target = target;
        }

        public BuildFailureException(BuildTarget target, Exception innerException) : base($"The '{target.Name}' target failed", innerException)
        {
            Target = target;
        }

        public BuildFailureException(string message) : base(message)
        {
        }

        public BuildFailureException(string message, BuildTarget target) : base(message)
        {
            Target = target;
        }

        public BuildFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BuildFailureException(string message, Exception innerException, BuildTarget target) : base(message, innerException)
        {
            Target = target;
        }

    }
}