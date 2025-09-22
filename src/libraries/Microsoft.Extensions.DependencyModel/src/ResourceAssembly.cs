// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class ResourceAssembly
    {
        public ResourceAssembly(string path, string locale)
            : this(path, locale, null)
        { }

        public ResourceAssembly(string path, string locale, string? localPath)
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
            LocalPath = localPath;
        }

        public string Locale { get; set; }

        // Depending on the source of the resource assembly, this path may be relative to the
        // a referenced NuGet package's root or to the app/component root.
        public string Path { get; set; }

        // Path relative to the app/component represented by the dependency context
        public string? LocalPath { get; }
    }
}
