// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.SqlTypes;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        MemoryStream ms = new MemoryStream();
        var sqlXml = new SqlXml(ms);
        var xmlReader = sqlXml.CreateReader();
        return xmlReader != null ? 100 : -1;
    }
}
