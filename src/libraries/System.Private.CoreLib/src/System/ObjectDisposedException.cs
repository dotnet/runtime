// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    /// <summary>
    /// The exception that is thrown when accessing an object that was disposed.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ObjectDisposedException : InvalidOperationException
    {
        private readonly string? _objectName;

        // This constructor should only be called by the EE (COMPlusThrow)
        private ObjectDisposedException() :
            this(null, SR.ObjectDisposed_Generic)
        {
        }

        public ObjectDisposedException(string? objectName) :
            this(objectName, SR.ObjectDisposed_Generic)
        {
        }

        public ObjectDisposedException(string? objectName, string? message) : base(message)
        {
            HResult = HResults.COR_E_OBJECTDISPOSED;
            _objectName = objectName;
        }

        public ObjectDisposedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_OBJECTDISPOSED;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ObjectDisposedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _objectName = info.GetString("ObjectName");
        }

        /// <summary>Throws an <see cref="ObjectDisposedException"/> if the specified <paramref name="condition"/> is <see langword="true"/>.</summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="instance">The object whose type's full name should be included in any resulting <see cref="ObjectDisposedException"/>.</param>
        /// <exception cref="ObjectDisposedException">The <paramref name="condition"/> is <see langword="true"/>.</exception>
        [StackTraceHidden]
        public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
        {
            if (condition)
            {
                ThrowHelper.ThrowObjectDisposedException(instance);
            }
        }

        /// <summary>Throws an <see cref="ObjectDisposedException"/> if the specified <paramref name="condition"/> is <see langword="true"/>.</summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="type">The type whose full name should be included in any resulting <see cref="ObjectDisposedException"/>.</param>
        /// <exception cref="ObjectDisposedException">The <paramref name="condition"/> is <see langword="true"/>.</exception>
        [StackTraceHidden]
        public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type type)
        {
            if (condition)
            {
                ThrowHelper.ThrowObjectDisposedException(type);
            }
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ObjectName", ObjectName, typeof(string));
        }

        /// <summary>
        /// Gets the text for the message for this exception.
        /// </summary>
        public override string Message
        {
            get
            {
                string name = ObjectName;
                if (string.IsNullOrEmpty(name))
                {
                    return base.Message;
                }

                string objectDisposed = SR.Format(SR.ObjectDisposed_ObjectName_Name, name);
                return base.Message + Environment.NewLineConst + objectDisposed;
            }
        }

        public string ObjectName => _objectName ?? string.Empty;
    }
}
