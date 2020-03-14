// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on helix (or a particular helix queue).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnHelixAttribute : Attribute, ITestCondition
    {
        public SkipOnHelixAttribute(string issueUrl)
        {
            if (string.IsNullOrEmpty(issueUrl))
            {
                throw new ArgumentException();
            }
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                var skip = OnHelix() && (Queues == null || Queues.ToLowerInvariant().Split(';').Contains(GetTargetHelixQueue().ToLowerInvariant()));
                return !skip;
            }
        }

        // Queues that should be skipped on, i.e. "Windows.10.Amd64.ClientRS4.VS2017.Open;OSX.1012.Amd64.Open"
        public string Queues { get; set; }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on helix";
            }
        }

        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());

        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
    }
}
