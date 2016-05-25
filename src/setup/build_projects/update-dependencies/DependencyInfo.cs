// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using NuGet.Versioning;

namespace Microsoft.DotNet.Scripts
{
    public class DependencyInfo
    {
        public string Name { get; set; }
        public List<PackageInfo> NewVersions { get; set; }
        public string NewReleaseVersion { get; set; }

        public bool IsUpdated { get; set; }
    }

    public class PackageInfo
    {
        public string Id { get; set; }
        public NuGetVersion Version { get; set; }
    }
}
