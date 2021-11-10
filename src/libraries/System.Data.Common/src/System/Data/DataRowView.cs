// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    public class DataRowView : ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
    {
        private readonly DataView _dataView;
        private readonly DataRow _row;
        private bool _delayBeginEdit;

        private static readonly PropertyDescriptorCollection s_zeroPropertyDescriptorCollection = new PropertyDescriptorCollection(null);

        internal DataRowView(DataView dataView, DataRow row)
        {
            _dataView = dataView;
            _row = row;
        }

        /// <remarks>
        /// Checks for same reference instead of equivalent <see cref="DataView"/> or <see cref="Row"/>.
        ///
        /// Necessary for ListChanged event handlers to use data structures that use the default to
        /// <see cref="object.Equals(object)"/> instead of <see cref="object.ReferenceEquals"/>
        /// to understand if they need to add a <see cref="PropertyChanged"/> event handler.
        /// </remarks>
        /// <returns><see cref="object.ReferenceEquals"/></returns>
        public override bool Equals(object? other) => ReferenceEquals(this, other);

        /// <returns>Hashcode of <see cref="Row"/></returns>
        public override int GetHashCode()
        {
            // Everett compatability, must return hashcode for DataRow
            // this does prevent using this object in collections like Hashtable
            // which use the hashcode as an immutable value to identify this object
            // user could/should have used the DataRow property instead of the hashcode
            return Row.GetHashCode();
        }

        public DataView DataView => _dataView;

        internal int ObjectID => _row._objectID;

        /// <summary>Gets or sets a value in specified column.</summary>
        /// <param name="ndx">Specified column index.</param>
        /// <remarks>Uses either <see cref="DataRowVersion.Default"/> or <see cref="DataRowVersion.Original"/> to access <see cref="Row"/></remarks>
        /// <exception cref="DataException"><see cref="System.Data.DataView.get_AllowEdit"/> when setting a value.</exception>
        /// <exception cref="IndexOutOfRangeException"><see cref="DataColumnCollection.get_Item(int)"/></exception>
        [AllowNull]
        public object this[int ndx]
        {
            get { return Row[ndx, RowVersionDefault]; }
            set
            {
                if (!_dataView.AllowEdit && !IsNew)
                {
                    throw ExceptionBuilder.CanNotEdit();
                }
                SetColumnValue(_dataView.Table!.Columns[ndx], value);
            }
        }

        /// <summary>Gets the specified column value or related child view or sets a value in specified column.</summary>
        /// <param name="property">Specified column or relation name when getting.  Specified column name when setting.</param>
        /// <exception cref="ArgumentException"><see cref="DataColumnCollection.get_Item(string)"/> when <paramref name="property"/> is ambiguous.</exception>
        /// <exception cref="ArgumentException">Unmatched <paramref name="property"/> when getting a value.</exception>
        /// <exception cref="DataException">Unmatched <paramref name="property"/> when setting a value.</exception>
        /// <exception cref="DataException"><see cref="System.Data.DataView.get_AllowEdit"/> when setting a value.</exception>
        [AllowNull]
        public object this[string property]
        {
            get
            {
                DataColumn? column = _dataView.Table!.Columns[property];
                if (null != column)
                {
                    return Row[column, RowVersionDefault];
                }
                else if (_dataView.Table.DataSet != null && _dataView.Table.DataSet.Relations.Contains(property))
                {
                    return CreateChildView(property);
                }
                throw ExceptionBuilder.PropertyNotFound(property, _dataView.Table.TableName);
            }
            set
            {
                DataColumn? column = _dataView.Table!.Columns[property];
                if (null == column)
                {
                    throw ExceptionBuilder.SetFailed(property);
                }
                if (!_dataView.AllowEdit && !IsNew)
                {
                    throw ExceptionBuilder.CanNotEdit();
                }
                SetColumnValue(column, value);
            }
        }

        // IDataErrorInfo stuff
        string IDataErrorInfo.this[string colName] => Row.GetColumnError(colName);

        string IDataErrorInfo.Error => Row.RowError;

        /// <summary>
        /// Gets the current version description of the <see cref="DataRow"/>
        /// in relation to <see cref="System.Data.DataView.get_RowStateFilter"/>
        /// </summary>
        /// <returns>Either <see cref="DataRowVersion.Current"/> or <see cref="DataRowVersion.Original"/></returns>
        public DataRowVersion RowVersion => (RowVersionDefault & ~DataRowVersion.Proposed);

        /// <returns>Either <see cref="DataRowVersion.Default"/> or <see cref="DataRowVersion.Original"/></returns>
        private DataRowVersion RowVersionDefault => Row.GetDefaultRowVersion(_dataView.RowStateFilter);

        internal int GetRecord() => Row.GetRecordFromVersion(RowVersionDefault);

        internal bool HasRecord() => Row.HasVersion(RowVersionDefault);

        internal object GetColumnValue(DataColumn column) => Row[column, RowVersionDefault];

        internal void SetColumnValue(DataColumn column, object? value)
        {
            if (_delayBeginEdit)
            {
                _delayBeginEdit = false;
                Row.BeginEdit();
            }
            if (DataRowVersion.Original == RowVersionDefault)
            {
                throw ExceptionBuilder.SetFailed(column.ColumnName);
            }
            Row[column] = value;
        }

        /// <summary>
        /// Returns a <see cref="System.Data.DataView"/>
        /// for the child <see cref="System.Data.DataTable"/>
        /// with the specified <see cref="System.Data.DataRelation"/>.
        /// </summary>
        /// <param name="relation">Specified <see cref="System.Data.DataRelation"/>.</param>
        /// <param name="followParent">The parent object.</param>
        /// <exception cref="ArgumentException">null or mismatch between <paramref name="relation"/> and <see cref="System.Data.DataView.get_Table"/>.</exception>
        public DataView CreateChildView(DataRelation relation, bool followParent)
        {
            if (relation == null || relation.ParentKey.Table != DataView.Table)
            {
                throw ExceptionBuilder.CreateChildView();
            }

            RelatedView childView;
            if (!followParent)
            {
                int record = GetRecord();
                object[] values = relation.ParentKey.GetKeyValues(record);
                childView = new RelatedView(relation.ChildColumnsReference, values);
            }
            else
            {
                childView = new RelatedView(this, relation.ParentKey, relation.ChildColumnsReference);
            }

            childView.SetIndex("", DataViewRowState.CurrentRows, null); // finish construction via RelatedView.SetIndex
            childView.SetDataViewManager(DataView.DataViewManager);
            return childView;
        }

        public DataView CreateChildView(DataRelation relation) =>
            CreateChildView(relation, followParent: false);

        /// <summary><see cref="CreateChildView(DataRelation)"/></summary>
        /// <param name="relationName">Specified <see cref="System.Data.DataRelation"/> name.</param>
        /// <param name="followParent">The parent object.</param>
        /// <exception cref="ArgumentException">Unmatched <paramref name="relationName"/>.</exception>
        public DataView CreateChildView(string relationName, bool followParent) =>
            CreateChildView(DataView.Table!.ChildRelations[relationName]!, followParent);

        public DataView CreateChildView(string relationName) =>
            CreateChildView(relationName, followParent: false);

        public DataRow Row => _row;

        public void BeginEdit() => _delayBeginEdit = true;

        public void CancelEdit()
        {
            DataRow tmpRow = Row;
            if (IsNew)
            {
                _dataView.FinishAddNew(false);
            }
            else
            {
                tmpRow.CancelEdit();
            }
            _delayBeginEdit = false;
        }

        public void EndEdit()
        {
            if (IsNew)
            {
                _dataView.FinishAddNew(true);
            }
            else
            {
                Row.EndEdit();
            }
            _delayBeginEdit = false;
        }

        public bool IsNew => (_row == _dataView._addNewRow);

        public bool IsEdit =>
            Row.HasVersion(DataRowVersion.Proposed) ||  // It was edited or
            _delayBeginEdit;                            // DataRowView.BegingEdit() was called, but not edited yet.

        public void Delete() => _dataView.Delete(Row);

        // When the PropertyChanged event happens, it must happen on the same DataRowView reference.
        // This is so a generic event handler like Windows Presentation Foundation can redirect as appropriate.
        // Having DataView.Equals is not sufficient for WPF, because two different instances may be equal but not equivalent.
        // For DataRowView, if two instances are equal then they are equivalent.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Do not try catch, we would mask users bugs. if they throw we would catch
        internal void RaisePropertyChangedEvent(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #region ICustomTypeDescriptor
        AttributeCollection ICustomTypeDescriptor.GetAttributes() => new AttributeCollection(null);
        string? ICustomTypeDescriptor.GetClassName() => null;
        string? ICustomTypeDescriptor.GetComponentName() => null;

        [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        TypeConverter ICustomTypeDescriptor.GetConverter() => null!;

        [RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;

        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;

        [RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
        object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => new EventDescriptorCollection(null);

        [RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => new EventDescriptorCollection(null);

        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => ((ICustomTypeDescriptor)this).GetProperties(null);

        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) =>
            (_dataView.Table != null ? _dataView.Table.GetPropertyDescriptorCollection(attributes) : s_zeroPropertyDescriptorCollection);

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;
        #endregion
    }
}
