// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer;

internal static class TargetExtensions
{
    public static unsafe string ReadZeroTerminatedUtf8String(this Target target, TargetPointer pointer, int maxLength)
    {
        byte[]? rented = null;
        Span<byte> bytes = maxLength <= 1024 ? stackalloc byte[maxLength] : (rented = ArrayPool<byte>.Shared.Rent(maxLength));
        try
        {
            target.ReadBuffer(pointer, bytes.Slice(0, maxLength));
            return Encoding.UTF8.GetString(bytes.Slice(0, bytes.IndexOf([(byte)0])));
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
