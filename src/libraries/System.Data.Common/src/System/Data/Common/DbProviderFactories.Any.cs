// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Data.Common
{
    public static partial class DbProviderFactories
    {
        private const string AssemblyQualifiedNameColumnName = "AssemblyQualifiedName";
        private const string InvariantNameColumnName = "InvariantName";
        private const string NameColumnName = "Name";
        private const string DescriptionColumnName = "Description";
        private const string ProviderGroupColumnName = "DbProviderFactories";

        public static DbProviderFactory GetFactory(string providerInvariantName)
        {
            return GetFactory(providerInvariantName, throwOnError: true)!;
        }

        public static DbProviderFactory GetFactory(DataRow providerRow)
        {
            ADP.CheckArgumentNull(providerRow, nameof(providerRow));

            DataColumn? assemblyQualifiedNameColumn = providerRow.Table.Columns[AssemblyQualifiedNameColumnName];
            if (null == assemblyQualifiedNameColumn)
            {
                throw ADP.Argument(SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
            }

            string? assemblyQualifiedName = providerRow[assemblyQualifiedNameColumn] as string;
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                throw ADP.Argument(SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
            }

            return GetFactoryInstance(GetProviderTypeFromTypeName(assemblyQualifiedName));
        }


        public static DbProviderFactory? GetFactory(DbConnection connection)
        {
            ADP.CheckArgumentNull(connection, nameof(connection));

            return connection.ProviderFactory;
        }

        public static DataTable GetFactoryClasses()
        {
            DataColumn nameColumn = new DataColumn(NameColumnName, typeof(string)) { ReadOnly = true };
            DataColumn descriptionColumn = new DataColumn(DescriptionColumnName, typeof(string)) { ReadOnly = true };
            DataColumn invariantNameColumn = new DataColumn(InvariantNameColumnName, typeof(string)) { ReadOnly = true };
            DataColumn assemblyQualifiedNameColumn = new DataColumn(AssemblyQualifiedNameColumnName, typeof(string)) { ReadOnly = true };

            DataTable toReturn = new DataTable(ProviderGroupColumnName) { Locale = CultureInfo.InvariantCulture };
            toReturn.Columns.AddRange(new[] { nameColumn, descriptionColumn, invariantNameColumn, assemblyQualifiedNameColumn });
            toReturn.PrimaryKey = new[] { invariantNameColumn };
            foreach (var kvp in _registeredFactories)
            {
                DataRow newRow = toReturn.NewRow();
                newRow[InvariantNameColumnName] = kvp.Key;
                newRow[AssemblyQualifiedNameColumnName] = kvp.Value.FactoryTypeAssemblyQualifiedName;
                newRow[NameColumnName] = string.Empty;
                newRow[DescriptionColumnName] = string.Empty;
                toReturn.AddRow(newRow);
            }
            return toReturn;
        }
    }
}

