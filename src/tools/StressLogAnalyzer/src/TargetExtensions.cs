// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer;

internal static class TargetExtensions
{
    public static unsafe string ReadZeroTerminatedAsciiString(this Target target, TargetPointer pointer, int maxLength)
    {
        StringBuilder sb = new();
        for (byte ch = target.Read<byte>(pointer);
        ch != 0;
            ch = target.Read<byte>(pointer = new TargetPointer((ulong)pointer + 1)))
        {
            if (sb.Length > maxLength)
            {
                break;
            }

            sb.Append((char)ch);
        }
        return sb.ToString();
    }
}
