// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(Server.Contract.Guids.MiscTypesTesting)]
public class MiscTypesTesting : Server.Contract.IMiscTypesTesting
{
    object Server.Contract.IMiscTypesTesting.Marshal_Variant(object obj)
    {
        if (obj is null)
        {
            return null;
        }

        if (obj is DBNull)
        {
            return DBNull.Value;
        }

        if (obj.GetType().IsValueType)
        {
            return CallMemberwiseClone(obj);
        }

        if (obj is string)
        {
            return obj;
        }

        Environment.FailFast($"Arguments must be ValueTypes or strings: {obj.GetType()}");
        return null;

        // object.MemberwiseClone() will bitwise copy for ValueTypes.
        // This is sufficient for the VARIANT marshalling scenario being
        // tested here.
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "MemberwiseClone")]
        static extern object CallMemberwiseClone(object obj);
    }

    object Server.Contract.IMiscTypesTesting.Marshal_Instance_Variant(string init)
    {
        if (Guid.TryParse(init, out Guid result))
        {
            return result;
        }

        Environment.FailFast($"Unknown init value: {init}");
        return null;
    }
}