// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Exception class for method arguments outside of the legal range.
**
**
=============================================================================*/

using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace System
{
    // The ArgumentOutOfRangeException is thrown when an argument
    // is outside the legal range for that argument.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ArgumentOutOfRangeException : ArgumentException
    {
        private readonly object? _actualValue;

        // Creates a new ArgumentOutOfRangeException with its message
        // string set to a default message explaining an argument was out of range.
        public ArgumentOutOfRangeException()
            : base(SR.Arg_ArgumentOutOfRangeException)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? paramName)
            : base(SR.Arg_ArgumentOutOfRangeException, paramName)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? paramName, string? message)
            : base(message, paramName)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? paramName, object? actualValue, string? message)
            : base(message, paramName)
        {
            _actualValue = actualValue;
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _actualValue = info.GetValue("ActualValue", typeof(object));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ActualValue", _actualValue, typeof(object));
        }

        public override string Message
        {
            get
            {
                string s = base.Message;
                if (_actualValue != null)
                {
                    string valueMessage = SR.Format(SR.ArgumentOutOfRange_ActualValue, _actualValue);
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLineConst + valueMessage;
                }
                return s;
            }
        }

        // Gets the value of the argument that caused the exception.
        public virtual object? ActualValue => _actualValue;

        [DoesNotReturn]
        private static void Throw(string? paramName, string message)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        public static void ThrowIfZero<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            if (T.IsZero(value))
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonZero, paramName));
        }

        public static void ThrowIfNegative<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            if (T.IsNegative(value))
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegative, paramName));
        }

        public static void ThrowIfNegativeOrZero<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            ThrowIfNegative(value, paramName);
            ThrowIfZero(value, paramName);
        }

        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) == 1)
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLowerOrEqual, paramName, other));
        }

        public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) != -1)
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLower, paramName, other));
        }

        public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) == -1)
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, paramName, other));
        }

        public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) != 1)
                Throw(paramName, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreater, paramName, other));
        }
    }
}
