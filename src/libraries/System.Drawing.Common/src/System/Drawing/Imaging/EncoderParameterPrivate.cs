// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncoderParameterPrivate
    {
        public Guid ParameterGuid;                    // GUID of the parameter
        public int NumberOfValues;                    // Number of the parameter values
        public EncoderParameterValueType ParameterValueType; // Value type, like ValueTypeLONG  etc.
        public IntPtr ParameterValue;                 // A pointer to the parameter values
    }
}
