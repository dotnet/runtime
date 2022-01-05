// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateRunScript : Task
    {
        [Required]
        public string[] SetCommands { get; set; }

        [Required]
        public string[] RunCommands { get; set; }

        [Required]
        public string TemplatePath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            if (RunCommands.Length == 0)
            {
                Log.LogError("Please provide at least one test command to execute via the RunCommands property.");
                return false;
            }

            if (!File.Exists(TemplatePath))
            {
                Log.LogError($"Runner script template {TemplatePath} was not found.");
                return false;
            }

            string templateContent = File.ReadAllText(TemplatePath);
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            Log.LogMessage($"Run commands = {string.Join(Environment.NewLine, RunCommands)}");

            string extension = Path.GetExtension(Path.GetFileName(OutputPath)).ToLowerInvariant();
            switch (extension)
            {
                case ".sh":
                case ".cmd":
                case ".bat":
                    WriteRunScript(templateContent, extension);
                    break;
                default:
                    Log.LogError($"Generating runner scripts with extension '{extension}' is not supported.");
                    return false;
            }

            return true;
        }

        private void WriteRunScript(string templateContent, string extension)
        {
            bool isUnix = extension == ".sh";
            string lineFeed = isUnix ? "\n" : "\r\n";

            var setCommandsBuilder = new StringBuilder();
            for (int i = 0; i < SetCommands.Length; i++)
            {
                setCommandsBuilder.Append(SetCommands[i]);
                if (i < SetCommands.Length - 1)
                {
                    setCommandsBuilder.Append(lineFeed);
                }
            }
            templateContent = templateContent.Replace("[[SetCommands]]", setCommandsBuilder.ToString());

            var runCommandsBuilder = new StringBuilder();
            for (int i = 0; i < RunCommands.Length; i++)
            {
                runCommandsBuilder.Append(RunCommands[i]);
                if (i < RunCommands.Length - 1)
                {
                    runCommandsBuilder.Append(lineFeed);
                }
            }
            templateContent = templateContent.Replace("[[RunCommands]]", runCommandsBuilder.ToString());


            var setCommandEchoesBuilder = new StringBuilder();
            foreach (string setCommand in SetCommands)
            {
                setCommandEchoesBuilder.Append($"echo {SanitizeEcho(setCommand,isUnix)}{lineFeed}");
            }
            templateContent = templateContent.Replace("[[SetCommandsEcho]]", setCommandEchoesBuilder.ToString());

            var runCommandEchoesBuilder = new StringBuilder();
            foreach (string runCommand in RunCommands)
            {
                runCommandEchoesBuilder.Append($"echo {SanitizeEcho(runCommand,isUnix)}{lineFeed}");
            }
            templateContent = templateContent.Replace("[[RunCommandsEcho]]", runCommandEchoesBuilder.ToString());


            if (isUnix)
            {
                // Just in case any Windows EOLs have made it in by here, clean any up.
                templateContent = templateContent.Replace("\r\n", "\n");
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(OutputPath, FileMode.Create)))
            {
                sw.NewLine = lineFeed;
                sw.Write(templateContent);
                sw.WriteLine();
            }

            Log.LogMessage($"Wrote {extension} run script to {OutputPath}");
        }

        private static string SanitizeEcho(string command, bool isUnix){
            // Escape backtick and question mark characters to avoid running commands instead of echo'ing them.
            string sanitizedRunCommand = command.Replace("`", "\\`")
                                                    .Replace("?", "\\")
                                                    .Replace("$", "")
                                                    .Replace("%", "")
                                                    .Replace("\r","")
                                                    .Replace("\n"," ")
                                                    .Replace("&", "^&")
                                                    .Replace(">", "^>");

            if (isUnix)
            {
                // Remove parentheses and quotes from echo command before wrapping it in quotes to avoid errors on Linux.
                sanitizedRunCommand = "\"" + sanitizedRunCommand.Replace("\"", "")
                                    .Replace("(", "")
                                    .Replace(")", "") + "\"";
            }
            return sanitizedRunCommand;
        }
    }
}