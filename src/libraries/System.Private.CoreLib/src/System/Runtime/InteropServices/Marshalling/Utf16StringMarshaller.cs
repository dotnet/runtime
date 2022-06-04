// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for UTF-16 strings
    /// </summary>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(string),
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public unsafe ref struct Utf16StringMarshaller
    {
        private void* _nativeValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf16StringMarshaller"/>.
        /// </summary>
        /// <remarks>
        /// The caller allocated constructor option is not provided because
        /// pinning should be preferred for UTF-16 scenarios.
        /// </remarks>
        /// <param name="str">The string to marshal.</param>
        public Utf16StringMarshaller(string? str)
        {
            _nativeValue = (void*)Marshal.StringToCoTaskMemUni(str);
        }

        /// <summary>
        /// Returns the native value representing the string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void* ToNativeValue() => _nativeValue;

        /// <summary>
        /// Sets the native value representing the string.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(void* value) => _nativeValue = value;

        /// <summary>
        /// Returns the managed string.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public string? ToManaged() => Marshal.PtrToStringUni((IntPtr)_nativeValue);

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            Marshal.FreeCoTaskMem((IntPtr)_nativeValue);
        }
    }
}
