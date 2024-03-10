// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when the value of an argument is outside the allowable range of values as defined by the invoked method.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
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
            : base(message ?? SR.Arg_ArgumentOutOfRangeException, paramName)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_ArgumentOutOfRangeException, innerException)
        {
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        public ArgumentOutOfRangeException(string? paramName, object? actualValue, string? message)
            : base(message ?? SR.Arg_ArgumentOutOfRangeException, paramName)
        {
            _actualValue = actualValue;
            HResult = HResults.COR_E_ARGUMENTOUTOFRANGE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _actualValue = info.GetValue("ActualValue", typeof(object));
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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
        private static void ThrowZero<T>(T value, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonZero, paramName, value));

        [DoesNotReturn]
        private static void ThrowNegative<T>(T value, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegative, paramName, value));

        [DoesNotReturn]
        private static void ThrowNegativeOrZero<T>(T value, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegativeNonZero, paramName, value));

        [DoesNotReturn]
        private static void ThrowGreater<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLessOrEqual, paramName, value, other));

        [DoesNotReturn]
        private static void ThrowGreaterEqual<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLess, paramName, value, other));

        [DoesNotReturn]
        private static void ThrowLess<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, paramName, value, other));

        [DoesNotReturn]
        private static void ThrowLessEqual<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreater, paramName, value, other));

        [DoesNotReturn]
        private static void ThrowEqual<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNotEqual, paramName, (object?)value ?? "null", (object?)other ?? "null"));

        [DoesNotReturn]
        private static void ThrowNotEqual<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeEqual, paramName, (object?)value ?? "null", (object?)other ?? "null"));

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero.</summary>
        /// <param name="value">The argument to validate as non-zero.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfZero<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            if (T.IsZero(value))
                ThrowZero(value, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
        /// <param name="value">The argument to validate as non-negative.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfNegative<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            if (T.IsNegative(value))
                ThrowNegative(value, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative or zero.</summary>
        /// <param name="value">The argument to validate as non-zero or non-negative.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfNegativeOrZero<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : INumberBase<T>
        {
            if (T.IsNegative(value) || T.IsZero(value))
                ThrowNegativeOrZero(value, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is equal to <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as not equal to <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IEquatable<T>?
        {
            if (EqualityComparer<T>.Default.Equals(value, other))
                ThrowEqual(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not equal to <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as equal to <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfNotEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IEquatable<T>?
        {
            if (!EqualityComparer<T>.Default.Equals(value, other))
                ThrowNotEqual(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as less or equal than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) > 0)
                ThrowGreater(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than or equal <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as less than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) >= 0)
                ThrowGreaterEqual(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as greatar than or equal than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) < 0)
                ThrowLess(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than or equal <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as greatar than than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) <= 0)
                ThrowLessEqual(value, other, paramName);
        }
    }
}
