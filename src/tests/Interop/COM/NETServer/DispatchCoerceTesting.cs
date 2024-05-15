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

    public void ReturnToManaged_Void(int value)
    {
        throw new NotImplementedException();
    }

    public double ReturnToManaged_Double(int value)
    {
        throw new NotImplementedException();
    }

    public string ReturnToManaged_String(int value)
    {
        throw new NotImplementedException();
    }

    public decimal ReturnToManaged_Decimal(int value)
    {
        throw new NotImplementedException();
    }

    public DateTime ReturnToManaged_DateTime(int value)
    {
        throw new NotImplementedException();
    }

    public System.Drawing.Color ReturnToManaged_Color(int value)
    {
        throw new NotImplementedException();
    }
    
    public System.Reflection.Missing ReturnToManaged_Missing(int value)
    {
        return System.Reflection.Missing.Value;
    }

    public DBNull ReturnToManaged_DBNull(int value)
    {
        return DBNull.Value;
    }
}