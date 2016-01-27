// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;


public class SyncTextWriter
{

    [Fact]
    public void SyncTextWriterLockedOnThis()
    {
        TextWriter oldWriter = Console.Out;
        try
        {
            var newWriter = new CallbackTextWriter();
            Console.SetOut(newWriter);
            TextWriter syncWriter = Console.Out;

            bool called = false;
            newWriter.Callback = _ =>
            {
                Assert.True(Monitor.IsEntered(syncWriter));
                called = true;
            };
            Console.Write("c");
            Assert.True(called);
        }
        finally
        {
            Console.SetOut(oldWriter);
        }
    }

    private sealed class CallbackTextWriter : TextWriter
    {
        internal Action<char> Callback;

        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void Write(char value) { Callback(value); }
    }

}
