// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Data
{
    internal sealed class FunctionNode : ExpressionNode
    {
        internal readonly string _name;
        internal readonly int _info = -1;
        internal int _argumentCount;
        internal const int initialCapacity = 1;
        internal ExpressionNode[]? _arguments;
        private readonly TypeLimiter? _capturedLimiter;

        private static readonly Function[] s_funcs = new Function[] {
            new Function("Abs", FunctionId.Abs, typeof(object), true, false, 1, typeof(object), null, null),
            new Function("IIf", FunctionId.Iif, typeof(object), false, false, 3, typeof(object), typeof(object), typeof(object)),
            new Function("In", FunctionId.In, typeof(bool), false, true, 1, null, null, null),
            new Function("IsNull", FunctionId.IsNull, typeof(object), false, false, 2, typeof(object), typeof(object), null),
            new Function("Len", FunctionId.Len, typeof(int), true, false, 1, typeof(string), null, null),
            new Function("Substring", FunctionId.Substring, typeof(string), true, false, 3, typeof(string), typeof(int), typeof(int)),
            new Function("Trim", FunctionId.Trim, typeof(string), true, false, 1, typeof(string), null, null),
            // convert
            new Function("Convert", FunctionId.Convert, typeof(object), false, true, 1, typeof(object), null, null),
            new Function("DateTimeOffset", FunctionId.DateTimeOffset, typeof(DateTimeOffset), false, true, 3, typeof(DateTime), typeof(int), typeof(int)),
            // store aggregates here
            new Function("Max", FunctionId.Max, typeof(object), false, false, 1, null, null, null),
            new Function("Min", FunctionId.Min, typeof(object), false, false, 1, null, null, null),
            new Function("Sum", FunctionId.Sum, typeof(object), false, false, 1, null, null, null),
            new Function("Count", FunctionId.Count, typeof(object), false, false, 1, null, null, null),
            new Function("Var", FunctionId.Var, typeof(object), false, false, 1, null, null, null),
            new Function("StDev", FunctionId.StDev, typeof(object), false, false, 1, null, null, null),
            new Function("Avg", FunctionId.Avg, typeof(object), false, false, 1, null, null, null),
        };

        internal FunctionNode(DataTable? table, string name) : base(table)
        {
            // Because FunctionNode instances are created eagerly but evaluated lazily,
            // we need to capture the deserialization scope here. The scope could be
            // null if no deserialization is in progress.

            _capturedLimiter = TypeLimiter.Capture();

            _name = name;
            for (int i = 0; i < s_funcs.Length; i++)
            {
                if (string.Equals(s_funcs[i]._name, name, StringComparison.OrdinalIgnoreCase))
                {
                    // we found the reserved word..
                    _info = i;
                    break;
                }
            }
            if (_info < 0)
            {
                throw ExprException.UndefinedFunction(_name);
            }
        }

        internal void AddArgument(ExpressionNode argument)
        {
            if (!s_funcs[_info]._isVariantArgumentList && _argumentCount >= s_funcs[_info]._argumentCount)
                throw ExprException.FunctionArgumentCount(_name);

            if (_arguments == null)
            {
                _arguments = new ExpressionNode[initialCapacity];
            }
            else if (_argumentCount == _arguments.Length)
            {
                ExpressionNode[] bigger = new ExpressionNode[_argumentCount * 2];
                Array.Copy(_arguments, bigger, _argumentCount);
                _arguments = bigger;
            }
            _arguments[_argumentCount++] = argument;
        }

        internal override void Bind(DataTable table, List<DataColumn> list)
        {
            BindTable(table);
            Check();

            // special case for the Convert function bind only the first argument:
            // the second argument should be a Type stored as a name node, replace it with constant.
            if (s_funcs[_info]._id == FunctionId.Convert)
            {
                if (_argumentCount != 2)
                    throw ExprException.FunctionArgumentCount(_name);
                _arguments![0].Bind(table, list);

                if (_arguments[1].GetType() == typeof(NameNode))
                {
                    NameNode type = (NameNode)_arguments[1];
                    _arguments[1] = new ConstNode(table, ValueType.Str, type._name);
                }
                _arguments[1].Bind(table, list);
            }
            else
            {
                for (int i = 0; i < _argumentCount; i++)
                {
                    _arguments![i].Bind(table, list);
                }
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval()
        {
            return Eval(null, DataRowVersion.Default);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(DataRow? row, DataRowVersion version)
        {
            Debug.Assert(_info < s_funcs.Length && _info >= 0, "Invalid function info.");

            object[] argumentValues = new object[_argumentCount];

            Debug.Assert(_argumentCount == s_funcs[_info]._argumentCount || s_funcs[_info]._isVariantArgumentList, "Invalid argument argumentCount.");

            // special case of the Convert function
            if (s_funcs[_info]._id == FunctionId.Convert)
            {
                if (_argumentCount != 2)
                    throw ExprException.FunctionArgumentCount(_name);

                argumentValues[0] = _arguments![0].Eval(row, version);
                argumentValues[1] = GetDataType(_arguments[1]);
            }
            else if (s_funcs[_info]._id != FunctionId.Iif)
            { // We do not want to evaluate arguments of IIF, we will already do it in EvalFunction/ second point: we may go to div by 0
                for (int i = 0; i < _argumentCount; i++)
                {
                    argumentValues[i] = _arguments![i].Eval(row, version);

                    if (s_funcs[_info]._isValidateArguments)
                    {
                        if ((argumentValues[i] == DBNull.Value) || (typeof(object) == s_funcs[_info]._parameters[i]))
                        {
                            // currently all supported functions with IsValidateArguments set to true
                            // NOTE: for IIF and ISNULL IsValidateArguments set to false
                            return DBNull.Value;
                        }

                        if (argumentValues[i].GetType() != s_funcs[_info]._parameters[i])
                        {
                            // We are allowing conversions in one very specific case: int, int64,...'nice' numeric to numeric..

                            if (s_funcs[_info]._parameters[i] == typeof(int) && ExpressionNode.IsInteger(DataStorage.GetStorageType(argumentValues[i].GetType())))
                            {
                                argumentValues[i] = Convert.ToInt32(argumentValues[i], FormatProvider);
                            }
                            else if ((s_funcs[_info]._id == FunctionId.Trim) || (s_funcs[_info]._id == FunctionId.Substring) || (s_funcs[_info]._id == FunctionId.Len))
                            {
                                if ((typeof(string) != (argumentValues[i].GetType())) && (typeof(SqlString) != (argumentValues[i].GetType())))
                                {
                                    throw ExprException.ArgumentType(s_funcs[_info]._name, i + 1, s_funcs[_info]._parameters[i]!);
                                }
                            }
                            else
                            {
                                throw ExprException.ArgumentType(s_funcs[_info]._name, i + 1, s_funcs[_info]._parameters[i]!);
                            }
                        }
                    }
                }
            }
            return EvalFunction(s_funcs[_info]._id, argumentValues, row, version);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal override object Eval(int[] recordNos)
        {
            throw ExprException.ComputeNotAggregate(ToString()!);
        }

        internal override bool IsConstant()
        {
            // Currently all function calls with const arguments return constant.
            // That could change in the future (if we implement Rand()...)
            // CONSIDER: We could be smarter for Iif.

            bool constant = true;

            for (int i = 0; i < _argumentCount; i++)
            {
                constant = constant && _arguments![i].IsConstant();
            }

            Debug.Assert(_info > -1, "All function nodes should be bound at this point.");  // default info is -1, it means if not bounded, it should be -1, not 0!!

            return (constant);
        }

        internal override bool IsTableConstant()
        {
            for (int i = 0; i < _argumentCount; i++)
            {
                if (!_arguments![i].IsTableConstant())
                {
                    return false;
                }
            }
            return true;
        }

        internal override bool HasLocalAggregate()
        {
            for (int i = 0; i < _argumentCount; i++)
            {
                if (_arguments![i].HasLocalAggregate())
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            for (int i = 0; i < _argumentCount; i++)
            {
                if (_arguments![i].HasRemoteAggregate())
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool DependsOn(DataColumn column)
        {
            for (int i = 0; i < _argumentCount; i++)
            {
                if (_arguments![i].DependsOn(column))
                    return true;
            }
            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Constant expressions are safe to be evaluated.")]
        internal override ExpressionNode Optimize()
        {
            for (int i = 0; i < _argumentCount; i++)
            {
                _arguments![i] = _arguments[i].Optimize();
            }

            Debug.Assert(_info > -1, "Optimizing unbound function "); // default info is -1, it means if not bounded, it should be -1, not 0!!

            if (s_funcs[_info]._id == FunctionId.In)
            {
                // we can not optimize the in node, just check that it has all constant arguments

                if (!IsConstant())
                {
                    throw ExprException.NonConstantArgument();
                }
            }
            else
            {
                if (IsConstant())
                {
                    return new ConstNode(table, ValueType.Object, Eval(), false);
                }
            }
            return this;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private Type GetDataType(ExpressionNode node)
        {
            Type nodeType = node.GetType();
            string? typeName = null;

            if (nodeType == typeof(NameNode))
            {
                typeName = ((NameNode)node)._name;
            }
            if (nodeType == typeof(ConstNode))
            {
                typeName = ((ConstNode)node)._val.ToString();
            }

            if (typeName == null)
            {
                throw ExprException.ArgumentType(s_funcs[_info]._name, 2, typeof(Type));
            }

            Type? dataType = Type.GetType(typeName);

            if (dataType == null)
            {
                throw ExprException.InvalidType(typeName);
            }

            // ReadXml might not be on the current call stack. So we'll use the TypeLimiter
            // that was captured when this FunctionNode instance was created.

            TypeLimiter.EnsureTypeIsAllowed(dataType, _capturedLimiter);

            return dataType;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private object EvalFunction(FunctionId id, object[] argumentValues, DataRow? row, DataRowVersion version)
        {
            StorageType storageType;
            switch (id)
            {
                case FunctionId.Abs:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");

                    storageType = DataStorage.GetStorageType(argumentValues[0].GetType());
                    if (ExpressionNode.IsInteger(storageType))
                        return (Math.Abs((long)argumentValues[0]));
                    if (ExpressionNode.IsNumeric(storageType))
                        return (Math.Abs((double)argumentValues[0]));

                    throw ExprException.ArgumentTypeInteger(s_funcs[_info]._name, 1);

                case FunctionId.cBool:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");

                    storageType = DataStorage.GetStorageType(argumentValues[0].GetType());
                    return storageType switch
                    {
                        StorageType.Boolean => (bool)argumentValues[0],
                        StorageType.Int32 => ((int)argumentValues[0] != 0),
                        StorageType.Double => ((double)argumentValues[0] != 0.0),
                        StorageType.String => bool.Parse((string)argumentValues[0]),
                        _ => throw ExprException.DatatypeConvertion(argumentValues[0].GetType(), typeof(bool)),
                    };
                case FunctionId.cInt:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                    return Convert.ToInt32(argumentValues[0], FormatProvider);

                case FunctionId.cDate:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                    return Convert.ToDateTime(argumentValues[0], FormatProvider);

                case FunctionId.cDbl:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                    return Convert.ToDouble(argumentValues[0], FormatProvider);

                case FunctionId.cStr:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                    return Convert.ToString(argumentValues[0], FormatProvider)!;

                case FunctionId.Charindex:
                    Debug.Assert(_argumentCount == 2, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");

                    Debug.Assert(argumentValues[0] is string, $"Invalid argument type for {s_funcs[_info]._name}");
                    Debug.Assert(argumentValues[1] is string, $"Invalid argument type for {s_funcs[_info]._name}");

                    if (DataStorage.IsObjectNull(argumentValues[0]) || DataStorage.IsObjectNull(argumentValues[1]))
                        return DBNull.Value;

                    if (argumentValues[0] is SqlString)
                        argumentValues[0] = ((SqlString)argumentValues[0]).Value;

                    if (argumentValues[1] is SqlString)
                        argumentValues[1] = ((SqlString)argumentValues[1]).Value;

                    return ((string)argumentValues[1]).IndexOf((string)argumentValues[0], StringComparison.Ordinal);

                case FunctionId.Iif:
                    Debug.Assert(_argumentCount == 3, $"Invalid argument argumentCount: {_argumentCount.ToString(FormatProvider)}");

                    object first = _arguments![0].Eval(row, version);

                    if (DataExpression.ToBoolean(first) != false)
                    {
                        return _arguments[1].Eval(row, version);
                    }
                    else
                    {
                        return _arguments[2].Eval(row, version);
                    }

                case FunctionId.In:
                    // we never evaluate IN directly: IN as a binary operator, so evaluation of this should be in
                    // BinaryNode class
                    throw ExprException.NYI(s_funcs[_info]._name);

                case FunctionId.IsNull:
                    Debug.Assert(_argumentCount == 2, "Invalid argument argumentCount: ");

                    if (DataStorage.IsObjectNull(argumentValues[0]))
                        return argumentValues[1];
                    else
                        return argumentValues[0];

                case FunctionId.Len:
                    Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                    Debug.Assert((argumentValues[0] is string) || (argumentValues[0] is SqlString), $"Invalid argument type for {s_funcs[_info]._name}");

                    if (argumentValues[0] is SqlString)
                    {
                        if (((SqlString)argumentValues[0]).IsNull)
                        {
                            return DBNull.Value;
                        }
                        else
                        {
                            argumentValues[0] = ((SqlString)argumentValues[0]).Value;
                        }
                    }

                    return ((string)argumentValues[0]).Length;


                case FunctionId.Substring:
                    Debug.Assert(_argumentCount == 3, $"Invalid argument argumentCount: {_argumentCount.ToString(FormatProvider)}");
                    Debug.Assert((argumentValues[0] is string) || (argumentValues[0] is SqlString), $"Invalid first argument {argumentValues[0].GetType().FullName} in {s_funcs[_info]._name}");
                    Debug.Assert(argumentValues[1] is int, $"Invalid second argument {argumentValues[1].GetType().FullName} in {s_funcs[_info]._name}");
                    Debug.Assert(argumentValues[2] is int, $"Invalid third argument {argumentValues[2].GetType().FullName} in {s_funcs[_info]._name}");

                    // work around the differences in .NET and VBA implementation of the Substring function
                    // 1. The <index> Argument is 0-based in .NET, and 1-based in VBA
                    // 2. If the <Length> argument is longer then the string length .NET throws an ArgumentException
                    //    but our users still want to get a result.

                    int start = (int)argumentValues[1] - 1;

                    int length = (int)argumentValues[2];

                    if (start < 0)
                        throw ExprException.FunctionArgumentOutOfRange("index", "Substring");

                    if (length < 0)
                        throw ExprException.FunctionArgumentOutOfRange("length", "Substring");

                    if (length == 0)
                        return string.Empty;

                    if (argumentValues[0] is SqlString)
                        argumentValues[0] = ((SqlString)argumentValues[0]).Value;

                    int src_length = ((string)argumentValues[0]).Length;

                    if (start > src_length)
                    {
                        return DBNull.Value;
                    }

                    if (start + length > src_length)
                    {
                        length = src_length - start;
                    }

                    return ((string)argumentValues[0]).Substring(start, length);

                case FunctionId.Trim:
                    {
                        Debug.Assert(_argumentCount == 1, $"Invalid argument argumentCount for {s_funcs[_info]._name} : {_argumentCount.ToString(FormatProvider)}");
                        Debug.Assert((argumentValues[0] is string) || (argumentValues[0] is SqlString), $"Invalid argument type for {s_funcs[_info]._name}");

                        if (DataStorage.IsObjectNull(argumentValues[0]))
                            return DBNull.Value;

                        if (argumentValues[0] is SqlString)
                            argumentValues[0] = ((SqlString)argumentValues[0]).Value;

                        return (((string)argumentValues[0]).Trim());
                    }

                case FunctionId.Convert:
                    if (_argumentCount != 2)
                        throw ExprException.FunctionArgumentCount(_name);

                    if (argumentValues[0] == DBNull.Value)
                    {
                        return DBNull.Value;
                    }

                    Type type = (Type)argumentValues[1];
                    StorageType mytype = DataStorage.GetStorageType(type);
                    storageType = DataStorage.GetStorageType(argumentValues[0].GetType());

                    if (mytype == StorageType.DateTimeOffset)
                    {
                        if (storageType == StorageType.String)
                        {
                            return SqlConvert.ConvertStringToDateTimeOffset((string)argumentValues[0], FormatProvider);
                        }
                    }

                    if (StorageType.Object != mytype)
                    {
                        if ((mytype == StorageType.Guid) && (storageType == StorageType.String))
                            return new Guid((string)argumentValues[0]);

                        if (ExpressionNode.IsFloatSql(storageType) && ExpressionNode.IsIntegerSql(mytype))
                        {
                            if (StorageType.Single == storageType)
                            {
                                return SqlConvert.ChangeType2((float)SqlConvert.ChangeType2(argumentValues[0], StorageType.Single, typeof(float), FormatProvider), mytype, type, FormatProvider);
                            }
                            else if (StorageType.Double == storageType)
                            {
                                return SqlConvert.ChangeType2((double)SqlConvert.ChangeType2(argumentValues[0], StorageType.Double, typeof(double), FormatProvider), mytype, type, FormatProvider);
                            }
                            else if (StorageType.Decimal == storageType)
                            {
                                return SqlConvert.ChangeType2((decimal)SqlConvert.ChangeType2(argumentValues[0], StorageType.Decimal, typeof(decimal), FormatProvider), mytype, type, FormatProvider);
                            }
                        }

                        // The Convert function can be called lazily, outside of a previous Serialization Guard scope.
                        // If there was a type limiter scope on the stack at the time this Convert function was created,
                        // we must manually re-enter the Serialization Guard scope.

                        DeserializationToken deserializationToken = (_capturedLimiter != null) ? SerializationInfo.StartDeserialization() : default;
                        using (deserializationToken)
                        {
                            return SqlConvert.ChangeType2(argumentValues[0], mytype, type, FormatProvider);
                        }
                    }

                    return argumentValues[0];

                case FunctionId.DateTimeOffset:
                    if (argumentValues[0] == DBNull.Value || argumentValues[1] == DBNull.Value || argumentValues[2] == DBNull.Value)
                        return DBNull.Value;
                    switch (((DateTime)argumentValues[0]).Kind)
                    {
                        case DateTimeKind.Utc:
                            if ((int)argumentValues[1] != 0 && (int)argumentValues[2] != 0)
                            {
                                throw ExprException.MismatchKindandTimeSpan();
                            }
                            break;
                        case DateTimeKind.Local:
                            if (DateTimeOffset.Now.Offset.Hours != (int)argumentValues[1] && DateTimeOffset.Now.Offset.Minutes != (int)argumentValues[2])
                            {
                                throw ExprException.MismatchKindandTimeSpan();
                            }
                            break;
                        case DateTimeKind.Unspecified: break;
                    }
                    if ((int)argumentValues[1] < -14 || (int)argumentValues[1] > 14)
                        throw ExprException.InvalidHoursArgument();
                    if ((int)argumentValues[2] < -59 || (int)argumentValues[2] > 59)
                        throw ExprException.InvalidMinutesArgument();
                    // range should be within -14 hours and  +14 hours
                    if ((int)argumentValues[1] == 14 && (int)argumentValues[2] > 0)
                        throw ExprException.InvalidTimeZoneRange();
                    if ((int)argumentValues[1] == -14 && (int)argumentValues[2] < 0)
                        throw ExprException.InvalidTimeZoneRange();

                    return new DateTimeOffset((DateTime)argumentValues[0], new TimeSpan((int)argumentValues[1], (int)argumentValues[2], 0));

                default:
                    throw ExprException.UndefinedFunction(s_funcs[_info]._name);
            }
        }

        internal FunctionId Aggregate
        {
            get
            {
                if (IsAggregate)
                {
                    return s_funcs[_info]._id;
                }
                return FunctionId.none;
            }
        }

        internal bool IsAggregate
        {
            get
            {
                bool aggregate = (s_funcs[_info]._id == FunctionId.Sum) ||
                                 (s_funcs[_info]._id == FunctionId.Avg) ||
                                 (s_funcs[_info]._id == FunctionId.Min) ||
                                 (s_funcs[_info]._id == FunctionId.Max) ||
                                 (s_funcs[_info]._id == FunctionId.Count) ||
                                 (s_funcs[_info]._id == FunctionId.StDev) ||
                                 (s_funcs[_info]._id == FunctionId.Var);
                return aggregate;
            }
        }

        internal void Check()
        {
            if (_info < 0)
                throw ExprException.UndefinedFunction(_name);

            if (s_funcs[_info]._isVariantArgumentList)
            {
                // for finctions with variabls argument list argumentCount is a minimal number of arguments
                if (_argumentCount < s_funcs[_info]._argumentCount)
                {
                    // Special case for the IN operator
                    if (s_funcs[_info]._id == FunctionId.In)
                        throw ExprException.InWithoutList();

                    throw ExprException.FunctionArgumentCount(_name);
                }
            }
            else
            {
                if (_argumentCount != s_funcs[_info]._argumentCount)
                    throw ExprException.FunctionArgumentCount(_name);
            }
        }
    }
    internal enum FunctionId
    {
        none = -1,
        Ascii = 0,
        Char = 1,
        Charindex = 2,
        Difference = 3,
        Len = 4,
        Lower = 5,
        LTrim = 6,
        Patindex = 7,
        Replicate = 8,
        Reverse = 9,
        Right = 10,
        RTrim = 11,
        Soundex = 12,
        Space = 13,
        Str = 14,
        Stuff = 15,
        Substring = 16,
        Upper = 17,
        IsNull = 18,
        Iif = 19,
        Convert = 20,
        cInt = 21,
        cBool = 22,
        cDate = 23,
        cDbl = 24,
        cStr = 25,
        Abs = 26,
        Acos = 27,
        In = 28,
        Trim = 29,
        Sum = 30,
        Avg = 31,
        Min = 32,
        Max = 33,
        Count = 34,
        StDev = 35,  // Statistical standard deviation
        Var = 37,    // Statistical variance
        DateTimeOffset = 38,
    }

    internal sealed class Function
    {
        internal readonly string _name;
        internal readonly FunctionId _id;
        internal readonly Type _result;
        internal readonly bool _isValidateArguments;
        internal readonly bool _isVariantArgumentList;
        internal readonly int _argumentCount;
        internal readonly Type?[] _parameters = new Type?[] { null, null, null };

        // TODO: Unused, consider removing
        internal Function()
        {
            _name = null!;
            _id = FunctionId.none;
            _result = null!;
            _isValidateArguments = false;
            _argumentCount = 0;
        }

        internal Function(string name, FunctionId id, Type result, bool IsValidateArguments,
                          bool IsVariantArgumentList, int argumentCount, Type? a1, Type? a2, Type? a3)
        {
            _name = name;
            _id = id;
            _result = result;
            _isValidateArguments = IsValidateArguments;
            _isVariantArgumentList = IsVariantArgumentList;
            _argumentCount = argumentCount;

            if (a1 != null)
                _parameters[0] = a1;
            if (a2 != null)
                _parameters[1] = a2;
            if (a3 != null)
                _parameters[2] = a3;
        }

        internal static string[] s_functionName = new string[] {
            "Unknown",
            "Ascii",
            "Char",
            "CharIndex",
            "Difference",
            "Len",
            "Lower",
            "LTrim",
            "Patindex",
            "Replicate",
            "Reverse",
            "Right",
            "RTrim",
            "Soundex",
            "Space",
            "Str",
            "Stuff",
            "Substring",
            "Upper",
            "IsNull",
            "Iif",
            "Convert",
            "cInt",
            "cBool",
            "cDate",
            "cDbl",
            "cStr",
            "Abs",
            "Acos",
            "In",
            "Trim",
            "Sum",
            "Avg",
            "Min",
            "Max",
            "Count",
            "StDev",
            "Var",
            "DateTimeOffset",
        };
    }
}
