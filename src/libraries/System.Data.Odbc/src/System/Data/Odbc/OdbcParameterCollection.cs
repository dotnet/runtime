// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;

namespace System.Data.Odbc
{
    [Editor("Microsoft.VSDesigner.Data.Design.DBParametersEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed partial class OdbcParameterCollection : DbParameterCollection
    {
        private bool _rebindCollection;   // The collection needs to be (re)bound

        private static readonly Type s_itemType = typeof(OdbcParameter);

        internal OdbcParameterCollection() : base()
        {
        }

        internal bool RebindCollection
        {
            get { return _rebindCollection; }
            set { _rebindCollection = value; }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public new OdbcParameter this[int index]
        {
            get
            {
                return (OdbcParameter)GetParameter(index);
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
        public new OdbcParameter this[string parameterName]
        {
            get
            {
                return (OdbcParameter)GetParameter(parameterName);
            }
            set
            {
                SetParameter(parameterName, value);
            }
        }

        public OdbcParameter Add(OdbcParameter value)
        {
            // MDAC 59206
            Add((object)value);
            return value;
        }

        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        [Obsolete("Add(String parameterName, Object value) has been deprecated. Use AddWithValue(String parameterName, Object value) instead.")]
        public OdbcParameter Add(string? parameterName, object? value)
        {
            // MDAC 59206
            return Add(new OdbcParameter(parameterName, value));
        }

        public OdbcParameter AddWithValue(string? parameterName, object? value)
        {
            // MDAC 79027
            return Add(new OdbcParameter(parameterName, value));
        }

        public OdbcParameter Add(string? parameterName, OdbcType odbcType)
        {
            return Add(new OdbcParameter(parameterName, odbcType));
        }

        public OdbcParameter Add(string? parameterName, OdbcType odbcType, int size)
        {
            return Add(new OdbcParameter(parameterName, odbcType, size));
        }

        public OdbcParameter Add(string? parameterName, OdbcType odbcType, int size, string? sourceColumn)
        {
            return Add(new OdbcParameter(parameterName, odbcType, size, sourceColumn));
        }

        public void AddRange(OdbcParameter[] values)
        {
            // V1.2.3300
            AddRange((Array)values);
        }

        // Walks through the collection and binds each parameter
        //
        internal void Bind(OdbcCommand command, CMDWrapper cmdWrapper, CNativeBuffer parameterBuffer)
        {
            for (int i = 0; i < Count; ++i)
            {
                this[i].Bind(cmdWrapper.StatementHandle!, command, checked((short)(i + 1)), parameterBuffer, true);
            }
            _rebindCollection = false;
        }

        internal int CalcParameterBufferSize(OdbcCommand command)
        {
            // Calculate the size of the buffer we need
            int parameterBufferSize = 0;

            for (int i = 0; i < Count; ++i)
            {
                if (_rebindCollection)
                {
                    this[i].HasChanged = true;
                }
                this[i].PrepareForBind(command, (short)(i + 1), ref parameterBufferSize);

                parameterBufferSize = (parameterBufferSize + (IntPtr.Size - 1)) & ~(IntPtr.Size - 1);          // align buffer;
            }
            return parameterBufferSize;
        }

        // Walks through the collection and clears the parameters
        //
        internal void ClearBindings()
        {
            for (int i = 0; i < Count; ++i)
            {
                this[i].ClearBinding();
            }
        }

        public override bool Contains(string value)
        { // WebData 97349
            return (-1 != IndexOf(value));
        }

        public bool Contains(OdbcParameter value)
        {
            return (-1 != IndexOf(value));
        }

        public void CopyTo(OdbcParameter[] array, int index)
        {
            CopyTo((Array)array, index);
        }

        private void OnChange()
        {
            _rebindCollection = true;
        }

        internal void GetOutputValues(CMDWrapper cmdWrapper)
        {
            // mdac 88542 - we will not read out the parameters if the collection has changed
            if (!_rebindCollection)
            {
                CNativeBuffer parameterBuffer = cmdWrapper._nativeParameterBuffer!;
                for (int i = 0; i < Count; ++i)
                {
                    this[i].GetOutputValue(parameterBuffer);
                }
            }
        }

        public int IndexOf(OdbcParameter value)
        {
            return IndexOf((object)value);
        }

        public void Insert(int index, OdbcParameter value)
        {
            Insert(index, (object)value);
        }

        public void Remove(OdbcParameter value)
        {
            Remove((object)value);
        }
    }
}
