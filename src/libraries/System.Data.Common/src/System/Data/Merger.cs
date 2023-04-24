// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class Merger
    {
        private readonly DataSet? _dataSet;
        private readonly DataTable? _dataTable;
        private readonly bool _preserveChanges;
        private readonly MissingSchemaAction _missingSchemaAction;
        private readonly bool _isStandAlonetable;
        private bool _IgnoreNSforTableLookup; // Everett Behavior : SQL BU DT 370850

        internal Merger(DataSet dataSet, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            _dataSet = dataSet;
            _preserveChanges = preserveChanges;

            // map AddWithKey -> Add
            _missingSchemaAction = missingSchemaAction == MissingSchemaAction.AddWithKey ?
                MissingSchemaAction.Add :
                missingSchemaAction;
        }

        internal Merger(DataTable dataTable, bool preserveChanges, MissingSchemaAction missingSchemaAction)
        {
            _isStandAlonetable = true;
            _dataTable = dataTable;
            _preserveChanges = preserveChanges;

            // map AddWithKey -> Add
            _missingSchemaAction = missingSchemaAction == MissingSchemaAction.AddWithKey ?
                MissingSchemaAction.Add :
                missingSchemaAction;
        }

        internal void MergeDataSet(DataSet source)
        {
            Debug.Assert(_dataSet != null);

            if (source == _dataSet) return;  //somebody is doing an 'automerge'
            bool fEnforce = _dataSet.EnforceConstraints;
            _dataSet.EnforceConstraints = false;
            _IgnoreNSforTableLookup = (_dataSet._namespaceURI != source._namespaceURI); // if two DataSets have different
            // Namespaces, ignore NS for table lookups as we won't be able to find the right tables which inherits its NS

            List<DataColumn>? existingColumns = null; // need to cache existing columns

            if (MissingSchemaAction.Add == _missingSchemaAction)
            {
                existingColumns = new List<DataColumn>(); // need to cache existing columns
                foreach (DataTable dt in _dataSet.Tables)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        existingColumns.Add(dc);
                    }
                }
            }

            for (int i = 0; i < source.Tables.Count; i++)
            {
                MergeTableData(source.Tables[i]); // since column expression might have dependency on relation, we do not set
                //column expression at this point. We need to set it after adding relations
            }

            if (MissingSchemaAction.Ignore != _missingSchemaAction)
            {
                // Add all independent constraints
                MergeConstraints(source);

                // Add all relationships
                for (int i = 0; i < source.Relations.Count; i++)
                {
                    MergeRelation(source.Relations[i]);
                }
            }

            if (MissingSchemaAction.Add == _missingSchemaAction)
            {
                // for which other options we should add expressions also?
                foreach (DataTable sourceTable in source.Tables)
                {
                    DataTable targetTable;
                    if (_IgnoreNSforTableLookup)
                    {
                        targetTable = _dataSet.Tables[sourceTable.TableName]!;
                    }
                    else
                    {
                        targetTable = _dataSet.Tables[sourceTable.TableName, sourceTable.Namespace]!; // we know that target table won't be null since MissingSchemaAction is Add , we have already added it!
                    }

                    foreach (DataColumn dc in sourceTable.Columns)
                    {
                        // Should we overwrite the previous expression column? No, refer to spec, if it is new column we need to add the schema
                        if (dc.Computed)
                        {
                            DataColumn targetColumn = targetTable.Columns[dc.ColumnName]!;
                            if (!existingColumns!.Contains(targetColumn))
                            {
                                targetColumn.CopyExpressionFrom(dc);
                            }
                        }
                    }
                }
            }

            MergeExtendedProperties(source.ExtendedProperties, _dataSet.ExtendedProperties);
            foreach (DataTable dt in _dataSet.Tables)
            {
                dt.EvaluateExpressions();
            }
            _dataSet.EnforceConstraints = fEnforce;
        }

        internal void MergeTable(DataTable src)
        {
            bool fEnforce = false;
            if (!_isStandAlonetable)
            {
                if (src.DataSet == _dataSet) return; //somebody is doing an 'automerge'
                fEnforce = _dataSet!.EnforceConstraints;
                _dataSet.EnforceConstraints = false;
            }
            else
            {
                if (src == _dataTable) return; //somebody is doing an 'automerge'
                _dataTable!.SuspendEnforceConstraints = true;
            }

            if (_dataSet != null)
            {
                // this is ds.Merge
                // if source does not have a DS, or if NS of both DS does not match, ignore the NS
                if (src.DataSet == null || src.DataSet._namespaceURI != _dataSet._namespaceURI)
                {
                    _IgnoreNSforTableLookup = true;
                }
            }
            else
            {
                // this is dt.Merge
                if (_dataTable!.DataSet == null || src.DataSet == null || src.DataSet._namespaceURI != _dataTable.DataSet._namespaceURI)
                {
                    _IgnoreNSforTableLookup = true;
                }
            }

            MergeTableData(src);

            DataTable? dt = _dataTable;
            if (dt == null && _dataSet != null)
            {
                dt = _IgnoreNSforTableLookup ?
                    _dataSet.Tables[src.TableName] :
                    _dataSet.Tables[src.TableName, src.Namespace];
            }

            dt?.EvaluateExpressions();

            if (!_isStandAlonetable)
            {
                _dataSet!.EnforceConstraints = fEnforce;
            }
            else
            {
                _dataTable!.SuspendEnforceConstraints = false;
                try
                {
                    if (_dataTable.EnforceConstraints)
                    {
                        _dataTable.EnableConstraints();
                    }
                }
                catch (ConstraintException)
                {
                    if (_dataTable.DataSet != null)
                    {
                        _dataTable.DataSet.EnforceConstraints = false;
                    }
                    throw;
                }
            }
        }

        private void MergeTable(DataTable src, DataTable dst)
        {
            int rowsCount = src.Rows.Count;
            bool wasEmpty = dst.Rows.Count == 0;
            if (0 < rowsCount)
            {
                Index? ndxSearch = null;
                DataKey key = default(DataKey);
                dst.SuspendIndexEvents();
                try
                {
                    if (!wasEmpty && dst._primaryKey != null)
                    {
                        key = GetSrcKey(src, dst);
                        if (key.HasValue)
                        {
                            ndxSearch = dst._primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows | DataViewRowState.Added);
                        }
                    }

                    // this improves performance by iterating over the rows instead of computing their position
                    foreach (DataRow sourceRow in src.Rows)
                    {
                        DataRow? targetRow = null;
                        if (ndxSearch != null)
                        {
                            targetRow = dst.FindMergeTarget(sourceRow, key, ndxSearch);
                        }
                        dst.MergeRow(sourceRow, targetRow, _preserveChanges, ndxSearch);
                    }
                }
                finally
                {
                    dst.RestoreIndexEvents(true);
                }
            }
            MergeExtendedProperties(src.ExtendedProperties, dst.ExtendedProperties);
        }

        internal void MergeRows(DataRow[] rows)
        {
            Debug.Assert(_dataSet != null);

            DataTable? src = null;
            DataTable? dst = null;
            DataKey key = default(DataKey);
            Index? ndxSearch = null;

            bool fEnforce = _dataSet.EnforceConstraints;
            _dataSet.EnforceConstraints = false;

            for (int i = 0; i < rows.Length; i++)
            {
                DataRow row = rows[i];

                if (row == null)
                {
                    throw ExceptionBuilder.ArgumentNull($"{nameof(rows)}[{i}]");
                }
                if (row.Table == null)
                {
                    throw ExceptionBuilder.ArgumentNull($"{nameof(rows)}[{i}].{nameof(DataRow.Table)}");
                }

                //somebody is doing an 'automerge'
                if (row.Table.DataSet == _dataSet)
                {
                    continue;
                }

                if (src != row.Table)
                {                     // row.Table changed from prev. row.
                    src = row.Table;
                    dst = MergeSchema(row.Table);
                    if (dst == null)
                    {
                        Debug.Assert(MissingSchemaAction.Ignore == _missingSchemaAction, "MergeSchema failed");
                        _dataSet.EnforceConstraints = fEnforce;
                        return;
                    }
                    if (dst._primaryKey != null)
                    {
                        key = GetSrcKey(src, dst);
                    }
                    if (key.HasValue)
                    {
                        // Getting our own copy instead. ndxSearch = dst.primaryKey.Key.GetSortIndex();
                        // IMO, Better would be to reuse index
                        // ndxSearch = dst.primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows | DataViewRowState.Added );
                        ndxSearch?.RemoveRef();
                        ndxSearch = new Index(dst, dst._primaryKey!.Key.GetIndexDesc(), DataViewRowState.OriginalRows | DataViewRowState.Added, null);
                        ndxSearch.AddRef(); // need to addref twice, otherwise it will be collected
                        ndxSearch.AddRef(); // in past first adref was done in const
                    }
                }

                if (row._newRecord == -1 && row._oldRecord == -1)
                {
                    continue;
                }

                DataRow? targetRow = null;
                if (0 < dst!.Rows.Count && ndxSearch != null)
                {
                    targetRow = dst.FindMergeTarget(row, key, ndxSearch);
                }

                targetRow = dst.MergeRow(row, targetRow, _preserveChanges, ndxSearch);

                if (targetRow.Table._dependentColumns != null && targetRow.Table._dependentColumns.Count > 0)
                {
                    targetRow.Table.EvaluateExpressions(targetRow, DataRowAction.Change, null);
                }
            }
            ndxSearch?.RemoveRef();

            _dataSet.EnforceConstraints = fEnforce;
        }

        private DataTable? MergeSchema(DataTable table)
        {
            DataTable? targetTable = null;
            if (!_isStandAlonetable)
            {
                if (_dataSet!.Tables.Contains(table.TableName, true))
                {
                    if (_IgnoreNSforTableLookup)
                    {
                        targetTable = _dataSet.Tables[table.TableName];
                    }
                    else
                    {
                        targetTable = _dataSet.Tables[table.TableName, table.Namespace];
                    }
                }
            }
            else
            {
                targetTable = _dataTable;
            }

            if (targetTable == null)
            {
                // in case of standalone table, we make sure that targetTable is not null, so if this check passes, it will be when it is called via detaset
                if (MissingSchemaAction.Add == _missingSchemaAction)
                {
                    targetTable = table.Clone(table.DataSet); // if we are here mainly we are called from DataSet.Merge at this point we don't set
                    //expression columns, since it might have refer to other columns via relation, so it won't find the table and we get exception;
                    // do it after adding relations.
                    _dataSet!.Tables.Add(targetTable);
                }
                else if (MissingSchemaAction.Error == _missingSchemaAction)
                {
                    throw ExceptionBuilder.MergeMissingDefinition(table.TableName);
                }
            }
            else
            {
                if (MissingSchemaAction.Ignore != _missingSchemaAction)
                {
                    // Do the columns
                    int oldCount = targetTable.Columns.Count;
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        DataColumn src = table.Columns[i];
                        DataColumn? dest = (targetTable.Columns.Contains(src.ColumnName, true)) ? targetTable.Columns[src.ColumnName] : null;
                        if (dest == null)
                        {
                            if (MissingSchemaAction.Add == _missingSchemaAction)
                            {
                                dest = src.Clone();
                                targetTable.Columns.Add(dest);
                            }
                            else
                            {
                                if (!_isStandAlonetable)
                                {
                                    _dataSet!.RaiseMergeFailed(targetTable, SR.Format(SR.DataMerge_MissingColumnDefinition, table.TableName, src.ColumnName), _missingSchemaAction);
                                }
                                else
                                {
                                    throw ExceptionBuilder.MergeFailed(SR.Format(SR.DataMerge_MissingColumnDefinition, table.TableName, src.ColumnName));
                                }
                            }
                        }
                        else
                        {
                            if (dest.DataType != src.DataType ||
                                ((dest.DataType == typeof(DateTime)) && (dest.DateTimeMode != src.DateTimeMode) && ((dest.DateTimeMode & src.DateTimeMode) != DataSetDateTime.Unspecified)))
                            {
                                if (!_isStandAlonetable)
                                    _dataSet!.RaiseMergeFailed(targetTable, SR.Format(SR.DataMerge_DataTypeMismatch, src.ColumnName), MissingSchemaAction.Error);
                                else
                                    throw ExceptionBuilder.MergeFailed(SR.Format(SR.DataMerge_DataTypeMismatch, src.ColumnName));
                            }

                            MergeExtendedProperties(src.ExtendedProperties, dest.ExtendedProperties);
                        }
                    }

                    // Set DataExpression
                    if (_isStandAlonetable)
                    {
                        for (int i = oldCount; i < targetTable.Columns.Count; i++)
                        {
                            targetTable.Columns[i].CopyExpressionFrom(table.Columns[targetTable.Columns[i].ColumnName]!);
                        }
                    }

                    // check the PrimaryKey
                    DataColumn[] targetPKey = targetTable.PrimaryKey;
                    DataColumn[] tablePKey = table.PrimaryKey;
                    if (targetPKey.Length != tablePKey.Length)
                    {
                        // special case when the target table does not have the PrimaryKey

                        if (targetPKey.Length == 0)
                        {
                            DataColumn[] key = new DataColumn[tablePKey.Length];
                            for (int i = 0; i < tablePKey.Length; i++)
                            {
                                key[i] = targetTable.Columns[tablePKey[i].ColumnName]!;
                            }
                            targetTable.PrimaryKey = key;
                        }
                        else if (tablePKey.Length != 0)
                        {
                            _dataSet!.RaiseMergeFailed(targetTable, SR.DataMerge_PrimaryKeyMismatch, _missingSchemaAction);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < targetPKey.Length; i++)
                        {
                            if (string.Compare(targetPKey[i].ColumnName, tablePKey[i].ColumnName, false, targetTable.Locale) != 0)
                            {
                                _dataSet!.RaiseMergeFailed(table,
                                    SR.Format(SR.DataMerge_PrimaryKeyColumnsMismatch, targetPKey[i].ColumnName, tablePKey[i].ColumnName),
                                    _missingSchemaAction);
                            }
                        }
                    }
                }

                MergeExtendedProperties(table.ExtendedProperties, targetTable.ExtendedProperties);
            }

            return targetTable;
        }

        private void MergeTableData(DataTable src)
        {
            DataTable? dest = MergeSchema(src);
            if (dest == null) return;

            dest.MergingData = true;
            try
            {
                MergeTable(src, dest);
            }
            finally
            {
                dest.MergingData = false;
            }
        }

        private void MergeConstraints(DataSet source)
        {
            for (int i = 0; i < source.Tables.Count; i++)
            {
                MergeConstraints(source.Tables[i]);
            }
        }

        private void MergeConstraints(DataTable table)
        {
            Debug.Assert(_dataSet != null);

            // Merge constraints
            for (int i = 0; i < table.Constraints.Count; i++)
            {
                Constraint src = table.Constraints[i];
                Constraint? dest = src.Clone(_dataSet, _IgnoreNSforTableLookup);

                if (dest == null)
                {
                    _dataSet.RaiseMergeFailed(table,
                        SR.Format(SR.DataMerge_MissingConstraint, src.GetType().FullName, src.ConstraintName),
                        _missingSchemaAction
                    );
                }
                else
                {
                    Constraint? cons = dest.Table!.Constraints.FindConstraint(dest);
                    if (cons == null)
                    {
                        if (MissingSchemaAction.Add == _missingSchemaAction)
                        {
                            try
                            {
                                // try to keep the original name
                                dest.Table.Constraints.Add(dest);
                            }
                            catch (DuplicateNameException)
                            {
                                // if fail, assume default name
                                dest.ConstraintName = string.Empty;
                                dest.Table.Constraints.Add(dest);
                            }
                        }
                        else if (MissingSchemaAction.Error == _missingSchemaAction)
                        {
                            _dataSet.RaiseMergeFailed(table,
                                SR.Format(SR.DataMerge_MissingConstraint, src.GetType().FullName, src.ConstraintName),
                                _missingSchemaAction
                            );
                        }
                    }
                    else
                    {
                        MergeExtendedProperties(src.ExtendedProperties, cons.ExtendedProperties);
                    }
                }
            }
        }

        private void MergeRelation(DataRelation relation)
        {
            Debug.Assert(_dataSet != null);
            Debug.Assert(MissingSchemaAction.Error == _missingSchemaAction ||
                         MissingSchemaAction.Add == _missingSchemaAction,
                         "Unexpected value of MissingSchemaAction parameter : " + _missingSchemaAction.ToString());
            DataRelation? destRelation = null;

            // try to find given relation in this dataSet

            int iDest = _dataSet.Relations.InternalIndexOf(relation.RelationName);

            if (iDest >= 0)
            {
                // check the columns and Relation properties..
                destRelation = _dataSet.Relations[iDest];

                if (relation.ParentKey.ColumnsReference.Length != destRelation.ParentKey.ColumnsReference.Length)
                {
                    _dataSet.RaiseMergeFailed(null,
                        SR.Format(SR.DataMerge_MissingDefinition, relation.RelationName),
                        _missingSchemaAction);
                }
                for (int i = 0; i < relation.ParentKey.ColumnsReference.Length; i++)
                {
                    DataColumn dest = destRelation.ParentKey.ColumnsReference[i];
                    DataColumn src = relation.ParentKey.ColumnsReference[i];

                    if (0 != string.Compare(dest.ColumnName, src.ColumnName, false, dest.Table!.Locale))
                    {
                        _dataSet.RaiseMergeFailed(null,
                            SR.Format(SR.DataMerge_ReltionKeyColumnsMismatch, relation.RelationName),
                            _missingSchemaAction);
                    }

                    dest = destRelation.ChildKey.ColumnsReference[i];
                    src = relation.ChildKey.ColumnsReference[i];

                    if (0 != string.Compare(dest.ColumnName, src.ColumnName, false, dest.Table!.Locale))
                    {
                        _dataSet.RaiseMergeFailed(null,
                            SR.Format(SR.DataMerge_ReltionKeyColumnsMismatch, relation.RelationName),
                            _missingSchemaAction);
                    }
                }
            }
            else
            {
                if (MissingSchemaAction.Add == _missingSchemaAction)
                {
                    // create identical realtion in the current dataset
                    DataTable parent = _IgnoreNSforTableLookup ?
                        _dataSet.Tables[relation.ParentTable.TableName]! :
                        _dataSet.Tables[relation.ParentTable.TableName, relation.ParentTable.Namespace]!;

                    DataTable child = _IgnoreNSforTableLookup ?
                        _dataSet.Tables[relation.ChildTable.TableName]! :
                        _dataSet.Tables[relation.ChildTable.TableName, relation.ChildTable.Namespace]!;

                    DataColumn[] parentColumns = new DataColumn[relation.ParentKey.ColumnsReference.Length];
                    DataColumn[] childColumns = new DataColumn[relation.ParentKey.ColumnsReference.Length];
                    for (int i = 0; i < relation.ParentKey.ColumnsReference.Length; i++)
                    {
                        parentColumns[i] = parent.Columns[relation.ParentKey.ColumnsReference[i].ColumnName]!;
                        childColumns[i] = child.Columns[relation.ChildKey.ColumnsReference[i].ColumnName]!;
                    }
                    try
                    {
                        destRelation = new DataRelation(relation.RelationName, parentColumns, childColumns, relation._createConstraints);
                        destRelation.Nested = relation.Nested;
                        _dataSet.Relations.Add(destRelation);
                    }
                    catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
                    {
                        ExceptionBuilder.TraceExceptionForCapture(e);
                        _dataSet.RaiseMergeFailed(null, e.Message, _missingSchemaAction);
                        // TODO: destRelation may be null, causing an NRE below
                    }
                }
                else
                {
                    Debug.Assert(MissingSchemaAction.Error == _missingSchemaAction, $"Unexpected value of MissingSchemaAction parameter : {_missingSchemaAction}");
                    throw ExceptionBuilder.MergeMissingDefinition(relation.RelationName);
                }
            }

            // TODO: destRelation may be null, see comment above
            MergeExtendedProperties(relation.ExtendedProperties, destRelation!.ExtendedProperties);
        }

        private void MergeExtendedProperties(PropertyCollection src, PropertyCollection dst)
        {
            if (MissingSchemaAction.Ignore == _missingSchemaAction)
            {
                return;
            }

            IDictionaryEnumerator srcDE = src.GetEnumerator();
            while (srcDE.MoveNext())
            {
                if (!_preserveChanges || dst[srcDE.Key] == null)
                {
                    dst[srcDE.Key] = srcDE.Value;
                }
            }
        }

        private static DataKey GetSrcKey(DataTable src, DataTable dst)
        {
            if (src._primaryKey != null)
            {
                return src._primaryKey.Key;
            }

            DataKey key = default(DataKey);
            if (dst._primaryKey != null)
            {
                DataColumn[] dstColumns = dst._primaryKey.Key.ColumnsReference;
                DataColumn[] srcColumns = new DataColumn[dstColumns.Length];
                for (int j = 0; j < dstColumns.Length; j++)
                {
                    srcColumns[j] = src.Columns[dstColumns[j].ColumnName]!;
                }

                key = new DataKey(srcColumns, false); // DataKey will take ownership of srcColumns
            }

            return key;
        }
    }
}
