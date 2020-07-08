// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Test
{
    public class Container<T>
    {
        private static long s_instanceCounter;
        private readonly long _instanceId = Interlocked.Increment(ref s_instanceCounter);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Container()
        {
            Console.Write("({0}) ID = ({1})\r\n", typeof(T).ToString(), _instanceId);
            return;
        }
    }


    internal static class App
    {
        private static int Main()
        {
            var container1 = new Container<string>();
            var container2 = new Container<object>();
            var container3 = new Container<Random>();
            var container4 = new Container<Stream>();
            var container5 = new Container<BinaryReader>();
            var container6 = new Container<BinaryWriter>();
            return 100;  //assume if no unhandled exception the test passes
        }
    }
}
