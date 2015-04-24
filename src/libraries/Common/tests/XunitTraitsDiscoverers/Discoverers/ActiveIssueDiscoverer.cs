// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.TraitDiscoverers
{
    /// <summary>
    /// This class discovers all of the tests and test classes that have
    /// applied the ActiveIssue attribute
    /// </summary>
    public class ActiveIssueDiscoverer : ITraitDiscoverer
    {
        /// <summary>
        /// Gets the trait values from the Category attribute.
        /// </summary>
        /// <param name="traitAttribute">The trait attribute containing the trait values.</param>
        /// <returns>The trait values.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            IEnumerable<object> ctorArgs = traitAttribute.GetConstructorArguments();
            Debug.Assert(ctorArgs.Count() >= 2);

            string issue = ctorArgs.First().ToString();
            PlatformID platforms = (PlatformID)ctorArgs.Last();
            if ((platforms.HasFlag(PlatformID.Windows) && Interop.IsWindows) ||
                (platforms.HasFlag(PlatformID.Linux) && Interop.IsLinux) ||
                (platforms.HasFlag(PlatformID.OSX) && Interop.IsOSX))
            {
                yield return new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.Failing);
                yield return new KeyValuePair<string, string>(XunitConstants.ActiveIssue, issue);
            }
                
        }
    }
}
