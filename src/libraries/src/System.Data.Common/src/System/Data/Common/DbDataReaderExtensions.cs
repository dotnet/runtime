// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    public static class DbDataReaderExtensions
    {
        public static System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
        {
            if (reader.CanGetColumnSchema())
            {
                return ((IDbColumnSchemaGenerator)reader).GetColumnSchema();
            }
            throw new NotSupportedException();
        }

        public static bool CanGetColumnSchema(this DbDataReader reader)
        {
            return reader is IDbColumnSchemaGenerator;
        }
    }
}
