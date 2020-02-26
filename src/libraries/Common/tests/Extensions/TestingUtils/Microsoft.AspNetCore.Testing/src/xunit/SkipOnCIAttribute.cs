// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on CI
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnCIAttribute : Attribute, ITestCondition
    {
        public SkipOnCIAttribute(string issueUrl = "")
        {
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                return !OnCI();
            }
        }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on CI";
            }
        }

        public static bool OnCI() => OnHelix() || OnAzdo();
        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());
        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
        public static bool OnAzdo() => !string.IsNullOrEmpty(GetIfOnAzdo());
        public static string GetIfOnAzdo() => Environment.GetEnvironmentVariable("AGENT_OS");
    }
}
