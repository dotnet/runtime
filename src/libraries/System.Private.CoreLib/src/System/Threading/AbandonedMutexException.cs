// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AbandonedMutexException : SystemException
    {
        private int _mutexIndex = -1;
        private Mutex? _mutex;

        public AbandonedMutexException()
            : base(SR.Threading_AbandonedMutexException)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
        }

        public AbandonedMutexException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
        }

        public AbandonedMutexException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
        }

        public AbandonedMutexException(int location, WaitHandle? handle)
            : base(SR.Threading_AbandonedMutexException)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
            SetupException(location, handle);
        }

        public AbandonedMutexException(string? message, int location, WaitHandle? handle)
            : base(message)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
            SetupException(location, handle);
        }

        public AbandonedMutexException(string? message, Exception? inner, int location, WaitHandle? handle)
            : base(message, inner)
        {
            HResult = HResults.COR_E_ABANDONEDMUTEX;
            SetupException(location, handle);
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected AbandonedMutexException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private void SetupException(int location, WaitHandle? handle)
        {
            _mutexIndex = location;
            _mutex = handle as Mutex;
        }

        public Mutex? Mutex => _mutex;
        public int MutexIndex => _mutexIndex;
    }
}
