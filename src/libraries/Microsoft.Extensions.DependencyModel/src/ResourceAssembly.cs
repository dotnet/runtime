// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyModel
{
    public class ResourceAssembly
    {
        public ResourceAssembly(string path, string locale)
        {
            Locale = locale;
            Path = path;
        }

        public string Locale { get; set; }

        public string Path { get; set; }

    }
}
