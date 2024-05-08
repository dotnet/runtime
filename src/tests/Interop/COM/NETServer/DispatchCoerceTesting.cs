// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Server.Contract;

[ComVisible(true)]
[Guid(Server.Contract.Guids.DispatchCoerceTesting)]
public class DispatchCoerceTesting : Server.Contract.IDispatchCoerceTesting
{
    public int ReturnToManaged(short vt)
    {
        throw new NotImplementedException();
    }

    public int ManagedArgument(int arg)
    {
        return arg;
    }

    public string BoolToString()
    {
        throw new NotImplementedException();
    }

    public void ReturnToManaged_Void()
    {
        throw new NotImplementedException();
    }

    double ReturnToManaged_Double()
    {
        throw new NotImplementedException();
    }

    string ReturnToManaged_String()
    {
        throw new NotImplementedException();
    }

    decimal ReturnToManaged_Decimal()
    {
        throw new NotImplementedException();
    }

    DateTime ReturnToManaged_DateTime()
    {
        throw new NotImplementedException();
    }

    System.Drawing.Color ReturnToManaged_Color()
    {
        throw new NotImplementedException();
    }
    
    public System.Reflection.Missing ReturnToManaged_Missing()
    {
        return System.Reflection.Missing.Value;
    }

    public DBNull ReturnToManaged_DBNull()
    {
        return DBNull.Value;
    }
}