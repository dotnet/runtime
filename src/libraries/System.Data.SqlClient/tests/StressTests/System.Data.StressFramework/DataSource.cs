// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stress.Data
{
    /// <summary>
    /// supported source types - values for 'type' attribute for 'source' node in App.config
    /// </summary>
    public enum DataSourceType
    {
        SqlServer
    }

    /// <summary>
    /// base class for database source information (SQL Server, Oracle Server, Access Database file, etc...). 
    /// Data sources are loaded from the app config file.
    /// </summary>
    public abstract class DataSource
    {
        /// <summary>
        /// name of the source - can be used in command line: StressTest ... -override source "sourcename"
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// database type
        /// </summary>
        public readonly DataSourceType Type;

        /// <summary>
        /// whether this source is the default one for the type specified
        /// </summary>
        public readonly bool IsDefault;

        /// <summary>
        /// constructs new data source - called by derived class c-tors only (thus protected)
        /// </summary>
        protected DataSource(string name, DataSourceType type, bool isDefault)
        {
            this.Name = name;
            this.Type = type;
            this.IsDefault = isDefault;
        }

        /// <summary>
        /// this method is used to create the data source, based on its type
        /// </summary>
        public static DataSource Create(string name, DataSourceType sourceType, bool isDefault, IDictionary<string, string> properties)
        {
            switch (sourceType)
            {
                case DataSourceType.SqlServer:
                    return new SqlServerDataSource(name, isDefault, properties);
                default:
                    throw new ArgumentException("Wrong source type value: " + sourceType);
            }
        }

        /// <summary>
        ///  used by GetRequiredAttributeValue or derived classes to construct exception on missing required attribute
        /// </summary>
        /// <param name="sourceName">name of the source (from XML) to include in exception message (for troubleshooting)</param>
        protected Exception MissingAttributeValueException(string sourceName, string attributeName)
        {
            return new ArgumentException(string.Format("Missing or empty value for {0} attribute in the config file for source: {1}", attributeName, sourceName));
        }

        /// <summary>
        /// search for required attribute or fail if not found
        /// </summary>
        protected string GetRequiredAttributeValue(string sourceName, IDictionary<string, string> properties, string valueName, bool allowEmpty)
        {
            string value;
            if (!properties.TryGetValue(valueName, out value) || (value == null) || (!allowEmpty && value.Length == 0))
            {
                throw MissingAttributeValueException(sourceName, valueName);
            }
            return value;
        }

        /// <summary>
        /// search for optional attribute or return default vale
        /// </summary>
        protected string GetOptionalAttributeValue(IDictionary<string, string> properties, string valueName, string defaultValue)
        {
            string value;
            if (!properties.TryGetValue(valueName, out value) || (value == null))
            {
                value = defaultValue;
            }
            return value;
        }
    }

    /// <summary>
    /// Represents SQL Server data source. This source is used by SqlClient as well as by ODBC and OLEDB when connecting to SQL with SNAC or MDAC/WDAC
    /// </summary>
    /// <example>
    ///       <source
    ///        name="mysrv01"
    ///        type="SqlServer"
    ///        isDefault="false"
    ///        dataSource="mysrv01"
    ///        database="stress"
    ///        user="stress"
    ///        password=""
    ///        supportsWindowsAuthentication="false">
    ///      </source>
    /// </example>
    public class SqlServerDataSource : DataSource
    {
        public readonly string DataSource;
        public readonly string Database;
        public readonly bool IsLocal;

        // if user and password are set, test can create connection strings with SQL auth settings
        public readonly string User;
        public readonly string Password;

        // if true, test can create connnection strings with integrated security (trusted connection) set to true (or SSPI).
        public readonly bool SupportsWindowsAuthentication;

        public bool DisableMultiSubnetFailoverSetup;

        public bool DisableNamedPipes;

        internal SqlServerDataSource(string name, bool isDefault, IDictionary<string, string> properties)
            : base(name, DataSourceType.SqlServer, isDefault)
        {
            this.DataSource = GetOptionalAttributeValue(properties, "dataSource", "localhost");
            this.Database = GetOptionalAttributeValue(properties, "database", "stress");

            this.User = GetOptionalAttributeValue(properties, "user", string.Empty);
            this.Password = GetOptionalAttributeValue(properties, "password", string.Empty);

            this.IsLocal = bool.Parse(GetOptionalAttributeValue(properties, "islocal", bool.FalseString));

            this.DisableMultiSubnetFailoverSetup = bool.Parse(GetOptionalAttributeValue(properties, "DisableMultiSubnetFailoverSetup", bool.TrueString));

            this.DisableNamedPipes = bool.Parse(GetOptionalAttributeValue(properties, "DisableNamedPipes", bool.TrueString));

            string temp = GetOptionalAttributeValue(properties, "supportsWindowsAuthentication", "false");
            if (!string.IsNullOrEmpty(temp))
                SupportsWindowsAuthentication = Convert.ToBoolean(temp);
            else
                SupportsWindowsAuthentication = false;

            if (string.IsNullOrEmpty(User) && !SupportsWindowsAuthentication)
                throw new ArgumentException("SQL Server settings should include either a valid User name or SupportsWindowsAuthentication=true");
        }
    }


}
