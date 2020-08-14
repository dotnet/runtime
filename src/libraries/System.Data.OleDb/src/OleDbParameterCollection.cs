// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;

namespace System.Data.OleDb
{
    public sealed partial class OleDbParameterCollection : DbParameterCollection
    {
        private int _changeID;

        private static readonly Type s_itemType = typeof(OleDbParameter);

        internal OleDbParameterCollection() : base()
        {
        }

        internal int ChangeID
        {
            get
            {
                return _changeID;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public new OleDbParameter this[int index]
        {
            get
            {
                return (OleDbParameter)GetParameter(index);
            }
            set
            {
                SetParameter(index, value);
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public new OleDbParameter this[string parameterName]
        {
            get
            {
                return (OleDbParameter)GetParameter(parameterName);
            }
            set
            {
                SetParameter(parameterName, value);
            }
        }

        public OleDbParameter Add(OleDbParameter value)
        {
            Add((object)value);
            return value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Add(String parameterName, Object value) has been deprecated.  Use AddWithValue(String parameterName, Object value).  https://go.microsoft.com/fwlink/?linkid=14202", false)]
        public OleDbParameter Add(string? parameterName, object? value)
        {
            return Add(new OleDbParameter(parameterName, value));
        }

        public OleDbParameter AddWithValue(string? parameterName, object? value)
        {
            return Add(new OleDbParameter(parameterName, value));
        }

        public OleDbParameter Add(string? parameterName, OleDbType oleDbType)
        {
            return Add(new OleDbParameter(parameterName, oleDbType));
        }

        public OleDbParameter Add(string? parameterName, OleDbType oleDbType, int size)
        {
            return Add(new OleDbParameter(parameterName, oleDbType, size));
        }

        public OleDbParameter Add(string? parameterName, OleDbType oleDbType, int size, string? sourceColumn)
        {
            return Add(new OleDbParameter(parameterName, oleDbType, size, sourceColumn));
        }

        public void AddRange(OleDbParameter[] values)
        { // V1.2.3300
            AddRange((Array)values);
        }

        public override bool Contains(string value)
        {
            return (-1 != IndexOf(value));
        }

        public bool Contains(OleDbParameter value)
        {
            return (-1 != IndexOf(value));
        }

        public void CopyTo(OleDbParameter[] array, int index)
        {
            CopyTo((Array)array, index);
        }

        public int IndexOf(OleDbParameter value)
        {
            return IndexOf((object)value);
        }

        public void Insert(int index, OleDbParameter value)
        {
            Insert(index, (object)value);
        }

        private void OnChange()
        {
            unchecked
            { _changeID++; }
        }

        public void Remove(OleDbParameter value)
        {
            Remove((object)value);
        }

    }
}
