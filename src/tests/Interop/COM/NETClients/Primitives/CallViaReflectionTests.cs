// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Reflection;
    using Xunit;

    class CallViaReflectionTests
    {
        private readonly Server.Contract.Servers.NumericTesting server;

        public CallViaReflectionTests()
        {
            this.server = (Server.Contract.Servers.NumericTesting)new Server.Contract.Servers.NumericTestingClass();
        }

        public void Run()
        {
            Console.WriteLine(nameof(CallViaReflectionTests));
            this.InvokeInstanceMethod();
        }

        private void InvokeInstanceMethod()
        {
            MethodInfo minfo = typeof(Server.Contract.INumericTesting).GetMethod("Add_Int")!;
            object[] parameters = new object[2] { 10, 20 };
            int sum = (int)minfo.Invoke(this.server, parameters);
            Assert.Equal(30, sum);
        }
    }
}