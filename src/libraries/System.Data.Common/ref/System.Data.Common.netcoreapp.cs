// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;

namespace System.Data.Common
{
    public static partial class DbProviderFactories
    {
        public static void RegisterFactory(string providerInvariantName, string factoryTypeAssemblyQualifiedName) { throw null; }
        public static void RegisterFactory(string providerInvariantName, Type factoryType) { throw null; }
        public static void RegisterFactory(string providerInvariantName, DbProviderFactory factory) { throw null; }
        public static bool TryGetFactory(string providerInvariantName, out DbProviderFactory factory) { throw null; }
        public static bool UnregisterFactory(string providerInvariantName) { throw null; }
        public static IEnumerable<string> GetProviderInvariantNames() { throw null; }
    }
}
