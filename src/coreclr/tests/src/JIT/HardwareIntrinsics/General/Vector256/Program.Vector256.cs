// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Create.Byte"] = CreateByte,
                ["Create.Double"] = CreateDouble,
                ["Create.Int16"] = CreateInt16,
                ["Create.Int32"] = CreateInt32,
                ["Create.Int64"] = CreateInt64,
                ["Create.SByte"] = CreateSByte,
                ["Create.Single"] = CreateSingle,
                ["Create.UInt16"] = CreateUInt16,
                ["Create.UInt32"] = CreateUInt32,
                ["Create.UInt64"] = CreateUInt64,
                ["CreateScalar.Byte"] = CreateScalarByte,
                ["CreateScalar.Double"] = CreateScalarDouble,
                ["CreateScalar.Int16"] = CreateScalarInt16,
                ["CreateScalar.Int32"] = CreateScalarInt32,
                ["CreateScalar.Int64"] = CreateScalarInt64,
                ["CreateScalar.SByte"] = CreateScalarSByte,
                ["CreateScalar.Single"] = CreateScalarSingle,
                ["CreateScalar.UInt16"] = CreateScalarUInt16,
                ["CreateScalar.UInt32"] = CreateScalarUInt32,
                ["CreateScalar.UInt64"] = CreateScalarUInt64,
                ["CreateScalarUnsafe.Byte"] = CreateScalarUnsafeByte,
                ["CreateScalarUnsafe.Double"] = CreateScalarUnsafeDouble,
                ["CreateScalarUnsafe.Int16"] = CreateScalarUnsafeInt16,
                ["CreateScalarUnsafe.Int32"] = CreateScalarUnsafeInt32,
                ["CreateScalarUnsafe.Int64"] = CreateScalarUnsafeInt64,
                ["CreateScalarUnsafe.SByte"] = CreateScalarUnsafeSByte,
                ["CreateScalarUnsafe.Single"] = CreateScalarUnsafeSingle,
                ["CreateScalarUnsafe.UInt16"] = CreateScalarUnsafeUInt16,
                ["CreateScalarUnsafe.UInt32"] = CreateScalarUnsafeUInt32,
                ["CreateScalarUnsafe.UInt64"] = CreateScalarUnsafeUInt64,
                ["CreateElement.Byte"] = CreateElementByte,
                ["CreateElement.Double"] = CreateElementDouble,
                ["CreateElement.Int16"] = CreateElementInt16,
                ["CreateElement.Int32"] = CreateElementInt32,
                ["CreateElement.Int64"] = CreateElementInt64,
                ["CreateElement.SByte"] = CreateElementSByte,
                ["CreateElement.Single"] = CreateElementSingle,
                ["CreateElement.UInt16"] = CreateElementUInt16,
                ["CreateElement.UInt32"] = CreateElementUInt32,
                ["CreateElement.UInt64"] = CreateElementUInt64,
                ["CreateVector.Byte"] = CreateVectorByte,
                ["CreateVector.Double"] = CreateVectorDouble,
                ["CreateVector.Int16"] = CreateVectorInt16,
                ["CreateVector.Int32"] = CreateVectorInt32,
                ["CreateVector.Int64"] = CreateVectorInt64,
                ["CreateVector.SByte"] = CreateVectorSByte,
                ["CreateVector.Single"] = CreateVectorSingle,
                ["CreateVector.UInt16"] = CreateVectorUInt16,
                ["CreateVector.UInt32"] = CreateVectorUInt32,
                ["CreateVector.UInt64"] = CreateVectorUInt64,
            };
        }
    }
}
