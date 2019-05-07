// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace System.Data
{
    /// <summary>
    /// Represents a constraint that can be enforced on one or more <see cref='System.Data.DataColumn'/> objects.
    /// </summary>
    [DefaultProperty(nameof(ConstraintName))]
    [TypeConverter(typeof(ConstraintConverter))]
    public abstract class Constraint
    {
        private string _schemaName = string.Empty;
        private bool _inCollection = false;
        private DataSet _dataSet = null;
        internal string _name = string.Empty;
        internal PropertyCollection _extendedProperties = null;

        internal Constraint() {}

        /// <summary>
        /// The name of this constraint within the <see cref='System.Data.ConstraintCollection'/>.
        /// </summary>
        [DefaultValue("")]
        public virtual string ConstraintName
        {
            get { return _name; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                if (string.IsNullOrEmpty(value) && (Table != null) && InCollection)
                {
                    throw ExceptionBuilder.NoConstraintName();
                }

                CultureInfo locale = (Table != null ? Table.Locale : CultureInfo.CurrentCulture);
                if (string.Compare(_name, value, true, locale) != 0)
                {
                    if ((Table != null) && InCollection)
                    {
                        Table.Constraints.RegisterName(value);
                        if (_name.Length != 0)
                            Table.Constraints.UnregisterName(_name);
                    }
                    _name = value;
                }
                else if (string.Compare(_name, value, false, locale) != 0)
                {
                    _name = value;
                }
            }
        }

        internal string SchemaName
        {
            get { return string.IsNullOrEmpty(_schemaName) ? ConstraintName : _schemaName; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _schemaName = value;
                }
            }
        }

        internal virtual bool InCollection
        {
            get { return _inCollection; }
            set
            {
                _inCollection = value;
                _dataSet = value ? Table.DataSet : null;
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataTable'/> to which the constraint applies.
        /// </summary>
        public abstract DataTable Table { get; }

        /// <summary>
        /// Gets the collection of customized user information.
        /// </summary>
        [Browsable(false)]
        public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

        internal abstract bool ContainsColumn(DataColumn column);
        internal abstract bool CanEnableConstraint();

        internal abstract Constraint Clone(DataSet destination);
        internal abstract Constraint Clone(DataSet destination, bool ignoreNSforTableLookup);

        internal void CheckConstraint()
        {
            if (!CanEnableConstraint())
            {
                throw ExceptionBuilder.ConstraintViolation(ConstraintName);
            }
        }

        internal abstract void CheckCanAddToCollection(ConstraintCollection constraint);
        internal abstract bool CanBeRemovedFromCollection(ConstraintCollection constraint, bool fThrowException);

        internal abstract void CheckConstraint(DataRow row, DataRowAction action);
        internal abstract void CheckState();

        protected void CheckStateForProperty()
        {
            try
            {
                CheckState();
            }
            catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
            {
                throw ExceptionBuilder.BadObjectPropertyAccess(e.Message);
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataSet'/> to which this constraint belongs.
        /// </summary>
        [CLSCompliant(false)]
        protected virtual DataSet _DataSet => _dataSet;

        /// <summary>
        /// Sets the constraint's <see cref='System.Data.DataSet'/>.
        /// </summary>
        protected internal void SetDataSet(DataSet dataSet) => _dataSet = dataSet;

        internal abstract bool IsConstraintViolated();

        public override string ToString() => ConstraintName;
    }
}
