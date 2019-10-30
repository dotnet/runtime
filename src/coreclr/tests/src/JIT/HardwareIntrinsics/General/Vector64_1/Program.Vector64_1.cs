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
                ["Zero.Byte"] = ZeroByte,
                ["Zero.Double"] = ZeroDouble,
                ["Zero.Int16"] = ZeroInt16,
                ["Zero.Int32"] = ZeroInt32,
                ["Zero.Int64"] = ZeroInt64,
                ["Zero.SByte"] = ZeroSByte,
                ["Zero.Single"] = ZeroSingle,
                ["Zero.UInt16"] = ZeroUInt16,
                ["Zero.UInt32"] = ZeroUInt32,
                ["Zero.UInt64"] = ZeroUInt64,
                ["As.Byte"] = AsByte,
                ["As.Double"] = AsDouble,
                ["As.Int16"] = AsInt16,
                ["As.Int32"] = AsInt32,
                ["As.Int64"] = AsInt64,
                ["As.SByte"] = AsSByte,
                ["As.Single"] = AsSingle,
                ["As.UInt16"] = AsUInt16,
                ["As.UInt32"] = AsUInt32,
                ["As.UInt64"] = AsUInt64,
                ["GetAndWithElement.Byte.0"] = GetAndWithElementByte0,
                ["GetAndWithElement.Byte.7"] = GetAndWithElementByte7,
                ["GetAndWithElement.Double.0"] = GetAndWithElementDouble0,
                ["GetAndWithElement.Int16.0"] = GetAndWithElementInt160,
                ["GetAndWithElement.Int16.3"] = GetAndWithElementInt163,
                ["GetAndWithElement.Int32.0"] = GetAndWithElementInt320,
                ["GetAndWithElement.Int32.1"] = GetAndWithElementInt321,
                ["GetAndWithElement.Int64.0"] = GetAndWithElementInt640,
                ["GetAndWithElement.SByte.0"] = GetAndWithElementSByte0,
                ["GetAndWithElement.SByte.7"] = GetAndWithElementSByte7,
                ["GetAndWithElement.Single.0"] = GetAndWithElementSingle0,
                ["GetAndWithElement.Single.1"] = GetAndWithElementSingle1,
                ["GetAndWithElement.UInt16.0"] = GetAndWithElementUInt160,
                ["GetAndWithElement.UInt16.3"] = GetAndWithElementUInt163,
                ["GetAndWithElement.UInt32.0"] = GetAndWithElementUInt320,
                ["GetAndWithElement.UInt32.1"] = GetAndWithElementUInt321,
                ["GetAndWithElement.UInt64.0"] = GetAndWithElementUInt640,
                ["ToScalar.Byte"] = ToScalarByte,
                ["ToScalar.Double"] = ToScalarDouble,
                ["ToScalar.Int16"] = ToScalarInt16,
                ["ToScalar.Int32"] = ToScalarInt32,
                ["ToScalar.Int64"] = ToScalarInt64,
                ["ToScalar.SByte"] = ToScalarSByte,
                ["ToScalar.Single"] = ToScalarSingle,
                ["ToScalar.UInt16"] = ToScalarUInt16,
                ["ToScalar.UInt32"] = ToScalarUInt32,
                ["ToScalar.UInt64"] = ToScalarUInt64,
                ["ToVector128.Byte"] = ToVector128Byte,
                ["ToVector128.Double"] = ToVector128Double,
                ["ToVector128.Int16"] = ToVector128Int16,
                ["ToVector128.Int32"] = ToVector128Int32,
                ["ToVector128.Int64"] = ToVector128Int64,
                ["ToVector128.SByte"] = ToVector128SByte,
                ["ToVector128.Single"] = ToVector128Single,
                ["ToVector128.UInt16"] = ToVector128UInt16,
                ["ToVector128.UInt32"] = ToVector128UInt32,
                ["ToVector128.UInt64"] = ToVector128UInt64,
                ["ToString.Byte"] = ToStringByte,
                ["ToString.SByte"] = ToStringSByte,
                ["ToString.Int16"] = ToStringInt16,
                ["ToString.UInt16"] = ToStringUInt16,
                ["ToString.Int32"] = ToStringInt32,
                ["ToString.UInt32"] = ToStringUInt32,
                ["ToString.Single"] = ToStringSingle,
                ["ToString.Double"] = ToStringDouble,
                ["ToString.Int64"] = ToStringInt64,
                ["ToString.UInt64"] = ToStringUInt64,
            };
        }
    }
}
