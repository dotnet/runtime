// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when a <see langword="null"/> reference (<see langword="Nothing"/> in Visual Basic) is passed to a method that does not accept it as a valid argument.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ArgumentNullException : ArgumentException
    {
        // Creates a new ArgumentNullException with its message
        // string set to a default message explaining an argument was null.
        public ArgumentNullException()
             : base(SR.ArgumentNull_Generic)
        {
            // Use E_POINTER - COM used that for null pointers.  Description is "invalid pointer"
            HResult = HResults.E_POINTER;
        }

        public ArgumentNullException(string? paramName)
            : base(SR.ArgumentNull_Generic, paramName)
        {
            HResult = HResults.E_POINTER;
        }

        public ArgumentNullException(string? message, Exception? innerException)
            : base(message ?? SR.ArgumentNull_Generic, innerException)
        {
            HResult = HResults.E_POINTER;
        }

        public ArgumentNullException(string? paramName, string? message)
            : base(message ?? SR.ArgumentNull_Generic, paramName)
        {
            HResult = HResults.E_POINTER;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ArgumentNullException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                Throw(paramName);
            }
        }

        internal static void ThrowIfNull([NotNull] object? argument, ExceptionArgument paramName)
        {
            if (argument is null)
            {
                ThrowHelper.ThrowArgumentNullException(paramName);
            }
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The pointer argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        [CLSCompliant(false)]
        public static unsafe void ThrowIfNull([NotNull] void* argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                Throw(paramName);
            }
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The pointer argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        internal static unsafe void ThrowIfNull(IntPtr argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument == IntPtr.Zero)
            {
                Throw(paramName);
            }
        }

        [DoesNotReturn]
        internal static void Throw(string? paramName) =>
            throw new ArgumentNullException(paramName);
    }
}
