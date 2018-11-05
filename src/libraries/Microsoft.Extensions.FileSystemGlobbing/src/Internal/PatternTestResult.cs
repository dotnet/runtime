// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileSystemGlobbing.Internal
{
    public struct PatternTestResult
    {
        public static readonly PatternTestResult Failed = new PatternTestResult(isSuccessful: false, stem: null);

        public bool IsSuccessful { get; }
        public string Stem { get; }

        private PatternTestResult(bool isSuccessful, string stem)
        {
            IsSuccessful = isSuccessful;
            Stem = stem;
        }

        public static PatternTestResult Success(string stem)
        {
            return new PatternTestResult(isSuccessful: true, stem: stem);
        }
    }
}