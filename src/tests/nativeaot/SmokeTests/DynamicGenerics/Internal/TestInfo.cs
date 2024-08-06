// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace RuntimeLibrariesTest.Internal
{
    public class TestInfo
    {
        public String Name { get; private set; }
        public Action AsAction { get; private set; }
        public Func<Task> AsFunc { get; private set; }
        public bool IsTask { get; private set; }
        public bool IsIgnored { get; private set; }
        public string IgnoredReason { get; private set; }
        public bool ShouldExecute { get; set; }
        public ClassInfo InstanceClass { get; set; }
        public ExpectedExceptionAttribute ExpectsException { get; set; }

        public TestInfo(String Name, Action a, ExpectedExceptionAttribute expectsExceptionType, bool IsIgnored = false, string ignoredReason = "")
        {
            this.Name = Name;
            this.AsAction = a;
            this.IsTask = false;
            this.IsIgnored = IsIgnored;
            this.IgnoredReason = ignoredReason;
            this.ShouldExecute = false;
            this.ExpectsException = expectsExceptionType;
        }

        public TestInfo(String Name, Func<Task> t, ExpectedExceptionAttribute expectsExceptionType, bool IsIgnored = false, string ignoredReason = "")
        {
            this.Name = Name;
            this.AsFunc = t;
            this.IsTask = true;
            this.IsIgnored = IsIgnored;
            this.IgnoredReason = ignoredReason;
            this.ShouldExecute = false;
            this.ExpectsException = expectsExceptionType;
        }
    }
}
