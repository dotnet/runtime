// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;

namespace System.Runtime.Caching.Configuration
{
    public sealed class CachingSectionGroup : ConfigurationSectionGroup
    {
        public CachingSectionGroup()
        {
        }

        // public properties
        [ConfigurationProperty("memoryCache")]
        public MemoryCacheSection MemoryCaches
        {
            get
            {
                return (MemoryCacheSection)Sections["memoryCache"];
            }
        }
    }
}
