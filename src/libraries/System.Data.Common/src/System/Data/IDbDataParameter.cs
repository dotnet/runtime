// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public interface IDbDataParameter : IDataParameter
    {
        byte Precision { get; set; }
        byte Scale { get; set; }
        int Size { get; set; }
    }
}
