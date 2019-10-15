// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    // optional columns that DataAdapter.Fill(Schema) will pay attention to
    // when constructing new DataColumns to add to a DataTable
    public static class SchemaTableOptionalColumn
    {
        public static readonly string ProviderSpecificDataType = "ProviderSpecificDataType";

        public static readonly string IsAutoIncrement = "IsAutoIncrement";
        public static readonly string IsHidden = "IsHidden";
        public static readonly string IsReadOnly = "IsReadOnly";
        public static readonly string IsRowVersion = "IsRowVersion";
        public static readonly string BaseServerName = "BaseServerName";
        public static readonly string BaseCatalogName = "BaseCatalogName";

        public static readonly string AutoIncrementSeed = "AutoIncrementSeed";
        public static readonly string AutoIncrementStep = "AutoIncrementStep";
        public static readonly string DefaultValue = "DefaultValue";
        public static readonly string Expression = "Expression";

        public static readonly string BaseTableNamespace = "BaseTableNamespace";
        public static readonly string BaseColumnNamespace = "BaseColumnNamespace";
        public static readonly string ColumnMapping = "ColumnMapping";
    }
}
