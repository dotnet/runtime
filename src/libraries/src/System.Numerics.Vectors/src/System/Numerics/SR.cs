// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Resources;

namespace System.Numerics
{
    /// <summary>
    /// A static class that encapsulates methods to perform operations on resources file (.resx)
    /// </summary>
    internal static class SR
    {
        private static readonly ResourceManager _resourceManager;

        static SR()
        {
            _resourceManager = new ResourceManager("System.Numerics.Resources.Strings", typeof(SR).GetTypeInfo().Assembly);
        }

        internal static string GetString(string key, params object[] args)
        {
            string strArgs = string.Join(" ", args);
            return _resourceManager.GetString(key + strArgs);
        }
    }
}
