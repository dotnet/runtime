// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Data.Common
{
    public static partial class DbProviderFactories
    {
        private struct ProviderRegistration
        {
            internal ProviderRegistration(string factoryTypeAssemblyQualifiedName, DbProviderFactory factoryInstance)
            {
                this.FactoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
                this.FactoryInstance = factoryInstance;
            }

            internal string FactoryTypeAssemblyQualifiedName { get; }
            /// <summary>
            /// The cached instance of the type in <see cref="FactoryTypeAssemblyQualifiedName"/>. If null, this registation is seen as a deferred registration
            /// and <see cref="FactoryTypeAssemblyQualifiedName"/> is checked the first time when this registration is requested through GetFactory().
            /// </summary>
            internal DbProviderFactory FactoryInstance { get; }
        }

        private static ConcurrentDictionary<string, ProviderRegistration> _registeredFactories = new ConcurrentDictionary<string, ProviderRegistration>();
        private const string AssemblyQualifiedNameColumnName = "AssemblyQualifiedName";
        private const string InvariantNameColumnName = "InvariantName";
        private const string NameColumnName = "Name";
        private const string DescriptionColumnName = "Description";
        private const string ProviderGroupColumnName = "DbProviderFactories";
        private const string InstanceFieldName = "Instance";

        public static bool TryGetFactory(string providerInvariantName, out DbProviderFactory factory)
        {
            factory = GetFactory(providerInvariantName, throwOnError: false);
            return factory != null;
        }

        public static DbProviderFactory GetFactory(string providerInvariantName)
        {
            return GetFactory(providerInvariantName, throwOnError: true);
        }

        public static DbProviderFactory GetFactory(DataRow providerRow)
        {
            ADP.CheckArgumentNull(providerRow, nameof(providerRow));

            DataColumn assemblyQualifiedNameColumn = providerRow.Table.Columns[AssemblyQualifiedNameColumnName];
            if (null == assemblyQualifiedNameColumn)
            {
                throw ADP.Argument(SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
            }

            string assemblyQualifiedName = providerRow[assemblyQualifiedNameColumn] as string;
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                throw ADP.Argument(SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
            }

            return GetFactoryInstance(GetProviderTypeFromTypeName(assemblyQualifiedName));
        }


        public static DbProviderFactory GetFactory(DbConnection connection)
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
            foreach(var kvp in _registeredFactories)
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

        public static IEnumerable<string> GetProviderInvariantNames()
        {
            return _registeredFactories.Keys.ToList();
        }

        public static void RegisterFactory(string providerInvariantName, string factoryTypeAssemblyQualifiedName)
        {
            ADP.CheckArgumentLength(providerInvariantName, nameof(providerInvariantName));
            ADP.CheckArgumentLength(factoryTypeAssemblyQualifiedName, nameof(factoryTypeAssemblyQualifiedName));
            
            // this method performs a deferred registration: the type name specified is checked when the factory is requested for the first time. 
            _registeredFactories[providerInvariantName] = new ProviderRegistration(factoryTypeAssemblyQualifiedName, null);
        }
        
        public static void RegisterFactory(string providerInvariantName, Type providerFactoryClass)
        {
            RegisterFactory(providerInvariantName, GetFactoryInstance(providerFactoryClass));
        }

        public static void RegisterFactory(string providerInvariantName, DbProviderFactory factory)
        {
            ADP.CheckArgumentLength(providerInvariantName, nameof(providerInvariantName));
            ADP.CheckArgumentNull(factory, nameof(factory));

            _registeredFactories[providerInvariantName] = new ProviderRegistration(factory.GetType().AssemblyQualifiedName, factory);
        }
        
        public static bool UnregisterFactory(string providerInvariantName)
        {
            return !string.IsNullOrWhiteSpace(providerInvariantName) && _registeredFactories.TryRemove(providerInvariantName, out _);
        }
        
        private static DbProviderFactory GetFactory(string providerInvariantName, bool throwOnError)
        {
            if (throwOnError)
            {
                ADP.CheckArgumentLength(providerInvariantName, nameof(providerInvariantName));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(providerInvariantName))
                {
                    return null;
                }
            }
            bool wasRegistered = _registeredFactories.TryGetValue(providerInvariantName, out ProviderRegistration registration);
            if (!wasRegistered)
            {
                return throwOnError ? throw ADP.Argument(SR.Format(SR.ADP_DbProviderFactories_InvariantNameNotFound, providerInvariantName)) : (DbProviderFactory)null;
            }
            DbProviderFactory toReturn = registration.FactoryInstance;
            if (toReturn == null)
            {
                // Deferred registration, do checks now on the type specified and register instance in storage.
                // Even in the case of throwOnError being false, this will throw when an exception occurs checking the registered type as the user has to be notified the 
                // registration is invalid, even though the registration is there.
                toReturn = GetFactoryInstance(GetProviderTypeFromTypeName(registration.FactoryTypeAssemblyQualifiedName));
                RegisterFactory(providerInvariantName, toReturn);
            }
            return toReturn;
        }
       
        private static DbProviderFactory GetFactoryInstance(Type providerFactoryClass)
        {
            ADP.CheckArgumentNull(providerFactoryClass, nameof(providerFactoryClass));
            if (!providerFactoryClass.IsSubclassOf(typeof(DbProviderFactory)))
            {
                throw ADP.Argument(SR.Format(SR.ADP_DbProviderFactories_NotAFactoryType, providerFactoryClass.FullName));
            }

            FieldInfo providerInstance = providerFactoryClass.GetField(InstanceFieldName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            if (null == providerInstance)
            {
                throw ADP.InvalidOperation(SR.ADP_DbProviderFactories_NoInstance);
            }
            if (!providerInstance.FieldType.IsSubclassOf(typeof(DbProviderFactory)))
            {
                throw ADP.InvalidOperation(SR.ADP_DbProviderFactories_NoInstance);
            }
            object factory = providerInstance.GetValue(null);
            if (null == factory)
            {
                throw ADP.InvalidOperation(SR.ADP_DbProviderFactories_NoInstance);
            }
            return (DbProviderFactory)factory;
        }


        private static Type GetProviderTypeFromTypeName(string assemblyQualifiedName)
        {
            Type providerType = Type.GetType(assemblyQualifiedName);
            if (null == providerType)
            {
                throw ADP.Argument(SR.Format(SR.ADP_DbProviderFactories_FactoryNotLoadable, assemblyQualifiedName));
            }
            return providerType;
        }
    }
}
