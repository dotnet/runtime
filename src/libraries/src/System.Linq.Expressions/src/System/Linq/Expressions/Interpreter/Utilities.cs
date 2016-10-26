// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using AstUtils = System.Linq.Expressions.Utils;

namespace System.Linq.Expressions.Interpreter
{
#if FEATURE_MAKE_RUN_METHODS
    internal static partial class DelegateHelpers
    {
        private const int MaximumArity = 17;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        internal static Type MakeDelegate(Type[] types)
        {
            Debug.Assert(types != null && types.Length > 0);

            // Can only used predefined delegates if we have no byref types and
            // the arity is small enough to fit in Func<...> or Action<...>
            if (types.Length > MaximumArity || types.Any(t => t.IsByRef))
            {
                throw Assert.Unreachable;
            }

            Type returnType = types[types.Length - 1];
            if (returnType == typeof(void))
            {
                Array.Resize(ref types, types.Length - 1);
                switch (types.Length)
                {
                    case 0: return typeof(Action);

                    case 1: return typeof(Action<>).MakeGenericType(types);
                    case 2: return typeof(Action<,>).MakeGenericType(types);
                    case 3: return typeof(Action<,,>).MakeGenericType(types);
                    case 4: return typeof(Action<,,,>).MakeGenericType(types);
                    case 5: return typeof(Action<,,,,>).MakeGenericType(types);
                    case 6: return typeof(Action<,,,,,>).MakeGenericType(types);
                    case 7: return typeof(Action<,,,,,,>).MakeGenericType(types);
                    case 8: return typeof(Action<,,,,,,,>).MakeGenericType(types);
                    case 9: return typeof(Action<,,,,,,,,>).MakeGenericType(types);
                    case 10: return typeof(Action<,,,,,,,,,>).MakeGenericType(types);
                    case 11: return typeof(Action<,,,,,,,,,,>).MakeGenericType(types);
                    case 12: return typeof(Action<,,,,,,,,,,,>).MakeGenericType(types);
                    case 13: return typeof(Action<,,,,,,,,,,,,>).MakeGenericType(types);
                    case 14: return typeof(Action<,,,,,,,,,,,,,>).MakeGenericType(types);
                    case 15: return typeof(Action<,,,,,,,,,,,,,,>).MakeGenericType(types);
                    case 16: return typeof(Action<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                }
            }
            else
            {
                switch (types.Length)
                {
                    case 1: return typeof(Func<>).MakeGenericType(types);
                    case 2: return typeof(Func<,>).MakeGenericType(types);
                    case 3: return typeof(Func<,,>).MakeGenericType(types);
                    case 4: return typeof(Func<,,,>).MakeGenericType(types);
                    case 5: return typeof(Func<,,,,>).MakeGenericType(types);
                    case 6: return typeof(Func<,,,,,>).MakeGenericType(types);
                    case 7: return typeof(Func<,,,,,,>).MakeGenericType(types);
                    case 8: return typeof(Func<,,,,,,,>).MakeGenericType(types);
                    case 9: return typeof(Func<,,,,,,,,>).MakeGenericType(types);
                    case 10: return typeof(Func<,,,,,,,,,>).MakeGenericType(types);
                    case 11: return typeof(Func<,,,,,,,,,,>).MakeGenericType(types);
                    case 12: return typeof(Func<,,,,,,,,,,,>).MakeGenericType(types);
                    case 13: return typeof(Func<,,,,,,,,,,,,>).MakeGenericType(types);
                    case 14: return typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(types);
                    case 15: return typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(types);
                    case 16: return typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                    case 17: return typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                }
            }
            throw Assert.Unreachable;
        }
    }
#endif

    internal static class ScriptingRuntimeHelpers
    {
        public static object Int32ToObject(int i)
        {
            switch (i)
            {
                case -1:
                    return Int32_M1;
                case 0:
                    return Int32_0;
                case 1:
                    return Int32_1;
                case 2:
                    return Int32_2;
            }

            return i;
        }

        private static readonly object Int32_M1 = -1;
        private static readonly object Int32_0 = 0;
        private static readonly object Int32_1 = 1;
        private static readonly object Int32_2 = 2;

        public static object BooleanToObject(bool b)
        {
            return b ? Boolean_True : Boolean_False;
        }

        internal static readonly object Boolean_True = true;
        internal static readonly object Boolean_False = false;

        internal static object GetPrimitiveDefaultValue(Type type)
        {
            object result;

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    result = Boolean_False;
                    break;
                case TypeCode.SByte:
                    result = default(sbyte);
                    break;
                case TypeCode.Byte:
                    result = default(byte);
                    break;
                case TypeCode.Char:
                    result = default(char);
                    break;
                case TypeCode.Int16:
                    result = default(short);
                    break;
                case TypeCode.Int32:
                    result = Int32_0;
                    break;
                case TypeCode.Int64:
                    result = default(long);
                    break;
                case TypeCode.UInt16:
                    result = default(ushort);
                    break;
                case TypeCode.UInt32:
                    result = default(uint);
                    break;
                case TypeCode.UInt64:
                    result = default(ulong);
                    break;
                case TypeCode.Single:
                    return default(float);
                case TypeCode.Double:
                    return default(double);
                //case TypeCode.DBNull: 
                //    return default(DBNull); 
                case TypeCode.DateTime:
                    return default(DateTime);
                case TypeCode.Decimal:
                    return default(decimal);
                default:
                    return null;
            }

            if (type.GetTypeInfo().IsEnum)
            {
                result = Enum.ToObject(type, result);
            }

            return result;
        }
    }

    internal static class ExceptionHelpers
    {
#if FEATURE_STACK_TRACES
        private const string prevStackTraces = "PreviousStackTraces";
#endif

        /// <summary>
        /// Updates an exception before it's getting re-thrown so
        /// we can present a reasonable stack trace to the user.
        /// </summary>
        public static Exception UpdateForRethrow(Exception rethrow)
        {
#if FEATURE_STACK_TRACES
            List<StackTrace> prev;

            // we don't have any dynamic stack trace data, capture the data we can
            // from the raw exception object.
            StackTrace st = new StackTrace(rethrow, true);

            if (!TryGetAssociatedStackTraces(rethrow, out prev))
            {
                prev = new List<StackTrace>();
                AssociateStackTraces(rethrow, prev);
            }

            prev.Add(st);

#endif // FEATURE_STACK_TRACES
            return rethrow;
        }
#if FEATURE_STACK_TRACES
        /// <summary>
        /// Returns all the stack traces associates with an exception
        /// </summary>
        public static IList<StackTrace> GetExceptionStackTraces(Exception rethrow)
        {
            List<StackTrace> result;
            return TryGetAssociatedStackTraces(rethrow, out result) ? result : null;
        }

        private static void AssociateStackTraces(Exception e, List<StackTrace> traces)
        {
            e.Data[prevStackTraces] = traces;
        }

        private static bool TryGetAssociatedStackTraces(Exception e, out List<StackTrace> traces)
        {
            traces = e.Data[prevStackTraces] as List<StackTrace>;
            return traces != null;
        }
#endif // FEATURE_STACK_TRACES
    }

    /// <summary>
    /// A hybrid dictionary which compares based upon object identity.
    /// </summary>
    internal class HybridReferenceDictionary<TKey, TValue> where TKey : class
    {
        private KeyValuePair<TKey, TValue>[] _keysAndValues;
        private Dictionary<TKey, TValue> _dict;
        private int _count;
        private const int _arraySize = 10;

        public HybridReferenceDictionary()
        {
        }

        public HybridReferenceDictionary(int initialCapicity)
        {
            if (initialCapicity > _arraySize)
            {
                _dict = new Dictionary<TKey, TValue>(initialCapicity);
            }
            else
            {
                _keysAndValues = new KeyValuePair<TKey, TValue>[initialCapicity];
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Debug.Assert(key != null);

            if (_dict != null)
            {
                return _dict.TryGetValue(key, out value);
            }
            else if (_keysAndValues != null)
            {
                for (int i = 0; i < _keysAndValues.Length; i++)
                {
                    if (_keysAndValues[i].Key == key)
                    {
                        value = _keysAndValues[i].Value;
                        return true;
                    }
                }
            }
            value = default(TValue);
            return false;
        }

        public bool Remove(TKey key)
        {
            Debug.Assert(key != null);

            if (_dict != null)
            {
                return _dict.Remove(key);
            }
            else if (_keysAndValues != null)
            {
                for (int i = 0; i < _keysAndValues.Length; i++)
                {
                    if (_keysAndValues[i].Key == key)
                    {
                        _keysAndValues[i] = new KeyValuePair<TKey, TValue>();
                        _count--;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsKey(TKey key)
        {
            Debug.Assert(key != null);

            if (_dict != null)
            {
                return _dict.ContainsKey(key);
            }
            else if (_keysAndValues != null)
            {
                for (int i = 0; i < _keysAndValues.Length; i++)
                {
                    if (_keysAndValues[i].Key == key)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int Count
        {
            get
            {
                if (_dict != null)
                {
                    return _dict.Count;
                }
                return _count;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_dict != null)
            {
                return _dict.GetEnumerator();
            }

            return GetEnumeratorWorker();
        }

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorWorker()
        {
            if (_keysAndValues != null)
            {
                for (int i = 0; i < _keysAndValues.Length; i++)
                {
                    if (_keysAndValues[i].Key != null)
                    {
                        yield return _keysAndValues[i];
                    }
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                Debug.Assert(key != null);

                TValue res;
                if (TryGetValue(key, out res))
                {
                    return res;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                Debug.Assert(key != null);

                if (_dict != null)
                {
                    _dict[key] = value;
                }
                else
                {
                    int index;
                    if (_keysAndValues != null)
                    {
                        index = -1;
                        for (int i = 0; i < _keysAndValues.Length; i++)
                        {
                            if (_keysAndValues[i].Key == key)
                            {
                                _keysAndValues[i] = new KeyValuePair<TKey, TValue>(key, value);
                                return;
                            }
                            else if (_keysAndValues[i].Key == null)
                            {
                                index = i;
                            }
                        }
                    }
                    else
                    {
                        _keysAndValues = new KeyValuePair<TKey, TValue>[_arraySize];
                        index = 0;
                    }

                    if (index != -1)
                    {
                        _count++;
                        _keysAndValues[index] = new KeyValuePair<TKey, TValue>(key, value);
                    }
                    else
                    {
                        _dict = new Dictionary<TKey, TValue>();
                        for (int i = 0; i < _keysAndValues.Length; i++)
                        {
                            _dict[_keysAndValues[i].Key] = _keysAndValues[i].Value;
                        }
                        _keysAndValues = null;

                        _dict[key] = value;
                    }
                }
            }
        }
    }

    internal static class Assert
    {
        [Conditional("DEBUG")]
        public static void NotNull(object var)
        {
            Debug.Assert(var != null);
        }
    }
}
