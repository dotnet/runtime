// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
            string issue = traitAttribute.GetConstructorArguments().First().ToString();
            PlatformID platforms = (PlatformID)traitAttribute.GetConstructorArguments().Last();
            if (platforms.HasFlag(PlatformID.Windows))
                yield return new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.NonWindowsTest);
            if (platforms.HasFlag(PlatformID.Linux))
                yield return new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.NonLinuxTest);
            if (platforms.HasFlag(PlatformID.OSX))
                yield return new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.NonOSXTest);
        }
    }
}
