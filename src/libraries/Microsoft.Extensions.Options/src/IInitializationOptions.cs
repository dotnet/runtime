// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    public interface IInitializationOptions<out TOptions> where TOptions : class
    {
        TOptions Initialize(string name);
    }
}
