// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    public static class DbDataReaderExtension
    {
        public static System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
        {
            if (reader.ProvidesSchema())
            {
                return ((IDbColumnSchemaGenerator)reader).GetColumnSchema();
            }
            throw new NotImplementedException();
        }

        public static bool ProvidesSchema(this DbDataReader reader)
        {
            return reader is IDbColumnSchemaGenerator;
        }
    }
}
