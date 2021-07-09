// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Globalization.Buffers
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class NumberBuffer
    {
        public char* Digits { get; set; }

        public int Precision { get; set; }

        public int Scale { get; set; }

        public bool IsNegativeSignExists { get; set; }

        public void ResetSettings()
        {
            Precision = default(int);
            Scale = default(int);
            IsNegativeSignExists = default(bool);
        }
    }
}
