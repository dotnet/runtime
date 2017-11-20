// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.Server
{
    [Serializable]
    public enum DataAccessKind
    {
        None = 0,
        Read = 1,
    }

    [Serializable]
    public enum SystemDataAccessKind
    {
        None = 0,
        Read = 1,
    }

    // sql specific attribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false), Serializable]
    public class SqlFunctionAttribute : Attribute
    {
        private bool _isDeterministic;
        private DataAccessKind _dataAccess;
        private SystemDataAccessKind _systemDataAccess;
        private bool _isPrecise;
        private string _name;
        private string _tableDefinition;
        private string _fillRowMethodName;

        public SqlFunctionAttribute()
        {
            // default values
            _isDeterministic = false;
            _dataAccess = DataAccessKind.None;
            _systemDataAccess = SystemDataAccessKind.None;
            _isPrecise = false;
            _name = null;
            _tableDefinition = null;
            _fillRowMethodName = null;
        }

        public bool IsDeterministic
        {
            get => _isDeterministic;
            set => _isDeterministic = value;
        }

        public DataAccessKind DataAccess
        {
            get => _dataAccess;
            set => _dataAccess = value;
        }

        public SystemDataAccessKind SystemDataAccess
        {
            get => _systemDataAccess;
            set => _systemDataAccess = value;
        }

        public bool IsPrecise
        {
            get => _isPrecise;
            set => _isPrecise = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string TableDefinition
        {
            get => _tableDefinition;
            set => _tableDefinition = value;
        }

        public string FillRowMethodName
        {
            get => _fillRowMethodName;
            set => _fillRowMethodName = value;
        }
    }
}