// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    public static class TwoKeywordsTest
    {
        public static int Main(string[] args)
        {
            return new TwoKeywordsTraceTest().Execute();
        }
    }

    public class TwoKeywordsTraceTest : AbstractTraceTest
    {
        private bool pass;

        protected override string GetConfigFileContents()
        {
            return @"
OutputPath=.
CircularMB=2048
Providers=My-Simple-Event-Source:0xFFFFFFFFFFFFFFFF:5:Key1=Value1;Key2=Value2
";;
        }

        public override void OnEventCommand(object sender, EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                this.pass = (command.Arguments.Count == 2);
            }
        }

        protected override bool Pass()
        {
            return this.pass;
        }
    }
}
