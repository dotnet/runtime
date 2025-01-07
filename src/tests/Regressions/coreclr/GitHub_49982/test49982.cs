// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    private class MockEndPoint : EndPoint
    {
    }

    private sealed class ExtendedSocketException : SocketException
    {
        private readonly EndPoint? _endPoint;

        public ExtendedSocketException(EndPoint? endPoint)
            : base(0)
        {
            _endPoint = endPoint;
        }
        
        public bool EndPointEquals(EndPoint? endPoint)
        {
            return _endPoint == endPoint;
        }
    }

    static bool TestExtendedSocketException()
    {
        EndPoint endPoint = new MockEndPoint();
        ExtendedSocketException extendedSocketException = new ExtendedSocketException(endPoint);
        Console.WriteLine("ExtendedSocketException: {0}", extendedSocketException.GetType());
        return extendedSocketException.EndPointEquals(endPoint);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Extended socket exception:");
        return TestExtendedSocketException() ? 100 : 1;
    }
}
