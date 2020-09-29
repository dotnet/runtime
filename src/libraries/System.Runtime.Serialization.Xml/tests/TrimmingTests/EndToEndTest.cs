// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using MyApplication.Shared.Types;

class Program
{
    // Preserve the AuthState type until DataContractSerializer is fully trim-safe.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AuthState))]
    static int Main(string[] args)
    {
        string data = @"<AuthState xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/MyApplication.Shared.Types""> <GivenName>Admin User</GivenName> <IsAuthenticated>true</IsAuthenticated> <Name>admin@fmdevsql.onmicrosoft.com</Name> </AuthState>";

        var s = DeserializeDataContract<AuthState>(data);
        if (s.GivenName != "Admin User" ||
            s.Name != "admin@fmdevsql.onmicrosoft.com" ||
            !s.IsAuthenticated)
        {
            return -1;
        }

        return 100;
    }

    private static T DeserializeDataContract<T>(string serialized)
    {
        using (var inStream = new MemoryStream(Encoding.UTF8.GetBytes(serialized)))
        {
            var ser = new DataContractSerializer(typeof(T));
            return (T)ser.ReadObject(inStream);
        }
    }
}

namespace MyApplication.Shared.Types
{
    public class AuthState
    {
        public AuthState()
        {
        }

        public bool IsAuthenticated { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
    }
}
