// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Transactions.Tests
{
    public class HelperFunctions
    {
        public static void PromoteTx(Transaction tx)
        {
             TransactionInterop.GetDtcTransaction(tx);
        }

        public static void DisplaySysTxTracing(ITestOutputHelper output, ConcurrentQueue<EventWrittenEventArgs> events)
        {
            if (output == null)
            {
                return;
            }

            string outputString = null;
            foreach (var actualevent in events)
            {
                switch (actualevent.Payload.Count)
                {
                    case 0:
                        {
                            outputString = actualevent.Message;
                            break;
                        }
                    case 1:
                        {
                            outputString = String.Format(actualevent.Message, actualevent.Payload[0]);
                            break;
                        }
                    case 2:
                        {
                            outputString = String.Format(actualevent.Message, actualevent.Payload[0], actualevent.Payload[1]);
                            break;
                        }
                    case 3:
                        {
                            outputString = String.Format(actualevent.Message, actualevent.Payload[0], actualevent.Payload[1], actualevent.Payload[2]);
                            break;
                        }
                    case 4:
                        {
                            outputString = String.Format(actualevent.Message, actualevent.Payload[0], actualevent.Payload[1], actualevent.Payload[2], actualevent.Payload[3]);
                            break;
                        }
                    default:
                        {
                            outputString = String.Format(actualevent.Message, actualevent.Payload[0], actualevent.Payload[1], actualevent.Payload[2], actualevent.Payload[3], actualevent.Payload[4]);
                            break;
                        }
                }
                output.WriteLine(actualevent.Opcode + " : " + outputString);
            }
        }
    }
}