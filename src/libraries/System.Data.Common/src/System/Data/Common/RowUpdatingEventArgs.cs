// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    public class RowUpdatingEventArgs : EventArgs
    {
        private IDbCommand _command;
        private StatementType _statementType;
        private DataTableMapping _tableMapping;
        private Exception _errors;

        private DataRow _dataRow;
        private UpdateStatus _status;

        public RowUpdatingEventArgs(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
        {
            ADP.CheckArgumentNull(dataRow, nameof(dataRow));
            ADP.CheckArgumentNull(tableMapping, nameof(tableMapping));
            switch (statementType)
            {
                case StatementType.Select:
                case StatementType.Insert:
                case StatementType.Update:
                case StatementType.Delete:
                    break;
                case StatementType.Batch:
                    throw ADP.NotSupportedStatementType(statementType, nameof(RowUpdatingEventArgs));
                default:
                    throw ADP.InvalidStatementType(statementType);
            }
            _dataRow = dataRow;
            _command = command; // maybe null
            _statementType = statementType;
            _tableMapping = tableMapping;
        }

        protected virtual IDbCommand BaseCommand
        {
            get { return _command; }
            set { _command = value; }
        }

        public IDbCommand Command
        {
            get { return BaseCommand; }
            set { BaseCommand = value; }
        }

        public Exception Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }

        public DataRow Row => _dataRow;

        public StatementType StatementType => _statementType;

        public UpdateStatus Status
        {
            get { return _status; }
            set
            {
                switch (value)
                {
                    case UpdateStatus.Continue:
                    case UpdateStatus.ErrorsOccurred:
                    case UpdateStatus.SkipCurrentRow:
                    case UpdateStatus.SkipAllRemainingRows:
                        _status = value;
                        break;
                    default:
                        throw ADP.InvalidUpdateStatus(value);
                }
            }
        }

        public DataTableMapping TableMapping => _tableMapping;
    }
}
