// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Apply this attribute to your test method to specify an active issue.
    /// </summary>
    [TraitDiscoverer("Xunit.TraitDiscoverers.ActiveIssueDiscoverer", "XunitTraitsDiscoverers")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ActiveIssueAttribute : Attribute, ITraitAttribute
    {
        public ActiveIssueAttribute(int issueNumber, PlatformID platforms = PlatformID.Any) { }
        public ActiveIssueAttribute(string issue, PlatformID platforms = PlatformID.Any) { }
    }
}
