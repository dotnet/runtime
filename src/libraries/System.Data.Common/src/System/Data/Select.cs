// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data
{
    internal sealed class Select
    {
        private readonly DataTable _table;
        private readonly IndexField[] _indexFields;
        private readonly DataViewRowState _recordStates;
        private readonly DataExpression? _rowFilter;
        private readonly ExpressionNode? _expression;

        private Index? _index;

        private int[]? _records;
        private int _recordCount;

        private ExpressionNode? _linearExpression;
        private bool _candidatesForBinarySearch;

        private sealed class ColumnInfo
        {
            public bool flag;               // Misc. Use
            public bool equalsOperator;     // True when the associated expr has = Operator defined
            public BinaryNode? expr;          // Binary Search capable expression associated
        }

        private ColumnInfo[]? _candidateColumns;
        private int _nCandidates;
        private int _matchedCandidates;

        public Select(DataTable table, string? filterExpression, string? sort, DataViewRowState recordStates)
        {
            _table = table;
            _indexFields = table.ParseSortString(sort);
            if (filterExpression != null && filterExpression.Length > 0)
            {
                _rowFilter = new DataExpression(_table, filterExpression);
                _expression = _rowFilter.ExpressionNode;
            }
            _recordStates = recordStates;
        }

        private bool IsSupportedOperator(int op)
        {
            return ((op >= Operators.EqualTo && op <= Operators.LessOrEqual) || op == Operators.Is || op == Operators.IsNot);
        }

        // Gathers all linear expressions in to this.linearExpression and all binary expressions in to their respective candidate columns expressions
        private void AnalyzeExpression(BinaryNode expr)
        {
            Debug.Assert(_candidateColumns != null);

            if (_linearExpression == _expression)
                return;

            if (expr._op == Operators.Or)
            {
                _linearExpression = _expression;
                return;
            }
            else
            if (expr._op == Operators.And)
            {
                bool isLeft = false, isRight = false;
                if (expr._left is BinaryNode)
                {
                    AnalyzeExpression((BinaryNode)expr._left);
                    if (_linearExpression == _expression)
                        return;
                    isLeft = true;
                }
                else
                {
                    UnaryNode? unaryNode = expr._left as UnaryNode;
                    if (unaryNode != null)
                    {
                        while (unaryNode._op == Operators.Noop && unaryNode._right is UnaryNode && ((UnaryNode)unaryNode._right)._op == Operators.Noop)
                        {
                            unaryNode = (UnaryNode)unaryNode._right;
                        }
                        if (unaryNode._op == Operators.Noop && unaryNode._right is BinaryNode)
                        {
                            AnalyzeExpression((BinaryNode)(unaryNode._right));
                            if (_linearExpression == _expression)
                            {
                                return;
                            }
                            isLeft = true;
                        }
                    }
                }

                if (expr._right is BinaryNode)
                {
                    AnalyzeExpression((BinaryNode)expr._right);
                    if (_linearExpression == _expression)
                        return;
                    isRight = true;
                }
                else
                {
                    UnaryNode? unaryNode = expr._right as UnaryNode;
                    if (unaryNode != null)
                    {
                        while (unaryNode._op == Operators.Noop && unaryNode._right is UnaryNode && ((UnaryNode)unaryNode._right)._op == Operators.Noop)
                        {
                            unaryNode = (UnaryNode)unaryNode._right;
                        }
                        if (unaryNode._op == Operators.Noop && unaryNode._right is BinaryNode)
                        {
                            AnalyzeExpression((BinaryNode)(unaryNode._right));
                            if (_linearExpression == _expression)
                            {
                                return;
                            }

                            isRight = true;
                        }
                    }
                }

                if (isLeft && isRight)
                    return;

                ExpressionNode e = isLeft ? expr._right : expr._left;
                _linearExpression = (_linearExpression == null ? e : new BinaryNode(_table, Operators.And, e, _linearExpression));
                return;
            }
            else
            if (IsSupportedOperator(expr._op))
            {
                if (expr._left is NameNode && expr._right is ConstNode)
                {
                    ColumnInfo canColumn = _candidateColumns[((NameNode)(expr._left))._column!.Ordinal];
                    canColumn.expr = (canColumn.expr == null ? expr : new BinaryNode(_table, Operators.And, expr, canColumn.expr));
                    if (expr._op == Operators.EqualTo)
                    {
                        canColumn.equalsOperator = true;
                    }
                    _candidatesForBinarySearch = true;
                    return;
                }
                else
                if (expr._right is NameNode && expr._left is ConstNode)
                {
                    ExpressionNode temp = expr._left;
                    expr._left = expr._right;
                    expr._right = temp;
                    switch (expr._op)
                    {
                        case Operators.GreaterThen: expr._op = Operators.LessThen; break;
                        case Operators.LessThen: expr._op = Operators.GreaterThen; break;
                        case Operators.GreaterOrEqual: expr._op = Operators.LessOrEqual; break;
                        case Operators.LessOrEqual: expr._op = Operators.GreaterOrEqual; break;
                        default: break;
                    }
                    ColumnInfo canColumn = _candidateColumns[((NameNode)(expr._left))._column!.Ordinal];
                    canColumn.expr = (canColumn.expr == null ? expr : new BinaryNode(_table, Operators.And, expr, canColumn.expr));
                    if (expr._op == Operators.EqualTo)
                    {
                        canColumn.equalsOperator = true;
                    }
                    _candidatesForBinarySearch = true;
                    return;
                }
            }

            _linearExpression = (_linearExpression == null ? expr : new BinaryNode(_table, Operators.And, expr, _linearExpression));
            return;
        }

        private bool CompareSortIndexDesc(IndexField[] fields)
        {
            Debug.Assert(_candidateColumns != null);

            if (fields.Length < _indexFields.Length)
                return false;
            int j = 0;
            for (int i = 0; i < fields.Length && j < _indexFields.Length; i++)
            {
                if (fields[i] == _indexFields[j])
                {
                    j++;
                }
                else
                {
                    ColumnInfo canColumn = _candidateColumns[fields[i].Column.Ordinal];
                    if (!(canColumn != null && canColumn.equalsOperator))
                        return false;
                }
            }
            return j == _indexFields.Length;
        }

        private bool FindSortIndex()
        {
            _index = null;
            _table._indexesLock.EnterUpgradeableReadLock();
            try
            {
                int count = _table._indexes.Count;
                for (int i = 0; i < count; i++)
                {
                    Index ndx = _table._indexes[i];
                    if (ndx.RecordStates != _recordStates)
                        continue;
                    if (!ndx.IsSharable)
                    {
                        continue;
                    }
                    if (CompareSortIndexDesc(ndx._indexFields))
                    {
                        _index = ndx;
                        return true;
                    }
                }
            }
            finally
            {
                _table._indexesLock.ExitUpgradeableReadLock();
            }
            return false;
        }

        // Returns no. of columns that are matched
        private int CompareClosestCandidateIndexDesc(IndexField[] fields)
        {
            Debug.Assert(_candidateColumns != null);

            int count = (fields.Length < _nCandidates ? fields.Length : _nCandidates);
            int i = 0;
            for (; i < count; i++)
            {
                ColumnInfo canColumn = _candidateColumns[fields[i].Column.Ordinal];
                if (canColumn == null || canColumn.expr == null)
                {
                    break;
                }
                else
                if (!canColumn.equalsOperator)
                {
                    return i + 1;
                }
            }
            return i;
        }

        // Returns whether the found index (if any) is a sort index as well
        private bool FindClosestCandidateIndex()
        {
            _index = null;
            _matchedCandidates = 0;
            bool sortPriority = true;
            _table._indexesLock.EnterUpgradeableReadLock();
            try
            {
                int count = _table._indexes.Count;
                for (int i = 0; i < count; i++)
                {
                    Index ndx = _table._indexes[i];
                    if (ndx.RecordStates != _recordStates)
                        continue;
                    if (!ndx.IsSharable)
                        continue;
                    int match = CompareClosestCandidateIndexDesc(ndx._indexFields);
                    if (match > _matchedCandidates || (match == _matchedCandidates && !sortPriority))
                    {
                        _matchedCandidates = match;
                        _index = ndx;
                        sortPriority = CompareSortIndexDesc(ndx._indexFields);
                        if (_matchedCandidates == _nCandidates && sortPriority)
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                _table._indexesLock.ExitUpgradeableReadLock();
            }

            return (_index != null ? sortPriority : false);
        }

        // Initialize candidate columns to new columnInfo and leave all non candidate columns to null
        private void InitCandidateColumns()
        {
            _nCandidates = 0;
            _candidateColumns = new ColumnInfo[_table.Columns.Count];
            if (_rowFilter == null)
                return;
            DataColumn[] depColumns = _rowFilter.GetDependency();
            for (int i = 0; i < depColumns.Length; i++)
            {
                if (depColumns[i].Table == _table)
                {
                    _candidateColumns[depColumns[i].Ordinal] = new ColumnInfo();
                    _nCandidates++;
                }
            }
        }

        // Based on the required sorting and candidate columns settings, create a new index; Should be called only when there is no existing index to be reused
        private void CreateIndex()
        {
            Debug.Assert(_candidateColumns != null);

            if (_index == null)
            {
                if (_nCandidates == 0)
                {
                    _index = new Index(_table, _indexFields, _recordStates, null);
                    _index.AddRef();
                }
                else
                {
                    int i;
                    int lenCanColumns = _candidateColumns.Length;
                    int lenIndexDesc = _indexFields.Length;
                    bool equalsOperator = true;
                    for (i = 0; i < lenCanColumns; i++)
                    {
                        if (_candidateColumns[i] != null)
                        {
                            if (!_candidateColumns[i].equalsOperator)
                            {
                                equalsOperator = false;
                                break;
                            }
                        }
                    }

                    int j = 0;
                    for (i = 0; i < lenIndexDesc; i++)
                    {
                        ColumnInfo candidateColumn = _candidateColumns[_indexFields[i].Column.Ordinal];
                        if (candidateColumn != null)
                        {
                            candidateColumn.flag = true;
                            j++;
                        }
                    }
                    int indexNotInCandidates = lenIndexDesc - j;
                    IndexField[] ndxFields = new IndexField[_nCandidates + indexNotInCandidates];

                    if (equalsOperator)
                    {
                        j = 0;
                        for (i = 0; i < lenCanColumns; i++)
                        {
                            if (_candidateColumns[i] != null)
                            {
                                ndxFields[j++] = new IndexField(_table.Columns[i], isDescending: false);
                                _candidateColumns[i].flag = false; // this means it is processed
                            }
                        }
                        for (i = 0; i < lenIndexDesc; i++)
                        {
                            ColumnInfo canColumn = _candidateColumns[_indexFields[i].Column.Ordinal];
                            if (canColumn == null || canColumn.flag)
                            { // if sort column is not a filter col , or not processed
                                ndxFields[j++] = _indexFields[i];
                                if (canColumn != null)
                                {
                                    canColumn.flag = false;
                                }
                            }
                        }

                        for (i = 0; i < _candidateColumns.Length; i++)
                        {
                            if (_candidateColumns[i] != null)
                            {
                                _candidateColumns[i].flag = false; // same as before, it is false when it returns
                            }
                        }

                        // Debug.Assert(j == candidatesNotInIndex, "Whole ndxDesc should be filled!");

                        _index = new Index(_table, ndxFields, _recordStates, null);
                        if (!IsOperatorIn(_expression))
                        {
                            // if the expression contains an 'IN' operator, the index will not be shared
                            // therefore we do not need to index.AddRef, also table would track index consuming more memory until first write
                            _index.AddRef();
                        }


                        _matchedCandidates = _nCandidates;
                    }
                    else
                    {
                        for (i = 0; i < lenIndexDesc; i++)
                        {
                            ndxFields[i] = _indexFields[i];
                            ColumnInfo canColumn = _candidateColumns[_indexFields[i].Column.Ordinal];
                            if (canColumn != null)
                                canColumn.flag = true;
                        }
                        j = i;
                        for (i = 0; i < lenCanColumns; i++)
                        {
                            if (_candidateColumns[i] != null)
                            {
                                if (!_candidateColumns[i].flag)
                                {
                                    ndxFields[j++] = new IndexField(_table.Columns[i], isDescending: false);
                                }
                                else
                                {
                                    _candidateColumns[i].flag = false;
                                }
                            }
                        }
                        //                        Debug.Assert(j == nCandidates+indexNotInCandidates, "Whole ndxDesc should be filled!");

                        _index = new Index(_table, ndxFields, _recordStates, null);
                        _matchedCandidates = 0;
                        if (_linearExpression != _expression)
                        {
                            IndexField[] fields = _index._indexFields;
                            while (_matchedCandidates < j)
                            {
                                ColumnInfo canColumn = _candidateColumns[fields[_matchedCandidates].Column.Ordinal];
                                if (canColumn == null || canColumn.expr == null)
                                    break;
                                _matchedCandidates++;
                                if (!canColumn.equalsOperator)
                                    break;
                            }
                        }
                        for (i = 0; i < _candidateColumns.Length; i++)
                        {
                            if (_candidateColumns[i] != null)
                            {
                                _candidateColumns[i].flag = false; // same as before, it is false when it returns
                            }
                        }
                    }
                }
            }
        }


        private bool IsOperatorIn(ExpressionNode? enode)
        {
            BinaryNode? bnode = (enode as BinaryNode);
            if (null != bnode)
            {
                if (Operators.In == bnode._op ||
                    IsOperatorIn(bnode._right) ||
                    IsOperatorIn(bnode._left))
                {
                    return true;
                }
            }
            return false;
        }



        // Based on the current index and candidate columns settings, build the linear expression; Should be called only when there is atleast something for Binary Searching
        private void BuildLinearExpression()
        {
            Debug.Assert(_candidateColumns != null);

            int i;
            IndexField[] fields = _index!._indexFields;
            int lenId = fields.Length;
            Debug.Assert(_matchedCandidates > 0 && _matchedCandidates <= lenId, "BuildLinearExpression : Invalid Index");
            for (i = 0; i < _matchedCandidates; i++)
            {
                ColumnInfo canColumn = _candidateColumns[fields[i].Column.Ordinal];
                Debug.Assert(canColumn != null && canColumn.expr != null, "BuildLinearExpression : Must be a matched candidate");
                canColumn.flag = true;
            }
            //this is invalid assert, assumption was that all equals operator exists at the beginning of candidateColumns
            // but with QFE 1704, this assumption is not true anymore
            //            Debug.Assert(matchedCandidates==1 || candidateColumns[matchedCandidates-1].equalsOperator, "BuildLinearExpression : Invalid matched candidates");
            int lenCanColumns = _candidateColumns.Length;
            for (i = 0; i < lenCanColumns; i++)
            {
                if (_candidateColumns[i] != null)
                {
                    if (!_candidateColumns[i].flag)
                    {
                        if (_candidateColumns[i].expr is BinaryNode expr)
                        {
                            _linearExpression = (_linearExpression == null ? _candidateColumns[i].expr : new BinaryNode(_table, Operators.And, expr, _linearExpression));
                        }
                    }
                    else
                    {
                        _candidateColumns[i].flag = false;
                    }
                }
            }
        }

        public DataRow[] SelectRows()
        {
            bool needSorting = true;

            InitCandidateColumns();
            Debug.Assert(_candidateColumns != null);

            if (_expression is BinaryNode)
            {
                AnalyzeExpression((BinaryNode)_expression);
                if (!_candidatesForBinarySearch)
                {
                    _linearExpression = _expression;
                }
                if (_linearExpression == _expression)
                {
                    for (int i = 0; i < _candidateColumns.Length; i++)
                    {
                        if (_candidateColumns[i] != null)
                        {
                            _candidateColumns[i].equalsOperator = false;
                            _candidateColumns[i].expr = null;
                        }
                    }
                }
                else
                {
                    needSorting = !FindClosestCandidateIndex();
                }
            }
            else
            {
                _linearExpression = _expression;
            }

            if (_index == null && (_indexFields.Length > 0 || _linearExpression == _expression))
            {
                needSorting = !FindSortIndex();
            }

            if (_index == null)
            {
                CreateIndex();
                needSorting = false;
            }

            if (_index!.RecordCount == 0)
                return _table.NewRowArray(0);

            Range range;
            if (_matchedCandidates == 0)
            {
                range = new Range(0, _index.RecordCount - 1);
                Debug.Assert(!needSorting, "What are we doing here if no real reuse of this index ?");
                _linearExpression = _expression;
                return GetLinearFilteredRows(range);
            }
            else
            {
                range = GetBinaryFilteredRecords();
                if (range.Count == 0)
                    return _table.NewRowArray(0);
                if (_matchedCandidates < _nCandidates)
                {
                    BuildLinearExpression();
                }
                if (!needSorting)
                {
                    return GetLinearFilteredRows(range);
                }
                else
                {
                    _records = GetLinearFilteredRecords(range);
                    _recordCount = _records.Length;
                    if (_recordCount == 0)
                        return _table.NewRowArray(0);
                    Sort(0, _recordCount - 1);
                    return GetRows();
                }
            }
        }

        public DataRow[] GetRows()
        {
            DataRow[] newRows = _table.NewRowArray(_recordCount);
            for (int i = 0; i < newRows.Length; i++)
            {
                newRows[i] = _table._recordManager[_records![i]]!;
            }
            return newRows;
        }

        private bool AcceptRecord(int record)
        {
            DataRow? row = _table._recordManager[record];

            if (row == null)
                return true;

            DataRowVersion version = DataRowVersion.Default;
            if (row._oldRecord == record)
            {
                version = DataRowVersion.Original;
            }
            else if (row._newRecord == record)
            {
                version = DataRowVersion.Current;
            }
            else if (row._tempRecord == record)
            {
                version = DataRowVersion.Proposed;
            }

            object val = _linearExpression!.Eval(row, version);
            bool result;
            try
            {
                result = DataExpression.ToBoolean(val);
            }
            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
            {
                throw ExprException.FilterConvertion(_rowFilter!.Expression);
            }
            return result;
        }

        private int Eval(BinaryNode expr, DataRow row, DataRowVersion version)
        {
            if (expr._op == Operators.And)
            {
                int lResult = Eval((BinaryNode)expr._left, row, version);
                if (lResult != 0)
                    return lResult;
                int rResult = Eval((BinaryNode)expr._right, row, version);
                if (rResult != 0)
                    return rResult;
                return 0;
            }

            long c = 0;
            object vLeft = expr._left.Eval(row, version);
            if (expr._op != Operators.Is && expr._op != Operators.IsNot)
            {
                object vRight = expr._right.Eval(row, version);
                bool isLConst = (expr._left is ConstNode);
                bool isRConst = (expr._right is ConstNode);

                if ((vLeft == DBNull.Value) || (expr._left.IsSqlColumn && DataStorage.IsObjectSqlNull(vLeft)))
                    return -1;
                if ((vRight == DBNull.Value) || (expr._right.IsSqlColumn && DataStorage.IsObjectSqlNull(vRight)))
                    return 1;

                StorageType leftType = DataStorage.GetStorageType(vLeft.GetType());
                if (StorageType.Char == leftType)
                {
                    if ((isRConst) || (!expr._right.IsSqlColumn))
                        vRight = Convert.ToChar(vRight, _table.FormatProvider);
                    else
                        vRight = SqlConvert.ChangeType2(vRight, StorageType.Char, typeof(char), _table.FormatProvider);
                }

                StorageType rightType = DataStorage.GetStorageType(vRight.GetType());
                StorageType resultType;
                if (expr._left.IsSqlColumn || expr._right.IsSqlColumn)
                {
                    resultType = expr.ResultSqlType(leftType, rightType, isLConst, isRConst, expr._op);
                }
                else
                {
                    resultType = expr.ResultType(leftType, rightType, isLConst, isRConst, expr._op);
                }
                if (StorageType.Empty == resultType)
                {
                    expr.SetTypeMismatchError(expr._op, vLeft.GetType(), vRight.GetType());
                }

                // if comparing a Guid column value against a string literal
                // use InvariantCulture instead of DataTable.Locale because in the Danish related cultures
                // sorting a Guid as a string has different results than in Invariant and English related cultures.
                // This fix is restricted to DataTable.Select("GuidColumn = 'string literal'") types of queries
                NameNode? namedNode = null;
                System.Globalization.CompareInfo? comparer =
                    ((isLConst && !isRConst && (leftType == StorageType.String) && (rightType == StorageType.Guid) && (null != (namedNode = expr._right as NameNode)) && (namedNode._column!.DataType == typeof(Guid))) ||
                     (isRConst && !isLConst && (rightType == StorageType.String) && (leftType == StorageType.Guid) && (null != (namedNode = expr._left as NameNode)) && (namedNode._column!.DataType == typeof(Guid))))
                     ? System.Globalization.CultureInfo.InvariantCulture.CompareInfo : null;

                c = expr.BinaryCompare(vLeft, vRight, resultType, expr._op, comparer);
            }
            switch (expr._op)
            {
                case Operators.EqualTo: c = (c == 0 ? 0 : c < 0 ? -1 : 1); break;
                case Operators.GreaterThen: c = (c > 0 ? 0 : -1); break;
                case Operators.LessThen: c = (c < 0 ? 0 : 1); break;
                case Operators.GreaterOrEqual: c = (c >= 0 ? 0 : -1); break;
                case Operators.LessOrEqual: c = (c <= 0 ? 0 : 1); break;
                case Operators.Is: c = (vLeft == DBNull.Value ? 0 : -1); break;
                case Operators.IsNot: c = (vLeft != DBNull.Value ? 0 : 1); break;
                default: Debug.Assert(true, "Unsupported Binary Search Operator!"); break;
            }
            return (int)c;
        }

        private int Evaluate(int record)
        {
            Debug.Assert(_index != null && _candidateColumns != null);

            DataRow? row = _table._recordManager[record];

            if (row == null)
                return 0;

            DataRowVersion version = DataRowVersion.Default;
            if (row._oldRecord == record)
            {
                version = DataRowVersion.Original;
            }
            else if (row._newRecord == record)
            {
                version = DataRowVersion.Current;
            }
            else if (row._tempRecord == record)
            {
                version = DataRowVersion.Proposed;
            }

            IndexField[] fields = _index._indexFields;
            for (int i = 0; i < _matchedCandidates; i++)
            {
                var candidateColumn = _candidateColumns[fields[i].Column.Ordinal];
                Debug.Assert(candidateColumn != null, "How come this is not a candidate column");
                Debug.Assert(candidateColumn.expr != null, "How come there is no associated expression");
                int c = Eval(candidateColumn.expr, row, version);
                if (c != 0)
                    return fields[i].IsDescending ? -c : c;
            }
            return 0;
        }

        private int FindFirstMatchingRecord()
        {
            Debug.Assert(_index != null);

            int rec = -1;
            int lo = 0;
            int hi = _index.RecordCount - 1;
            while (lo <= hi)
            {
                int i = lo + hi >> 1;
                int recNo = _index.GetRecord(i);
                int c = Evaluate(recNo);
                if (c == 0) { rec = i; }
                if (c < 0) lo = i + 1;
                else hi = i - 1;
            }
            return rec;
        }

        private int FindLastMatchingRecord(int lo)
        {
            Debug.Assert(_index != null);

            int rec = -1;
            int hi = _index.RecordCount - 1;
            while (lo <= hi)
            {
                int i = lo + hi >> 1;
                int recNo = _index.GetRecord(i);
                int c = Evaluate(recNo);
                if (c == 0) { rec = i; }
                if (c <= 0) lo = i + 1;
                else hi = i - 1;
            }
            return rec;
        }

        private Range GetBinaryFilteredRecords()
        {
            Debug.Assert(_index != null);

            if (_matchedCandidates == 0)
            {
                return new Range(0, _index.RecordCount - 1);
            }
            Debug.Assert(_matchedCandidates <= _index._indexFields.Length, "GetBinaryFilteredRecords : Invalid Index");
            int lo = FindFirstMatchingRecord();
            if (lo == -1)
            {
                return default;
            }
            int hi = FindLastMatchingRecord(lo);
            Debug.Assert(lo <= hi, "GetBinaryFilteredRecords : Invalid Search Results");
            return new Range(lo, hi);
        }

        private int[] GetLinearFilteredRecords(Range range)
        {
            Debug.Assert(_index != null);

            if (_linearExpression == null)
            {
                int[] resultRecords = new int[range.Count];
                RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(range.Min);
                for (int i = 0; i < range.Count && iterator.MoveNext(); i++)
                {
                    resultRecords[i] = iterator.Current;
                }
                return resultRecords;
            }
            else
            {
                List<int> matchingRecords = new List<int>();
                RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(range.Min);
                for (int i = 0; i < range.Count && iterator.MoveNext(); i++)
                {
                    if (AcceptRecord(iterator.Current))
                    {
                        matchingRecords.Add(iterator.Current);
                    }
                }
                return matchingRecords.ToArray();
            }
        }

        private DataRow[] GetLinearFilteredRows(Range range)
        {
            Debug.Assert(_index != null);

            DataRow[] resultRows;
            if (_linearExpression == null)
            {
                return _index.GetRows(range);
            }

            List<DataRow> matchingRows = new List<DataRow>();
            RBTree<int>.RBTreeEnumerator iterator = _index.GetEnumerator(range.Min);
            for (int i = 0; i < range.Count && iterator.MoveNext(); i++)
            {
                if (AcceptRecord(iterator.Current))
                {
                    matchingRows.Add(_table._recordManager[iterator.Current]!);
                }
            }
            resultRows = _table.NewRowArray(matchingRows.Count);
            matchingRows.CopyTo(resultRows);
            return resultRows;
        }


        private int CompareRecords(int record1, int record2)
        {
            int lenIndexDesc = _indexFields.Length;
            for (int i = 0; i < lenIndexDesc; i++)
            {
                int c = _indexFields[i].Column.Compare(record1, record2);
                if (c != 0)
                {
                    if (_indexFields[i].IsDescending) c = -c;
                    return c;
                }
            }

            var (row1, row2) = (_table._recordManager[record1], _table._recordManager[record2]);

            long id1 = row1 is null ? 0 : row1.rowID;
            long id2 = row2 is null ? 0 : row2.rowID;
            int diff = (id1 < id2) ? -1 : ((id2 < id1) ? 1 : 0);

            // if they're two records in the same row, we need to be able to distinguish them.
            if (diff == 0 && record1 != record2 && row1 != null && row2 != null)
            {
                id1 = (int)row1.GetRecordState(record1);
                id2 = (int)row2.GetRecordState(record2);
                diff = (id1 < id2) ? -1 : ((id2 < id1) ? 1 : 0);
            }

            return diff;
        }

        private void Sort(int left, int right)
        {
            int i, j;
            int record;
            do
            {
                i = left;
                j = right;
                record = _records![i + j >> 1];
                do
                {
                    while (CompareRecords(_records[i], record) < 0) i++;
                    while (CompareRecords(_records[j], record) > 0) j--;
                    if (i <= j)
                    {
                        int r = _records[i];
                        _records[i] = _records[j];
                        _records[j] = r;
                        i++;
                        j--;
                    }
                } while (i <= j);
                if (left < j) Sort(left, j);
                left = i;
            } while (i < right);
        }
    }
}
