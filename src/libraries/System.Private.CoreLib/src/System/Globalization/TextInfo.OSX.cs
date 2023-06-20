// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Unicode;

namespace System.Globalization
{
    public partial class TextInfo
    {
        internal unsafe char* ChangeCaseNative(char* src, int srcLen, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(GlobalizationMode.Hybrid);
            string result="";

            if (HasEmptyCultureName)
            {
                result = Interop.Globalization.ChangeCaseInvariantNative(src, srcLen, toUpper);
                //var resultSpan = result.AsSpan();
                /*fixed (char* changedStr= &MemoryMarshal.GetReference(resultSpan))
                {
                    System.Diagnostics.Debug.WriteLine("ChangeCaseNative:result is " + result);
                    return changedStr;
                }*/
            }
            else
            {
                result = Interop.Globalization.ChangeCaseNative(_cultureName, _cultureName.Length, src, srcLen, toUpper);
                /*var resultSpan = result.AsSpan();
                fixed (char* changedStr= &MemoryMarshal.GetReference(resultSpan))
                {
                    System.Diagnostics.Debug.WriteLine("ChangeCaseNative:result is " + result);
                    return changedStr;
                    ref MemoryMarshal.GetReference(
                }*/
            }
            ReadOnlySpan<char> resultSpan = result.AsSpan();
            System.Diagnostics.Debug.WriteLine("ChangeCaseNative:result is " + result);
            System.Diagnostics.Debug.WriteLine("ChangeCaseNative:result is " + resultSpan.ToString());
            fixed (char* pSource = &MemoryMarshal.GetReference(resultSpan))
            {
                return pSource;
            }
        }
    }
}
