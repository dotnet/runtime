// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net-Mail", LocalizationResources = "FxResources.System.Net.Mail.SR")]
    internal sealed class MailEventSource : EventSource
    {
        private const int RemoveId = 1;
        private const int GetId = 2;
        private const int SetId = 3;
        private const int SendId = 4;
        private const int AssociateId = 5;

        private readonly static MailEventSource s_log = new MailEventSource();
        private MailEventSource() { }
        public static MailEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [Event(RemoveId, Level = EventLevel.Informational)]
        internal void Remove(string name)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(RemoveId, name);
        }

        [Event(GetId, Level = EventLevel.Informational)]
        internal void Get(string name)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(GetId, name);
        }

        [Event(SetId, Level = EventLevel.Informational)]
        internal void Set(string name, string value)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(SetId, string.Format("{0}={1}", name, value));
        }

        [Event(SendId, Level = EventLevel.Informational)]
        internal void Send(string name, string value)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            WriteEvent(SendId, string.Format("{0}={1}", name, value));
        }

        [Event(AssociateId, Level = EventLevel.Informational)]
        internal void Associate(object a, object b)
        {
            WriteEvent(AssociateId, string.Format("Associating {0}#{1} with {2}#{3}", LoggingHash.GetObjectName(a), LoggingHash.HashString(a), LoggingHash.GetObjectName(b), LoggingHash.HashString(b)));
        }
    }
}
