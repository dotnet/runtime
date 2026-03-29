// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    // Tracks GC memory pressure for native cryptographic objects on Android.
    //
    // Each Android crypto object wraps JNI global refs pointing to Java objects
    // backed by native BoringSSL/ASN.1 memory. The managed SafeHandle is tiny,
    // so without this hint the GC has no visibility into the true native cost
    // and won't collect aggressively enough — leading to OOM on
    // memory-constrained environments such as Android emulators.
    internal struct AndroidCryptoMemoryPressure
    {
        private const long PressureBytes = 200_000;

        private long _pressure;

        internal void Add()
        {
            Debug.Assert(_pressure == 0);
            GC.AddMemoryPressure(PressureBytes);
            _pressure = PressureBytes;
        }

        internal void Remove()
        {
            long pressure = _pressure;

            if (pressure > 0)
            {
                _pressure = 0;
                GC.RemoveMemoryPressure(pressure);
            }
        }
    }
}
