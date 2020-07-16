// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Data.Common
{
    public static partial class DbProviderFactories
    {
        public static DbProviderFactory GetFactory(string providerInvariantName)
        {
            throw new PlatformNotSupportedException();
        }

        public static DbProviderFactory GetFactory(DataRow providerRow)
        {
            throw new PlatformNotSupportedException();
        }

        public static DbProviderFactory? GetFactory(DbConnection connection)
        {
            throw new PlatformNotSupportedException();
        }

        public static DataTable GetFactoryClasses()
        {
            throw new PlatformNotSupportedException();
        }
    }
}