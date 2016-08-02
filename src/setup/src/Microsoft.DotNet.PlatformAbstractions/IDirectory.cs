﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.EnvironmentAbstractions
{
    internal interface IDirectory
    {
        bool Exists(string path);

        ITemporaryDirectory CreateTemporaryDirectory();
    }
}