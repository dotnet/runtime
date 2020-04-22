// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class TargetInfo
    {
        public TargetInfo(string framework,
            string runtime,
            string runtimeSignature,
            bool isPortable)
        {
            if (string.IsNullOrEmpty(framework))
            {
                throw new ArgumentException(nameof(framework));
            }

            Framework = framework;
            Runtime = runtime;
            RuntimeSignature = runtimeSignature;
            IsPortable = isPortable;
        }

        public string Framework { get; }

        public string Runtime { get; }

        public string RuntimeSignature { get; }

        public bool IsPortable { get; }

    }
}