// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    internal sealed class LogAdapter(TaskLoggingHelper taskLogger) : Logger
    {
        protected override void LogCore(in Message message)
        {
            switch (message.Level)
            {
                case MessageLevel.Error:
                    taskLogger.LogError(
                        subcategory: default,
                        errorCode: message.Code,
                        helpKeyword: default,
                        file: message.File,
                        lineNumber: default,
                        columnNumber: default,
                        endLineNumber: default,
                        endColumnNumber: default,
                        message: message.Text);
                    break;

                case MessageLevel.Warning:
                    taskLogger.LogWarning(
                        subcategory: default,
                        warningCode: message.Code,
                        helpKeyword: default,
                        file: message.File,
                        lineNumber: default,
                        columnNumber: default,
                        endLineNumber: default,
                        endColumnNumber: default,
                        message: message.Text);
                    break;

                case MessageLevel.HighImportance:
                case MessageLevel.NormalImportance:
                case MessageLevel.LowImportance:
                    if (message.Code == null && message.File == null)
                    {
                        // use shorter overload when there is no code and no file. Otherwise, msbuild
                        // will display:
                        //
                        // <project file>(<line>,<column>): message : <text>
                        taskLogger.LogMessage(message.Level.ToImportance(), message.Text);
                    }
                    else
                    {
                        taskLogger.LogMessage(
                            subcategory: default,
                            code: message.Code,
                            helpKeyword: default,
                            file: message.File,
                            lineNumber: default,
                            columnNumber: default,
                            endLineNumber: default,
                            endColumnNumber: default,
                            importance: message.Level.ToImportance(),
                            message: message.Text);
                    }
                    break;

                default:
                    throw new ArgumentException(
                        $"Message \"{message.Code}: {message.Text}\" logged with invalid Level=${message.Level}",
                        paramName: nameof(message));
            }
        }
    }
}
