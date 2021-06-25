// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class FileExcludedException : Exception
    {
        public ExclusionFilters MatchingFilter { get; }

        public FileExcludedException(string message, ExclusionFilters matchingFilter)
            : base(message)
        {
            MatchingFilter = matchingFilter;
        }
    }
}
