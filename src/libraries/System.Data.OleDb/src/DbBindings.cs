// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace System.Data.OleDb
{
    internal sealed class Bindings
    {
        private readonly tagDBPARAMBINDINFO[]? _bindInfo;
        private readonly tagDBBINDING[] _dbbindings;
        private readonly tagDBCOLUMNACCESS[] _dbcolumns;

        private OleDbParameter[]? _parameters;
        private readonly int _collectionChangeID;

        private OleDbDataReader? _dataReader;
        private ColumnBinding[]? _columnBindings;
        private RowBinding? _rowBinding;

        private int _index;
        private readonly int _count;
        private int _dataBufferSize;
        private readonly bool _ifIRowsetElseIRow;
        private bool _forceRebind;
        private bool _needToReset;

        private Bindings(int count)
        {
            _count = count;

            _dbbindings = new tagDBBINDING[count];
            for (int i = 0; i < _dbbindings.Length; ++i)
            {
                _dbbindings[i] = new tagDBBINDING();
            }
            _dbcolumns = new tagDBCOLUMNACCESS[count];
        }

        internal Bindings(OleDbParameter[] parameters, int collectionChangeID) : this(parameters.Length)
        {
            _bindInfo = new tagDBPARAMBINDINFO[parameters.Length];
            _parameters = parameters;
            _collectionChangeID = collectionChangeID;
            _ifIRowsetElseIRow = true;
        }

        internal Bindings(OleDbDataReader dataReader, bool ifIRowsetElseIRow, int count) : this(count)
        {
            _dataReader = dataReader;
            _ifIRowsetElseIRow = ifIRowsetElseIRow;
        }

        internal tagDBPARAMBINDINFO[]? BindInfo
        {
            get { return _bindInfo; }
        }
        internal tagDBCOLUMNACCESS[] DBColumnAccess
        {
            get { return _dbcolumns; }
        }

        internal int CurrentIndex
        {
            //get { return _index; }
            set
            {
                Debug.Assert((0 <= value) && (value < _count), "bad binding index");
                _index = value;
            }
        }

        internal ColumnBinding[] ColumnBindings()
        {
            Debug.Assert(null != _columnBindings, "null ColumnBindings");
            return _columnBindings;
        }

        internal OleDbParameter[] Parameters()
        {
            Debug.Assert(null != _parameters, "null Parameters");
            return _parameters;
        }

        internal RowBinding? RowBinding()
        {
            //Debug.Assert(null != _rowBinding, "null RowBinding");
            return _rowBinding;
        }

        internal bool ForceRebind
        {
            get { return _forceRebind; }
            set { _forceRebind = value; }
        }

        // tagDBPARAMBINDINFO member access
        internal IntPtr DataSourceType
        {
            //get { return _bindInfo[_index].pwszDataSourceType; }
            set
            {
                _bindInfo![_index].pwszDataSourceType = value;
            }
        }
        internal IntPtr Name
        {
            //get { return _bindInfo[_index].pwszName; }
            set
            {
                _bindInfo![_index].pwszName = value;
            }
        }
        internal IntPtr ParamSize
        {
            get
            {
                if (null != _bindInfo)
                {
                    return _bindInfo[_index].ulParamSize;
                }
                return IntPtr.Zero;
            }
            set
            {
                _bindInfo![_index].ulParamSize = value;
            }
        }
        internal int Flags
        {
            //get { return _bindInfo[_index].dwFlag; }
            set
            {
                _bindInfo![_index].dwFlags = value;
            }
        }

        // tagDBBINDING member access
        //
        internal IntPtr Ordinal
        { // iOrdinal
            //get { return _dbbindings[_index].iOrdinal.ToInt32(); }
            set
            {
                _dbbindings[_index].iOrdinal = value;
            }
        }
#if DEBUG
        /*internal int ValueOffset { // obValue
            get { return _dbbindings[_index].obValue.ToInt32(); }
        }
        internal int LengthOffset { // obLength
            get { return _dbbindings[_index].obLength.ToInt32(); }
        }
        internal int StatusOffset { // obStatus
            get { return _dbbindings[_index].obStatus.ToInt32(); }
        }*/
#endif
        internal int Part
        { // dwPart
#if DEBUG
            //get { return _dbbindings[_index].dwPart; }
#endif
            set { _dbbindings[_index].dwPart = value; }
        }
        internal int ParamIO
        { // eParamIO
#if DEBUG
            //get { return _dbbindings[_index].eParamIO; }
#endif
            set { _dbbindings[_index].eParamIO = value; }
        }
        internal int MaxLen
        { // cbMaxLen
            //get { return (int) _dbbindings[_index].cbMaxLen; }
            set
            {
                Debug.Assert(0 <= value, "invalid MaxLen");

                _dbbindings[_index].obStatus = (IntPtr)(_dataBufferSize + 0);
                _dbbindings[_index].obLength = (IntPtr)(_dataBufferSize + ADP.PtrSize);
                _dbbindings[_index].obValue = (IntPtr)(_dataBufferSize + ADP.PtrSize + ADP.PtrSize);
                _dataBufferSize += ADP.PtrSize + ADP.PtrSize;

                switch (DbType)
                {
                    case (NativeDBType.BSTR):  // ADP.PtrSize
                    case (NativeDBType.HCHAPTER): // ADP.PtrSize
                    case (NativeDBType.PROPVARIANT): // sizeof(PROPVARIANT)
                    case (NativeDBType.VARIANT): // 16 or 24 (8 + ADP.PtrSize *2)
                    case (NativeDBType.BYREF | NativeDBType.BYTES): // ADP.PtrSize
                    case (NativeDBType.BYREF | NativeDBType.WSTR): // ADP.PtrSize
                                                                   // allocate extra space to cache original value for disposal
                        _dataBufferSize += System.Data.OleDb.RowBinding.AlignDataSize(value * 2);
                        _needToReset = true;
                        break;
                    default:
                        _dataBufferSize += System.Data.OleDb.RowBinding.AlignDataSize(value);
                        break;
                }

                _dbbindings[_index].cbMaxLen = (IntPtr)value;
                _dbcolumns[_index].cbMaxLen = (IntPtr)value;
            }
        }
        internal int DbType
        { // wType
            get { return _dbbindings[_index].wType; }
            set
            {
                _dbbindings[_index].wType = (short)value;
                _dbcolumns[_index].wType = (short)value;
            }
        }
        internal byte Precision
        { // bPrecision
#if DEBUG
            //get { return _dbbindings[_index].bPrecision; }

#endif
            set
            {
                if (null != _bindInfo)
                {
                    _bindInfo[_index].bPrecision = value;
                }
                _dbbindings[_index].bPrecision = value;
                _dbcolumns[_index].bPrecision = value;
            }
        }
        internal byte Scale
        { // bScale
#if DEBUG
            //get { return _dbbindings[_index].bScale; }
#endif
            set
            {
                if (null != _bindInfo)
                {
                    _bindInfo[_index].bScale = value;
                }
                _dbbindings[_index].bScale = value;
                _dbcolumns[_index].bScale = value;
            }
        }

        internal int AllocateForAccessor(OleDbDataReader? dataReader, int indexStart, int indexForAccessor)
        {
            Debug.Assert(null == _rowBinding, "row binding already allocated");
            Debug.Assert(null == _columnBindings, "column bindings already allocated");

            RowBinding rowBinding = System.Data.OleDb.RowBinding.CreateBuffer(_count, _dataBufferSize, _needToReset);
            _rowBinding = rowBinding;

            ColumnBinding[] columnBindings = rowBinding.SetBindings(dataReader, this, indexStart, indexForAccessor, _parameters, _dbbindings, _ifIRowsetElseIRow);
            Debug.Assert(null != columnBindings, "null column bindings");
            _columnBindings = columnBindings;

            if (!_ifIRowsetElseIRow)
            {
                Debug.Assert(columnBindings.Length == _dbcolumns.Length, "length mismatch");
                for (int i = 0; i < columnBindings.Length; ++i)
                {
                    _dbcolumns[i].pData = rowBinding.DangerousGetDataPtr(columnBindings[i].ValueOffset); // We are simply pointing at a location later in the buffer, so we're OK to not addref the buffer.
                }
            }

#if DEBUG
            int index = -1;
            foreach (ColumnBinding binding in columnBindings)
            {
                Debug.Assert(index < binding.Index, "invaild index");
                index = binding.Index;
            }
#endif
            return (indexStart + columnBindings.Length);
        }

        internal void ApplyInputParameters()
        {
            ColumnBinding[] columnBindings = this.ColumnBindings();
            OleDbParameter[] parameters = this.Parameters();

            RowBinding()!.StartDataBlock();
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (ADP.IsDirection(parameters[i], ParameterDirection.Input))
                {
                    columnBindings[i].SetOffset(parameters[i].Offset);
                    columnBindings[i].Value(parameters[i].GetCoercedValue());
                }
                else
                {
                    // always set ouput only and return value parameter values to null when executing
                    parameters[i].Value = null;

                    //columnBindings[i].SetValueEmpty();
                }
            }
        }

        internal void ApplyOutputParameters()
        {
            ColumnBinding[] columnBindings = this.ColumnBindings();
            OleDbParameter[] parameters = this.Parameters();

            for (int i = 0; i < parameters.Length; ++i)
            {
                if (ADP.IsDirection(parameters[i], ParameterDirection.Output))
                {
                    parameters[i].Value = columnBindings[i].Value();
                }
            }
            CleanupBindings();
        }

        internal bool AreParameterBindingsInvalid(OleDbParameterCollection collection)
        {
            Debug.Assert(null != collection, "null parameter collection");
            Debug.Assert(null != _parameters, "null parameters");

            ColumnBinding[] columnBindings = this.ColumnBindings();
            if (!ForceRebind && ((collection.ChangeID == _collectionChangeID) && (_parameters.Length == collection.Count)))
            {
                for (int i = 0; i < columnBindings.Length; ++i)
                {
                    ColumnBinding binding = columnBindings[i];

                    Debug.Assert(null != binding, "null column binding");
                    Debug.Assert(binding.Parameter() == _parameters[i], "parameter mismatch");
                    if (binding.IsParameterBindingInvalid(collection[i]))
                    {
                        //Debug.WriteLine("ParameterBindingsInvalid");
                        return true;
                    }
                }
                //Debug.WriteLine("ParameterBindingsValid");
                return false; // collection and cached values are the same
            }
            //Debug.WriteLine("ParameterBindingsInvalid");
            return true;
        }

        internal void CleanupBindings()
        {
            RowBinding? rowBinding = this.RowBinding();
            if (null != rowBinding)
            {
                rowBinding.ResetValues();

                ColumnBinding[] columnBindings = this.ColumnBindings();
                for (int i = 0; i < columnBindings.Length; ++i)
                {
                    ColumnBinding binding = columnBindings[i];
                    if (null != binding)
                    {
                        binding.ResetValue();
                    }
                }
            }
        }

        internal void CloseFromConnection()
        {
            if (null != _rowBinding)
            {
                _rowBinding.CloseFromConnection();
            }
            Dispose();
        }

        internal OleDbHResult CreateAccessor(UnsafeNativeMethods.IAccessor iaccessor, int flags)
        {
            Debug.Assert(null != _rowBinding, "no row binding");
            Debug.Assert(null != _columnBindings, "no column bindings");
            return _rowBinding.CreateAccessor(iaccessor, flags, _columnBindings);
        }

        public void Dispose()
        {
            _parameters = null;
            _dataReader = null;
            _columnBindings = null;

            RowBinding? rowBinding = _rowBinding;
            _rowBinding = null;
            if (null != rowBinding)
            {
                rowBinding.Dispose();
            }
        }

        internal void GuidKindName(Guid guid, int eKind, IntPtr propid)
        {
            tagDBCOLUMNACCESS[] dbcolumns = DBColumnAccess;
            dbcolumns[_index].columnid.uGuid = guid;
            dbcolumns[_index].columnid.eKind = eKind;
            dbcolumns[_index].columnid.ulPropid = propid;
        }

        internal void ParameterStatus(StringBuilder builder)
        {
            ColumnBinding[] columnBindings = ColumnBindings();
            for (int i = 0; i < columnBindings.Length; ++i)
            {
                ODB.CommandParameterStatus(builder, i, columnBindings[i].StatusValue());
            }
        }
    }
}
