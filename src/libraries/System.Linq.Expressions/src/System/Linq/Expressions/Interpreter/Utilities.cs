// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace System.Linq.Expressions.Interpreter
{
#if FEATURE_MAKE_RUN_METHODS
    internal static partial class DelegateHelpers
    {
        private const int MaximumArity = 17;

        internal static Type MakeDelegate(Type[] types)
        {
            Debug.Assert(types != null && types.Length > 0);

            // Can only used predefined delegates if we have no byref types and
            // the arity is small enough to fit in Func<...> or Action<...>
            if (types.Length > MaximumArity || types.Any(t => t.IsByRef))
            {
                throw ContractUtils.Unreachable;
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
            throw ContractUtils.Unreachable;
        }
    }
#endif

    internal static class ScriptingRuntimeHelpers
    {
        public static object Int32ToObject(int i) =>
            i switch
            {
                -1 => Utils.BoxedIntM1,
                0 => Utils.BoxedInt0,
                1 => Utils.BoxedInt1,
                2 => Utils.BoxedInt2,
                3 => Utils.BoxedInt3,

                _ => i,
            };

        internal static object? GetPrimitiveDefaultValue(Type type)
        {
            object result;

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    result = Utils.BoxedFalse;
                    break;
                case TypeCode.SByte:
                    result = Utils.BoxedDefaultSByte;
                    break;
                case TypeCode.Byte:
                    result = Utils.BoxedDefaultByte;
                    break;
                case TypeCode.Char:
                    result = Utils.BoxedDefaultChar;
                    break;
                case TypeCode.Int16:
                    result = Utils.BoxedDefaultInt16;
                    break;
                case TypeCode.Int32:
                    result = Utils.BoxedInt0;
                    break;
                case TypeCode.Int64:
                    result = Utils.BoxedDefaultInt64;
                    break;
                case TypeCode.UInt16:
                    result = Utils.BoxedDefaultUInt16;
                    break;
                case TypeCode.UInt32:
                    result = Utils.BoxedDefaultUInt32;
                    break;
                case TypeCode.UInt64:
                    result = Utils.BoxedDefaultUInt64;
                    break;
                case TypeCode.Single:
                    return Utils.BoxedDefaultSingle;
                case TypeCode.Double:
                    return Utils.BoxedDefaultDouble;
                case TypeCode.DateTime:
                    return Utils.BoxedDefaultDateTime;
                case TypeCode.Decimal:
                    return Utils.BoxedDefaultDecimal;
                default:
                    // Also covers DBNull which is a class.
                    return null;
            }

            if (type.IsEnum)
            {
                result = Enum.ToObject(type, result);
            }

            return result;
        }
    }

    internal static class ExceptionHelpers
    {
        /// <summary>
        /// Updates an exception before it's getting re-thrown so
        /// we can present a reasonable stack trace to the user.
        /// </summary>
        public static void UnwrapAndRethrow(TargetInvocationException exception)
        {
            ExceptionDispatchInfo.Throw(exception.InnerException!);
        }
    }

    /// <summary>
    /// A hybrid dictionary which compares based upon object identity.
    /// </summary>
    internal class HybridReferenceDictionary<TKey, TValue> where TKey : class
    {
        private KeyValuePair<TKey, TValue>[]? _keysAndValues;
        private Dictionary<TKey, TValue>? _dict;
        private const int ArraySize = 10;

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
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
            value = default;
            return false;
        }

        public void Remove(TKey key)
        {
            Debug.Assert(key != null);

            if (_dict != null)
            {
                _dict.Remove(key);
            }
            else if (_keysAndValues != null)
            {
                for (int i = 0; i < _keysAndValues.Length; i++)
                {
                    if (_keysAndValues[i].Key == key)
                    {
                        _keysAndValues[i] = default;
                        return;
                    }
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            Debug.Assert(key != null);

            if (_dict != null)
            {
                return _dict.ContainsKey(key);
            }

            KeyValuePair<TKey, TValue>[]? keysAndValues = _keysAndValues;
            if (keysAndValues != null)
            {
                for (int i = 0; i < keysAndValues.Length; i++)
                {
                    if (keysAndValues[i].Key == key)
                    {
                        return true;
                    }
                }
            }

            return false;
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

                throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
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
                        _keysAndValues = new KeyValuePair<TKey, TValue>[ArraySize];
                        index = 0;
                    }

                    if (index != -1)
                    {
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
