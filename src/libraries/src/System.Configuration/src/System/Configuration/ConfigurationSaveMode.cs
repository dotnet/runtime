// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // Determines how much of configuration is written out on save.
    public enum ConfigurationSaveMode
    {
        // If a setting is modified, it'll get written no matter it's
        // same as the parent or not.
        Modified = 0,

        // If a setting is the same as in its parent, it won't get written
        Minimal = 1,

        // It writes out all the properties in the configurationat that level,
        // including the one from the parents.  Used for writing out the
        // full config settings at a file.
        Full = 2,
    }
}