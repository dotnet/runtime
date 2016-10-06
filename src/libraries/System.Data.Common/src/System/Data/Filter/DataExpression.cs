// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Data.Common;

namespace System.Data
{
    internal sealed class DataExpression : IFilter
    {
        internal string _originalExpression = null;  // original, unoptimized string

        private bool _parsed = false;
        private bool _bound = false;
        private ExpressionNode _expr = null;
        private DataTable _table = null;
        private readonly StorageType _storageType;
        private readonly Type _dataType;  // This set if the expression is part of ExpressionCoulmn
        private DataColumn[] _dependency = Array.Empty<DataColumn>();

        internal DataExpression(DataTable table, string expression) : this(table, expression, null)
        {
        }

        internal DataExpression(DataTable table, string expression, Type type)
        {
            ExpressionParser parser = new ExpressionParser(table);
            parser.LoadExpression(expression);

            _originalExpression = expression;
            _expr = null;

            if (expression != null)
            {
                _storageType = DataStorage.GetStorageType(type);
                if (_storageType == StorageType.BigInteger)
                {
                    throw ExprException.UnsupportedDataType(type);
                }

                _dataType = type;
                _expr = parser.Parse();
                _parsed = true;
                if (_expr != null && table != null)
                {
                    Bind(table);
                }
                else
                {
                    _bound = false;
                }
            }
        }

        internal string Expression
        {
            get
            {
                return (_originalExpression != null ? _originalExpression : ""); // CONSIDER: return optimized expression here (if bound)
            }
        }

        internal ExpressionNode ExpressionNode
        {
            get
            {
                return _expr;
            }
        }

        internal bool HasValue
        {
            get
            {
                return (null != _expr);
            }
        }

        internal void Bind(DataTable table)
        {
            _table = table;

            if (table == null)
                return;

            if (_expr != null)
            {
                Debug.Assert(_parsed, "Invalid calling order: Bind() before Parse()");
                List<DataColumn> list = new List<DataColumn>();
                _expr.Bind(table, list);
                _expr = _expr.Optimize();
                _table = table;
                _bound = true;
                _dependency = list.ToArray();
            }
        }

        internal bool DependsOn(DataColumn column)
        {
            if (_expr != null)
            {
                return _expr.DependsOn(column);
            }
            else
            {
                return false;
            }
        }

        internal object Evaluate()
        {
            return Evaluate((DataRow)null, DataRowVersion.Default);
        }

        internal object Evaluate(DataRow row, DataRowVersion version)
        {
            object result;

            if (!_bound)
            {
                Bind(_table);
            }
            if (_expr != null)
            {
                result = _expr.Eval(row, version);
                // if the type is a SqlType (StorageType.Uri < _storageType), convert DBNull values.
                if (result != DBNull.Value || StorageType.Uri < _storageType)
                {
                    // we need to convert the return value to the column.Type;
                    try
                    {
                        if (StorageType.Object != _storageType)
                        {
                            result = SqlConvert.ChangeType2(result, _storageType, _dataType, _table.FormatProvider);
                        }
                    }
                    catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                    {
                        ExceptionBuilder.TraceExceptionForCapture(e);
                        throw ExprException.DatavalueConvertion(result, _dataType, e);
                    }
                }
            }
            else
            {
                result = null;
            }
            return result;
        }

        internal object Evaluate(DataRow[] rows)
        {
            return Evaluate(rows, DataRowVersion.Default);
        }


        internal object Evaluate(DataRow[] rows, DataRowVersion version)
        {
            if (!_bound)
            {
                Bind(_table);
            }
            if (_expr != null)
            {
                List<int> recordList = new List<int>();
                foreach (DataRow row in rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                        continue;
                    if (version == DataRowVersion.Original && row._oldRecord == -1)
                        continue;
                    recordList.Add(row.GetRecordFromVersion(version));
                }
                int[] records = recordList.ToArray();
                return _expr.Eval(records);
            }
            else
            {
                return DBNull.Value;
            }
        }

        public bool Invoke(DataRow row, DataRowVersion version)
        {
            if (_expr == null)
                return true;

            if (row == null)
            {
                throw ExprException.InvokeArgument();
            }
            object val = _expr.Eval(row, version);
            bool result;
            try
            {
                result = ToBoolean(val);
            }
            catch (EvaluateException)
            {
                throw ExprException.FilterConvertion(Expression);
            }
            return result;
        }

        internal DataColumn[] GetDependency()
        {
            Debug.Assert(_dependency != null, "GetDependencies: null, we should have created an empty list");
            return _dependency;
        }

        internal bool IsTableAggregate()
        {
            if (_expr != null)
                return _expr.IsTableConstant();
            else
                return false;
        }

        internal static bool IsUnknown(object value)
        {
            return DataStorage.IsObjectNull(value);
        }

        internal bool HasLocalAggregate()
        {
            if (_expr != null)
                return _expr.HasLocalAggregate();
            else
                return false;
        }

        internal bool HasRemoteAggregate()
        {
            if (_expr != null)
                return _expr.HasRemoteAggregate();
            else
                return false;
        }

        internal static bool ToBoolean(object value)
        {
            if (IsUnknown(value))
                return false;
            if (value is bool)
                return (bool)value;
            if (value is SqlBoolean)
            {
                return (((SqlBoolean)value).IsTrue);
            }
            //check for SqlString is not added, value for true and false should be given with String, not with SqlString
            if (value is string)
            {
                try
                {
                    return bool.Parse((string)value);
                }
                catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                {
                    ExceptionBuilder.TraceExceptionForCapture(e);
                    throw ExprException.DatavalueConvertion(value, typeof(bool), e);
                }
            }

            throw ExprException.DatavalueConvertion(value, typeof(bool), null);
        }
    }
}
