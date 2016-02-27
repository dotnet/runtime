// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlServer.Server
{
    // SmiMetaDataProperty defines an extended, optional property to be used on the SmiMetaData class
    //  This approach to adding properties is added combat the growing number of sparsely-used properties 
    //  that are specially handled on the base classes

    internal enum SmiPropertySelector
    {
        DefaultFields = 0x0,
        SortOrder = 0x1,
        UniqueKey = 0x2,
    }

    // Simple collection for properties.  Could extend to IDictionary support if needed in future.
    internal class SmiMetaDataPropertyCollection
    {
        private const int SelectorCount = 3;  // number of elements in SmiPropertySelector

        private SmiMetaDataProperty[] _properties;
        private bool _isReadOnly;

        // Singleton empty instances to ensure each property is always non-null
        private static readonly SmiDefaultFieldsProperty s_emptyDefaultFields = new SmiDefaultFieldsProperty(new List<bool>());
        private static readonly SmiOrderProperty s_emptySortOrder = new SmiOrderProperty(new List<SmiOrderProperty.SmiColumnOrder>());
        private static readonly SmiUniqueKeyProperty s_emptyUniqueKey = new SmiUniqueKeyProperty(new List<bool>());

        internal static readonly SmiMetaDataPropertyCollection EmptyInstance = CreateEmptyInstance();

        private static SmiMetaDataPropertyCollection CreateEmptyInstance()
        {
            var emptyInstance = new SmiMetaDataPropertyCollection();
            emptyInstance.SetReadOnly();
            return emptyInstance;
        }

        internal SmiMetaDataPropertyCollection()
        {
            _properties = new SmiMetaDataProperty[SelectorCount];
            _isReadOnly = false;
            _properties[(int)SmiPropertySelector.DefaultFields] = s_emptyDefaultFields;
            _properties[(int)SmiPropertySelector.SortOrder] = s_emptySortOrder;
            _properties[(int)SmiPropertySelector.UniqueKey] = s_emptyUniqueKey;
        }

        internal SmiMetaDataProperty this[SmiPropertySelector key]
        {
            get
            {
                return _properties[(int)key];
            }
            set
            {
                if (null == value)
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.InvalidSmiCall);
                }
                EnsureWritable();
                _properties[(int)key] = value;
            }
        }

        internal bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
        }


        // Allow switching to read only, but not back.
        internal void SetReadOnly()
        {
            _isReadOnly = true;
        }

        private void EnsureWritable()
        {
            if (IsReadOnly)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
        }
    }

    // Base class for properties
    internal abstract class SmiMetaDataProperty
    {
    }

    // Property defining a list of column ordinals that define a unique key
    internal class SmiUniqueKeyProperty : SmiMetaDataProperty
    {
        private IList<bool> _columns;

        internal SmiUniqueKeyProperty(IList<bool> columnIsKey)
        {
            _columns = new System.Collections.ObjectModel.ReadOnlyCollection<bool>(columnIsKey);
        }

        // indexed by column ordinal indicating for each column whether it is key or not
        internal bool this[int ordinal]
        {
            get
            {
                if (_columns.Count <= ordinal)
                {
                    return false;
                }
                else
                {
                    return _columns[ordinal];
                }
            }
        }

        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert(0 == _columns.Count || countToMatch == _columns.Count,
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _columns.Count +
                    ") not equal to checked size (" + countToMatch + ")");
        }
    }

    // Property defining a sort order for a set of columns (by ordinal and ASC/DESC).
    internal class SmiOrderProperty : SmiMetaDataProperty
    {
        internal struct SmiColumnOrder
        {
            internal int SortOrdinal;
            internal SortOrder Order;
        }

        private IList<SmiColumnOrder> _columns;

        internal SmiOrderProperty(IList<SmiColumnOrder> columnOrders)
        {
            _columns = new System.Collections.ObjectModel.ReadOnlyCollection<SmiColumnOrder>(columnOrders);
        }

        // Readonly list of the columnorder instances making up the sort order
        //  order in list indicates precedence
        internal SmiColumnOrder this[int ordinal]
        {
            get
            {
                if (_columns.Count <= ordinal)
                {
                    SmiColumnOrder order = new SmiColumnOrder();
                    order.Order = SortOrder.Unspecified;
                    order.SortOrdinal = -1;
                    return order;
                }
                else
                {
                    return _columns[ordinal];
                }
            }
        }


        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert(0 == _columns.Count || countToMatch == _columns.Count,
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _columns.Count +
                    ") not equal to checked size (" + countToMatch + ")");
        }
    }

    // property defining inheritance relationship(s)
    internal class SmiDefaultFieldsProperty : SmiMetaDataProperty
    {
        #region private fields

        private IList<bool> _defaults;

        #endregion

        #region internal interface

        internal SmiDefaultFieldsProperty(IList<bool> defaultFields)
        {
            _defaults = new System.Collections.ObjectModel.ReadOnlyCollection<bool>(defaultFields);
        }

        internal bool this[int ordinal]
        {
            get
            {
                if (_defaults.Count <= ordinal)
                {
                    return false;
                }
                else
                {
                    return _defaults[ordinal];
                }
            }
        }

        [Conditional("DEBUG")]
        internal void CheckCount(int countToMatch)
        {
            Debug.Assert(0 == _defaults.Count || countToMatch == _defaults.Count,
                    "SmiDefaultFieldsProperty.CheckCount: DefaultFieldsProperty size (" + _defaults.Count +
                    ") not equal to checked size (" + countToMatch + ")");
        }

        #endregion
    }
}
