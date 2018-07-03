// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public abstract class SteTypeBoundaries : StePermutationGenerator
    {
        // Use this marker for attribute value to indicate the attribute is not used 
        // (ex. option where Decimal parameter's Scale property should not be set at all)
        public static object s_doNotUseMarker = new object();
    }

    // simple types can just wrap a simple permutation generator
    public class SteSimpleTypeBoundaries : SteTypeBoundaries
    {
        private static readonly byte[] s_theBigByteArray = CreateByteArray(1000000);
        private static readonly byte[] s_moderateSizeByteArray = CreateByteArray(8000);
        private static readonly string s_moderateSizeString = CreateString(8000);
        private static readonly char[] s_moderateSizeCharArray = s_moderateSizeString.ToCharArray();

        // Class members
        public static readonly IList<SteSimpleTypeBoundaries> s_allTypes;
        public static readonly IList<SteSimpleTypeBoundaries> s_allTypesExceptUdts;
        public static readonly IList<SteSimpleTypeBoundaries> s_udtsOnly;
        static SteSimpleTypeBoundaries()
        {
            List<SteSimpleTypeBoundaries> list = new List<SteSimpleTypeBoundaries>();

            // DevNote: Don't put null value attributes first -- it confuses DataTable generation for SteStructuredTypeBoundaries

            // BigInt
            SteSimplePermutationGenerator type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.BigInt);
            type.Add(SteAttributeKey.Value, (long)0);
            type.Add(SteAttributeKey.Value, long.MaxValue);
            type.Add(SteAttributeKey.Value, long.MinValue);
            type.Add(SteAttributeKey.Value, new SqlInt64(long.MaxValue));
            type.Add(SteAttributeKey.Value, new SqlInt64(long.MinValue));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Binary types
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Binary);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.VarBinary);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Image);
            type.Add(SteAttributeKey.MaxLength, 1);    // a small value
            type.Add(SteAttributeKey.MaxLength, 40);   // Somewhere in the middle
            type.Add(SteAttributeKey.MaxLength, 8000); // Couple values around maximum tds length
            type.Add(SteAttributeKey.Value, CreateByteArray(0));
            type.Add(SteAttributeKey.Value, CreateByteArray(1));
            type.Add(SteAttributeKey.Value, CreateByteArray(50));
            type.Add(SteAttributeKey.Value, s_moderateSizeByteArray);
            type.Add(SteAttributeKey.Value, new SqlBytes(CreateByteArray(0)));
            type.Add(SteAttributeKey.Value, new SqlBytes(CreateByteArray(1)));
            type.Add(SteAttributeKey.Value, new SqlBytes(CreateByteArray(40)));
            type.Add(SteAttributeKey.Value, new SqlBytes(s_moderateSizeByteArray));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            type.Add(SteAttributeKey.Offset, s_doNotUseMarker);
            type.Add(SteAttributeKey.Offset, -1);
            type.Add(SteAttributeKey.Offset, 0);
            type.Add(SteAttributeKey.Offset, 10);
            type.Add(SteAttributeKey.Offset, 8000);
            type.Add(SteAttributeKey.Offset, int.MaxValue);
            type.Add(SteAttributeKey.Length, 0);
            type.Add(SteAttributeKey.Length, 40);
            type.Add(SteAttributeKey.Length, 8000);
            type.Add(SteAttributeKey.Length, 1000000);
            type.Add(SteAttributeKey.Length, -1);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Byte
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.TinyInt);
            type.Add(SteAttributeKey.Value, byte.MaxValue);
            type.Add(SteAttributeKey.Value, byte.MinValue);
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Character (ANSI)
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Char);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Text);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.VarChar);
            type.Add(SteAttributeKey.MaxLength, 1);
            type.Add(SteAttributeKey.MaxLength, 30);
            type.Add(SteAttributeKey.MaxLength, 8000);
            type.Add(SteAttributeKey.Value, CreateString(1));
            type.Add(SteAttributeKey.Value, CreateString(20));
            type.Add(SteAttributeKey.Value, s_moderateSizeString);
            type.Add(SteAttributeKey.Value, CreateString(1).ToCharArray());
            type.Add(SteAttributeKey.Value, CreateString(25).ToCharArray());
            type.Add(SteAttributeKey.Value, s_moderateSizeCharArray);
            type.Add(SteAttributeKey.Value, new SqlChars(CreateString(1).ToCharArray()));
            type.Add(SteAttributeKey.Value, new SqlChars(CreateString(30).ToCharArray()));
            type.Add(SteAttributeKey.Value, new SqlChars(s_moderateSizeCharArray));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Character (UNICODE)
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.NChar);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.NText);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.NVarChar);
            type.Add(SteAttributeKey.MaxLength, 1);
            type.Add(SteAttributeKey.MaxLength, 35);
            type.Add(SteAttributeKey.MaxLength, 4000);
            type.Add(SteAttributeKey.Value, CreateString(1));
            type.Add(SteAttributeKey.Value, CreateString(15));
            type.Add(SteAttributeKey.Value, s_moderateSizeString);
            type.Add(SteAttributeKey.Value, CreateString(1).ToCharArray());
            type.Add(SteAttributeKey.Value, CreateString(20).ToCharArray());
            type.Add(SteAttributeKey.Value, s_moderateSizeCharArray);
            type.Add(SteAttributeKey.Value, new SqlChars(CreateString(1).ToCharArray()));
            type.Add(SteAttributeKey.Value, new SqlChars(CreateString(25).ToCharArray()));
            type.Add(SteAttributeKey.Value, new SqlChars(s_moderateSizeCharArray));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // DateTime
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.DateTime);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.SmallDateTime);
            type.Add(SteAttributeKey.Value, new DateTime(1753, 1, 1));
            type.Add(SteAttributeKey.Value, new SqlDateTime(new DateTime(1753, 1, 1)));  // min SqlDateTime
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Decimal
            //  the TVP test isn't robust in the face of OverflowExceptions on input, so a number of these
            //  values are commented out and other numbers substituted.
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Decimal);
            type.Add(SteAttributeKey.Precision, (byte)38);
            type.Add(SteAttributeKey.Scale, (byte)0);
            type.Add(SteAttributeKey.Scale, (byte)10);
            type.Add(SteAttributeKey.Value, (decimal)0);
            type.Add(SteAttributeKey.Value, decimal.MaxValue / 10000000000);
            type.Add(SteAttributeKey.Value, new SqlDecimal(0));
            type.Add(SteAttributeKey.Value, ((SqlDecimal)1234567890123456.789012345678M) * 100); // Bigger than a Decimal
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Float
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Float);
            type.Add(SteAttributeKey.Value, (double)0);
            type.Add(SteAttributeKey.Value, double.MaxValue);
            type.Add(SteAttributeKey.Value, double.MinValue);
            type.Add(SteAttributeKey.Value, new SqlDouble(double.MaxValue));
            type.Add(SteAttributeKey.Value, new SqlDouble(double.MinValue));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Int
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Int);
            type.Add(SteAttributeKey.Value, (int)0);
            type.Add(SteAttributeKey.Value, int.MaxValue);
            type.Add(SteAttributeKey.Value, int.MinValue);
            type.Add(SteAttributeKey.Value, new SqlInt32(int.MaxValue));
            type.Add(SteAttributeKey.Value, new SqlInt32(int.MinValue));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Money types
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Money);
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.SmallMoney);
            type.Add(SteAttributeKey.Value, (decimal)0);
            type.Add(SteAttributeKey.Value, (decimal)unchecked(((long)0x8000000000000000L) / 10000));
            type.Add(SteAttributeKey.Value, (decimal)0x7FFFFFFFFFFFFFFFL / 10000);
            type.Add(SteAttributeKey.Value, new decimal(-214748.3648)); // smallmoney min
            type.Add(SteAttributeKey.Value, new decimal(214748.3647)); // smallmoney max
            type.Add(SteAttributeKey.Value, new SqlMoney(((decimal)int.MaxValue) / 10000));
            type.Add(SteAttributeKey.Value, new SqlMoney(((decimal)int.MinValue) / 10000));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // Real
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.Real);
            type.Add(SteAttributeKey.Value, (float)0);
            type.Add(SteAttributeKey.Value, float.MaxValue);
            type.Add(SteAttributeKey.Value, float.MinValue);
            type.Add(SteAttributeKey.Value, new SqlSingle(float.MaxValue));
            type.Add(SteAttributeKey.Value, new SqlSingle(float.MinValue));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // SmallInt
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.SmallInt);
            type.Add(SteAttributeKey.Value, (short)0);
            type.Add(SteAttributeKey.Value, short.MaxValue);
            type.Add(SteAttributeKey.Value, short.MinValue);
            type.Add(SteAttributeKey.Value, new SqlInt16(short.MaxValue));
            type.Add(SteAttributeKey.Value, new SqlInt16(short.MinValue));
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // UniqueIdentifier
            type = new SteSimplePermutationGenerator();
            type.Add(SteAttributeKey.SqlDbType, SqlDbType.UniqueIdentifier);
            type.Add(SteAttributeKey.Value, new Guid());
            type.Add(SteAttributeKey.Value, null);
            type.Add(SteAttributeKey.Value, DBNull.Value);
            list.Add(new SteSimpleTypeBoundaries(type));

            // UDT
            // UDTs aren's supported in all table scenarios, so make a separate list
            // that doesn't include them.
            s_allTypesExceptUdts = new List<SteSimpleTypeBoundaries>(list).AsReadOnly();

           //type = new SteSimplePermutationGenerator();
           //type.Add(SteAttributeKey.SqlDbType, SqlDbType.Udt);
           //type.Add(SteAttributeKey.TypeName, "dbo.WeakAddress");
           //type.Add(SteAttributeKey.Type, typeof(WeakAddress));
           //type.Add(SteAttributeKey.Value, new WeakAddress("", ""));
           //type.Add(SteAttributeKey.Value, new WeakAddress(CreateString(22), ""));
           //type.Add(SteAttributeKey.Value, new WeakAddress(ModerateSizeString, ""));
           //type.Add(SteAttributeKey.Value, null);
           //type.Add(SteAttributeKey.Value, DBNull.Value);
           //
           //SteSimpleTypeBoundaries udt = new SteSimpleTypeBoundaries(type);
           //list.Add(udt);
           //
           //AllTypes = list.AsReadOnly();
           //
           //list = new List<SteSimpleTypeBoundaries>();
           //list.Add(udt);
           //UdtsOnly = list.AsReadOnly();
        }

        private SteSimplePermutationGenerator _generator;

        public SteSimpleTypeBoundaries(SteSimplePermutationGenerator generator)
        {
            _generator = generator;
        }

        public override IEnumerable<SteAttributeKey> DefaultKeys
        {
            get
            {
                return _generator.DefaultKeys;
            }
        }

        public override IEnumerator<StePermutation> GetEnumerator(IEnumerable<SteAttributeKey> keysOfInterest)
        {
            return _generator.GetEnumerator(keysOfInterest);
        }

        private const string __prefix = "Char: ";
        public static string CreateString(int size)
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            b.Append(__prefix);
            for (int i = 0; i < s_theBigByteArray.Length && b.Length < size; i++)
            {
                b.Append(s_theBigByteArray[i]);
            }

            if (b.Length > size)
            {
                b.Remove(size, b.Length - size);
            }

            return b.ToString();
        }

        public static byte[] CreateByteArray(int size)
        {
            byte[] result = new byte[size];

            // 
            // Leave a marker of three 0s, followed by the cycle count
            int cycleCount = 0;
            byte cycleStep = 0;
            for (int i = 0; i < result.Length; i++)
            {
                if (cycleStep < 3)
                {
                    result[i] = 0;
                }
                else if (3 == cycleStep)
                {
                    result[i] = (byte)cycleCount;
                }
                else
                {
                    result[i] = cycleStep;
                }
                if (cycleStep == byte.MaxValue)
                {
                    cycleCount++;
                    cycleStep = 0;
                }
                else
                {
                    cycleStep++;
                }
            }

            return result;
        }
    }


    // Structured type boundary value generator
    public class SteStructuredTypeBoundaries : SteTypeBoundaries
    {
        private class SteStructuredTypeBoundariesEnumerator : IEnumerator<StePermutation>
        {
            private enum LogicalPosition
            {
                BeforeElements,          // Position is prior to first element and there is at least one element
                OnElement,         // Position is on an element
                AfterElements             // Position is after final element
            }


            // Value list can be static, since it'll only ever be used in one way.
            private static IList<SteAttributeKey> __valueKey = new List<SteAttributeKey>(new SteAttributeKey[] { SteAttributeKey.Value });

            private SteStructuredTypeBoundaries _parent;
            private bool _isMultiValued;
            private IList<SteAttributeKey> _metaDataKeysOfInterest; // metadata keys that should be used
            private object[][] _separateValueList;  // use the value list separately?
            private IList<IEnumerator<StePermutation>> _fieldEnumerators; // List of enumerators over subordinate types
            private bool[] _completed;         // Which enumerators have already completed?
            private LogicalPosition _logicalPosition;   // Logical positioning of self
            private int _typeNumber;        // used to uniquely separate each type for this session
            private string _typeNameBase;
            private StePermutation _current;
            private StePermutation _rowCountColumn;

            public SteStructuredTypeBoundariesEnumerator(
                            SteStructuredTypeBoundaries parent, IEnumerable<SteAttributeKey> keysOfInterest, bool isMultiValued)
            {
                _parent = parent;
                _typeNameBase = "SteStructType" + Guid.NewGuid();
                _isMultiValued = isMultiValued;
                _current = null;

                // Separate values from everything else, so we can generate a complete table per permutation based on said values.
                bool usesValues = false;
                _metaDataKeysOfInterest = new List<SteAttributeKey>();
                foreach (SteAttributeKey key in keysOfInterest)
                {
                    if (SteAttributeKey.Value == key)
                    {
                        usesValues = true;
                    }
                    else
                    {
                        _metaDataKeysOfInterest.Add(key);
                    }
                }

                if (usesValues)
                {
                    if (_isMultiValued)
                    {
                        CreateSeparateValueList();
                    }
                    else
                    {
                        _metaDataKeysOfInterest.Add(SteAttributeKey.Value);
                    }
                }

                // set up rowcount column
                _rowCountColumn = new StePermutation();
                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.SqlDbType))
                {
                    _rowCountColumn.Add(SteAttributeKey.SqlDbType, SqlDbType.Int);
                }

                Reset();
            }

            public StePermutation Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
                _logicalPosition = LogicalPosition.AfterElements;
                _fieldEnumerators = null;
            }

            public bool UseSeparateValueList
            {
                get
                {
                    return null != _separateValueList;
                }
            }

            public object[][] SeparateValues
            {
                get
                {
                    return _separateValueList;
                }
            }

            public bool MoveNext()
            {
                bool result = false;
                if (LogicalPosition.BeforeElements == _logicalPosition)
                {
                    _logicalPosition = LogicalPosition.OnElement;
                    result = true;
                }
                else if (LogicalPosition.OnElement == _logicalPosition)
                {
                    for (int i = 0; i < _fieldEnumerators.Count; i++)
                    {
                        IEnumerator<StePermutation> field = _fieldEnumerators[i];
                        if (!field.MoveNext())
                        {
                            field.Reset();
                            field.MoveNext();
                            _completed[i] = true;
                        }
                        else if (!_completed[i])
                        {
                            result = true;
                            break;
                        }
                    }
                }

                if (result)
                {
                    if (LogicalPosition.OnElement == _logicalPosition)
                    {
                        List<StePermutation> fields = new List<StePermutation>();
                        foreach (IEnumerator<StePermutation> field in _fieldEnumerators)
                        {
                            fields.Add(field.Current);
                        }
                        fields.Add(_rowCountColumn);

                        _current = CreateTopLevelPermutation(fields);
                    }
                }


                return result;
            }

            public void Reset()
            {
                _fieldEnumerators = new List<IEnumerator<StePermutation>>();
                foreach (SteSimpleTypeBoundaries columnBounds in _parent.ColumnTypes)
                {
                    IEnumerator<StePermutation> fieldPermutations = columnBounds.GetEnumerator(_metaDataKeysOfInterest);

                    // Ignore empty lists
                    if (fieldPermutations.MoveNext())
                    {
                        _fieldEnumerators.Add(fieldPermutations);
                    }
                }

                if (0 < _fieldEnumerators.Count)
                {
                    _logicalPosition = LogicalPosition.BeforeElements;
                    _completed = new bool[_fieldEnumerators.Count];
                }
                else
                {
                    _logicalPosition = LogicalPosition.AfterElements;
                }
            }

            private void CreateSeparateValueList()
            {
                int childColumnCount = _parent.ColumnTypes.Count;
                int columnCount = childColumnCount + 1;
                IEnumerator<StePermutation>[] valueSources = new IEnumerator<StePermutation>[childColumnCount];
                ArrayList[] valueList = new ArrayList[childColumnCount];
                int i = 0;
                foreach (SteSimpleTypeBoundaries field in _parent.ColumnTypes)
                {
                    valueSources[i] = field.GetEnumerator(__valueKey);
                    valueList[i] = new ArrayList();
                    i++;
                }

                // Loop over the permutation enumerators until they all complete at least once
                //  Restart enumerators that complete before the others do
                int completedColumns = 0;

                // Array to track columns that have already completed once
                bool[] isColumnComplete = new bool[childColumnCount];
                for (i = 0; i < childColumnCount; i++)
                {
                    isColumnComplete[i] = false;
                }

                // The main value-accumulation loop
                while (completedColumns < childColumnCount)
                {
                    for (i = 0; i < childColumnCount; i++)
                    {
                        if (!valueSources[i].MoveNext())
                        {
                            // update column completion, if it's the first time for this column
                            if (!isColumnComplete[i])
                            {
                                completedColumns++;
                                isColumnComplete[i] = true;
                            }

                            // restart column, and make sure there's at least one value
                            valueSources[i].Reset();
                            if (!valueSources[i].MoveNext())
                            {
                                throw new InvalidOperationException("Separate value list, but no values for column " + i);
                            }
                        }
                        valueList[i].Add(valueSources[i].Current[SteAttributeKey.Value]);
                    }
                }

                // pivot values into final list
                int rowCount = valueList[0].Count;
                object[][] separateValues = new object[rowCount][];
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    separateValues[rowIndex] = new object[columnCount];
                    for (int columnIndex = 0; columnIndex < childColumnCount; columnIndex++)
                    {
                        separateValues[rowIndex][columnIndex] = valueList[columnIndex][rowIndex];
                    }
                    separateValues[rowIndex][childColumnCount] = rowIndex;
                }
                _separateValueList = separateValues;

            }

            private StePermutation CreateTopLevelPermutation(IList<StePermutation> fields)
            {
                StePermutation perm = new StePermutation();
                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.SqlDbType))
                {
                    perm.Add(SteAttributeKey.SqlDbType, SqlDbType.Structured);
                }

                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.MultiValued))
                {
                    perm.Add(SteAttributeKey.MultiValued, _isMultiValued);
                }

                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.MaxLength))
                {
                    perm.Add(SteAttributeKey.MaxLength, -1);
                }

                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.TypeName))
                {
                    perm.Add(SteAttributeKey.TypeName, _typeNameBase + _typeNumber);
                    _typeNumber++;
                }

                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.Fields))
                {
                    perm.Add(SteAttributeKey.Fields, fields);
                }

                if (0 <= _metaDataKeysOfInterest.IndexOf(SteAttributeKey.Value))
                {
                    if (!UseSeparateValueList)
                    {
                        throw new NotSupportedException("Integrated values not yet supported by test framework.");
                    }

                    perm.Add(SteAttributeKey.Value, _separateValueList);
                }

                return perm;
            }
        }

        // class members
        public static readonly IList<SteStructuredTypeBoundaries> AllTypes;
        public static readonly SteStructuredTypeBoundaries AllColumnTypes;
        public static readonly SteStructuredTypeBoundaries AllColumnTypesExceptUdts;
        public static readonly SteStructuredTypeBoundaries UdtsOnly;
        static SteStructuredTypeBoundaries()
        {
            AllColumnTypesExceptUdts = new SteStructuredTypeBoundaries(SteSimpleTypeBoundaries.s_allTypesExceptUdts, true);
            AllColumnTypes = new SteStructuredTypeBoundaries(SteSimpleTypeBoundaries.s_allTypes, true);
            UdtsOnly = new SteStructuredTypeBoundaries(SteSimpleTypeBoundaries.s_udtsOnly, true);

            AllTypes = new List<SteStructuredTypeBoundaries>();
            AllTypes.Add(AllColumnTypes);
            AllTypes.Add(AllColumnTypesExceptUdts);
            AllTypes.Add(UdtsOnly);
        }

        // instance fields
        private IList<SteSimpleTypeBoundaries> _columnTypes;
        private bool _isMultiValued;

        // ctor
        public SteStructuredTypeBoundaries(IList<SteSimpleTypeBoundaries> columnTypes, bool isMultiValued)
        {
            _columnTypes = columnTypes;
            _isMultiValued = true;
        }

        private IList<SteSimpleTypeBoundaries> ColumnTypes
        {
            get
            {
                return _columnTypes;
            }
        }

        public override IEnumerable<SteAttributeKey> DefaultKeys
        {
            get
            {
                List<SteAttributeKey> result = new List<SteAttributeKey>();
                foreach (SteSimpleTypeBoundaries column in _columnTypes)
                {
                    foreach (SteAttributeKey columnKey in column.DefaultKeys)
                    {
                        if (0 > result.IndexOf(columnKey))
                        {
                            result.Add(columnKey);
                        }
                    }
                }

                if (0 > result.IndexOf(SteAttributeKey.SqlDbType))
                {
                    result.Add(SteAttributeKey.SqlDbType);
                }
                if (0 > result.IndexOf(SteAttributeKey.Value))
                {
                    result.Add(SteAttributeKey.Value);
                }
                if (0 > result.IndexOf(SteAttributeKey.MaxLength))
                {
                    result.Add(SteAttributeKey.MaxLength);
                }
                if (0 > result.IndexOf(SteAttributeKey.TypeName))
                {
                    result.Add(SteAttributeKey.TypeName);
                }
                if (0 > result.IndexOf(SteAttributeKey.Fields))
                {
                    result.Add(SteAttributeKey.Fields);
                }
                return result;
            }
        }

        public override IEnumerator<StePermutation> GetEnumerator(IEnumerable<SteAttributeKey> keysOfInterest)
        {
            return new SteStructuredTypeBoundariesEnumerator(this, keysOfInterest, _isMultiValued);
        }

        public static object[][] GetSeparateValues(IEnumerator<StePermutation> enumerator)
        {
            SteStructuredTypeBoundariesEnumerator myEnum = enumerator as SteStructuredTypeBoundariesEnumerator;
            if (null != myEnum)
            {
                return myEnum.SeparateValues;
            }
            else
            {
                return null;
            }
        }
    }
}
