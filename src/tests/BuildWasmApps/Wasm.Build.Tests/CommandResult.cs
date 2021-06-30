using System;
using System.Diagnostics;
using System.Text;
using Xunit.Sdk;

namespace Wasm.Build.Tests
{
    // taken from https://github.com/dotnet/arcade/blob/main/src/Common/Microsoft.Arcade.Common/CommandResult.cs
    public struct CommandResult
    {
        public static readonly CommandResult Empty = new CommandResult();

        public ProcessStartInfo StartInfo { get; }
        public int ExitCode { get; }
        public string Output { get; }

        public CommandResult(ProcessStartInfo startInfo, int exitCode, string output)
        {
            StartInfo = startInfo;
            ExitCode = exitCode;
            Output = output;
        }

        public void EnsureSuccessful(string messagePrefix = "", bool suppressOutput = false)
        {
            if (ExitCode != 0)
            {
                StringBuilder message = new StringBuilder($"{messagePrefix} Command failed with exit code {ExitCode}: {StartInfo.FileName} {StartInfo.Arguments}");

                if (!suppressOutput)
                {
                    if (!string.IsNullOrEmpty(Output))
                    {
                        message.AppendLine($"{Environment.NewLine}Standard Output:{Environment.NewLine}{Output}");
                    }
                }

                throw new XunitException(message.ToString());
            }
        }
    }
}
