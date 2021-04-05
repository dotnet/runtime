// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    public class TestBuildEngine : IBuildEngine
    {
        private ILog _log;

        public TestBuildEngine(ILog log)
        {
            ColumnNumberOfTaskNode = 0;
            ContinueOnError = true;
            LineNumberOfTaskNode = 0;
            ProjectFileOfTaskNode = "test";
            _log = log;
        }

        public int ColumnNumberOfTaskNode { get; set; }

        public bool ContinueOnError { get; set; }

        public int LineNumberOfTaskNode { get; set; }

        public string ProjectFileOfTaskNode { get; set; }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            _log.LogMessage(e.Message);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            _log.LogError(e.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            _log.LogMessage((LogImportance)e.Importance, e.Message);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            _log.LogWarning(e.Message);
        }
    }
}
