// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    public interface IInternalConfigConfigurationFactory
    {
        Configuration Create(Type typeConfigHost, params object[] hostInitConfigurationParams);
        string NormalizeLocationSubPath(string subPath, IConfigErrorInfo errorInfo);
    }
}