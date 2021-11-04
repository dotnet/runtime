// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SkipOnPlatformAttribute : Attribute
    {
        internal SkipOnPlatformAttribute() { }
        public SkipOnPlatformAttribute(TestPlatforms testPlatforms, string reason) { }
    }
}
