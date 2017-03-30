// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // This is the public type of the override mode enum visible to the API users
    // The override mode is an attribute of a <location> tag and controls if the sections inside this tag
    // can be defined in child web.config files
    public enum OverrideMode
    {
        // Default ( aka Indiferent ) - When specified on a location tag means the location tag will not alter
        // the locking mode ( locked or unlocked ). Rather, the locking mode should be picked
        // from the <location> tag with the closest parent path in the current file, or the parent file if no such location in the current one,
        // or the default for the specific section ( section.OverrideModeDefault )
        Inherit = 0,
        // Allow overriding in child config files. I.e. unlock the settings for overridiing
        Allow = 1,
        // Deny overriding of the settings defined in the <location> tag. It is an error for the sections in the <location> tag
        // to appear in a child config file. It is not an error for them to appear in another <lcoation> tag in the current file
        Deny = 2,
    }
}