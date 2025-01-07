// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    public interface IDependencyContextReader : IDisposable
    {
        DependencyContext Read(Stream stream);
    }
}
