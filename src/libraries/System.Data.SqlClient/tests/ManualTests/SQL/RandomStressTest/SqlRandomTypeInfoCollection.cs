// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// Defines a collection of types to be used by the test. Tests can start with CreateSql2005Collection or 
    /// CreateSql2008Collection and add/remove types, as needed.
    /// </summary>
    public sealed class SqlRandomTypeInfoCollection : System.Collections.ObjectModel.KeyedCollection<SqlDbType, SqlRandomTypeInfo>
    {
        private static readonly SqlRandomTypeInfo[] s_sql2005Types =
        {
            // var types
            new SqlVarBinaryTypeInfo(),
            new SqlVarCharTypeInfo(),
            new SqlNVarCharTypeInfo(),

            // integer data types
            new SqlBigIntTypeInfo(),
            new SqlIntTypeInfo(),
            new SqlSmallIntTypeInfo(),
            new SqlTinyIntTypeInfo(),

            // fixed length blobs
            new SqlCharTypeInfo(),
            new SqlNCharTypeInfo(),
            new SqlBinaryTypeInfo(),

            // large blobs
            new SqlTextTypeInfo(),
            new SqlNTextTypeInfo(),
            new SqlImageTypeInfo(),

            // bit
            new SqlBitTypeInfo(),

            // decimal
            new SqlDecimalTypeInfo(),

            // money types
            new SqlMoneyTypeInfo(),
            new SqlSmallMoneyTypeInfo(),

            // float types
            new SqRealTypeInfo(),
            new SqFloatTypeInfo(),

            // typestamp (== rowversion)
            new SqlRowVersionTypeInfo(),

            // unique identifier (== guid)
            new SqlUniqueIdentifierTypeInfo(),

            // date/time types
            new SqlDateTimeTypeInfo(),
            new SqlSmallDateTimeTypeInfo(),

            // variant
            new SqlVariantTypeInfo(),

            // xml
            new SqlXmlTypeInfo(),
        };

        // types added in SQL 2008
        private static readonly SqlRandomTypeInfo[] s_newInSql2008Types =
        {
            // date/time types
            new SqlDateTypeInfo(),
            new SqlDateTime2TypeInfo(),
            new SqlDateTimeOffsetTypeInfo(),
            new SqlTimeTypeInfo(),
        };

        // reset it each time collection is modified
        private IList<SqlRandomTypeInfo> _sparseColumns = null;

        public IList<SqlRandomTypeInfo> SparseColumns
        {
            get
            {
                if (_sparseColumns == null)
                {
                    // rebuild it
                    var sparseColumns = this.Where(t => t.CanBeSparseColumn).ToArray();
                    _sparseColumns = new ReadOnlyCollection<SqlRandomTypeInfo>(sparseColumns);
                }

                return _sparseColumns;
            }
        }

        public SqlRandomTypeInfoCollection(params SqlRandomTypeInfo[] typeSet1)
            : this(typeSet1, null)
        { }

        protected override void ClearItems()
        {
            _sparseColumns = null;
            base.ClearItems();
        }

        protected override void InsertItem(int index, SqlRandomTypeInfo item)
        {
            _sparseColumns = null;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            _sparseColumns = null;
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, SqlRandomTypeInfo item)
        {
            _sparseColumns = null;
            base.SetItem(index, item);
        }

        /// <summary>
        /// helper c-tor to fill one or two type sets
        /// </summary>
        private SqlRandomTypeInfoCollection(SqlRandomTypeInfo[] typeSet1, SqlRandomTypeInfo[] typeSet2)
        {
            if (typeSet1 != null)
            {
                AddRange(typeSet1);
            }
            if (typeSet2 != null)
            {
                AddRange(typeSet2);
            }
        }

        protected override SqlDbType GetKeyForItem(SqlRandomTypeInfo item)
        {
            return item.Type;
        }

        public void AddRange(SqlRandomTypeInfo[] types)
        {
            for (int i = 0; i < types.Length; i++)
                Add(types[i]);
        }

        /// <summary>
        /// creates a collection of types supported in SQL 2005
        /// </summary>
        public static SqlRandomTypeInfoCollection CreateSql2005Collection()
        {
            return new SqlRandomTypeInfoCollection(s_sql2005Types);
        }

        /// <summary>
        /// creates a collection of types supported in SQL 2005 and 2008
        /// </summary>
        public static SqlRandomTypeInfoCollection CreateSql2008Collection()
        {
            return new SqlRandomTypeInfoCollection(s_sql2005Types, s_newInSql2008Types);
        }

        /// <summary>
        /// returns random type info
        /// </summary>
        public SqlRandomTypeInfo Next(SqlRandomizer rand)
        {
            return base[rand.NextIntInclusive(0, maxValueInclusive: Count - 1)];
        }

        /// <summary>
        /// returns random type info from the columns that can be sparse
        /// </summary>
        public SqlRandomTypeInfo NextSparse(SqlRandomizer rand)
        {
            var sparseColumns = SparseColumns;
            return sparseColumns[rand.NextIntInclusive(0, maxValueInclusive: sparseColumns.Count - 1)];
        }
    }
}
