// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class ResourceAssembly
    {
        public ResourceAssembly(string path, string locale)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(null, nameof(path));
            }
            if (string.IsNullOrEmpty(locale))
            {
                throw new ArgumentException(null, nameof(locale));
            }
            Locale = locale;
            Path = path;
        }

        public string Locale { get; set; }

        public string Path { get; set; }

    }
}
