// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Scripts
{
    public static class BuildContextProperties
    {
        public static List<DependencyInfo> GetDependencyInfos(this BuildTargetContext c)
        {
            const string propertyName = "DependencyInfos";

            List<DependencyInfo> dependencyInfos;
            object dependencyInfosObj;
            if (c.BuildContext.Properties.TryGetValue(propertyName, out dependencyInfosObj))
            {
                dependencyInfos = (List<DependencyInfo>)dependencyInfosObj;
            }
            else
            {
                dependencyInfos = new List<DependencyInfo>();
                c.BuildContext[propertyName] = dependencyInfos;
            }

            return dependencyInfos;
        }
    }
}
