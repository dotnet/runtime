using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildTargetResult
    {
        public BuildTarget Target { get; }
        public bool Success { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }

        public BuildTargetResult(BuildTarget target, bool success) : this(target, success, errorMessage: string.Empty) { }

        public BuildTargetResult(BuildTarget target, bool success, Exception exception) : this(target, success, exception.ToString())
        {
            Exception = exception;
        }

        public BuildTargetResult(BuildTarget target, bool success, string errorMessage)
        {
            Target = target;
            Success = success;
            ErrorMessage = errorMessage;
        }

        public void EnsureSuccessful()
        {
            if(!Success)
            {
                if(string.IsNullOrEmpty(ErrorMessage))
                {
                    throw new BuildFailureException(Target, Exception);
                }
                else
                {
                    throw new BuildFailureException(ErrorMessage, Exception, Target);
                }
            }
        }
    }
}