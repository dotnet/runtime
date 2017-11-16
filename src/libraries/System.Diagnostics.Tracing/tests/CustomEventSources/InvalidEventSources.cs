// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framework), we use this Ifdef make each kind 

namespace SdtEventSources
{
    public class UnsealedEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }
    }

    public sealed class EventWithReturnEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public int WriteInteger(int n)
        {
            WriteEvent(1, n);
            return n;
        }
    }

    public sealed class NegativeEventIdEventSource : EventSource
    {
        [Event(-10, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(-10, n);
        }
    }

    // this behaves differently in 4.5 vs. 4.5.1 so just skip it for those scenarios
    public sealed class OutOfRangeKwdEventSource : EventSource
    {
        [Event(1, Keywords = Keywords.Kwd1, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
        public static class Keywords
        {
            public const EventKeywords Kwd1 = (EventKeywords)0x0000100000000000UL;
        }
        #endregion
    }

    public sealed class ReservedOpcodeEventSource : EventSource
    {
        [Event(1, Opcode = Opcodes.Op1, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
        public static class Opcodes
        {
            public const EventOpcode Op1 = (EventOpcode)3; // values <= 10 are reserved
        }
        #endregion
    }

    public sealed class EnumKindMismatchEventSource : EventSource
    {
        [Event(1, Keywords = Opcodes.Op1, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
        public static class Opcodes
        {
            public const EventKeywords Op1 = (EventKeywords)0x1;
        }
        #endregion
    }

    public sealed class MismatchIdEventSource : EventSource
    {
        [Event(10, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }
    }

    public sealed class EventIdReusedEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void WriteInteger1(int n)
        {
            WriteEvent(1, n);
        }
        [Event(1, Level = EventLevel.Informational)]
        public void WriteInteger2(int n)
        {
            WriteEvent(1, n);
        }
    }

    public sealed class EventNameReusedEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void WriteInteger(uint n)
        {
            WriteEvent(2, n);
        }
    }

    public sealed class TaskOpcodePairReusedEventSource : EventSource
    {
        [Event(1, Task = Tasks.MyTask, Opcode = Opcodes.Op1)]
        public void WriteInteger1(int n)
        {
            WriteEvent(1, n);
        }
        [Event(2, Task = Tasks.MyTask, Opcode = Opcodes.Op1)]
        public void WriteInteger2(int n)
        {
            WriteEvent(2, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
        public static class Tasks
        {
            public const EventTask MyTask = (EventTask)1;
        }
        public static class Opcodes
        {
            public const EventOpcode Op1 = (EventOpcode)15;
        }
        #endregion
    }

    public sealed class EventWithOpcodeNoTaskEventSource : EventSource
    {
        [Event(1, Opcode = EventOpcode.Send)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }
    }

    public sealed class EventWithInvalidMessageEventSource : EventSource
    {
        [Event(1, Message = "Message = {0,12:G}")]
        public void WriteString(string msg)
        { WriteEvent(1, msg); }
    }
#if FEATURE_ADVANCED_MANAGED_ETW_CHANNELS
    public sealed class TooManyChannelsEventSource : EventSource
    {
        [Event(1, Channel = Channels.Analytic, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

    #region Keywords / Tasks /Opcodes / Channels
        /// <summary>
        /// The Channels definition for the ETW manifest
        /// </summary>
        public static class Channels
        {
            [Channel(Enabled = true, ChannelType = ChannelType.Admin)]
            public const EventChannel Admin = (EventChannel)16;

            [Channel(Enabled = true, ChannelType = ChannelType.Operational)]
            public const EventChannel Operational = (EventChannel)17;

            [Channel(Enabled = false, ChannelType = ChannelType.Analytic)]
            public const EventChannel Analytic = (EventChannel)18;

            [Channel(Enabled = false, ChannelType = ChannelType.Debug)]
            public const EventChannel Debug = (EventChannel)19;

            [Channel(Enabled = true, ChannelType = ChannelType.Admin)]
            public const EventChannel Admin2 = (EventChannel)20;

            [Channel(Enabled = true, ChannelType = ChannelType.Operational)]
            public const EventChannel Operational2 = (EventChannel)21;

            [Channel(Enabled = false, ChannelType = ChannelType.Analytic)]
            public const EventChannel Analytic2 = (EventChannel)22;

            [Channel(Enabled = false, ChannelType = ChannelType.Debug)]
            public const EventChannel Debug2 = (EventChannel)23;

            [Channel(Enabled = false, ChannelType = ChannelType.Debug)]
            public const EventChannel BrokeCamelsBack = (EventChannel)24;
        }
    #endregion
    }
#endif

    public sealed class EventWithAdminChannelNoMessageEventSource : EventSource
    {
        [Event(1, Channel = EventChannel.Admin, Level = EventLevel.Informational)]
        public void WriteInteger(int n)
        {
            WriteEvent(1, n);
        }

        #region Keywords / Tasks /Opcodes / Channels
#if FEATURE_ADVANCED_MANAGED_ETW_CHANNELS
        /// <summary>
        /// The Channels definition for the ETW manifest
        /// </summary>
        public static class Channels
        {
            [Channel(Enabled = true, ChannelType = ChannelType.Admin)]
            public const EventChannel Admin = (EventChannel)16;
        }
#endif
        #endregion
    }

    public abstract class AbstractWithKwdTaskOpcodeEventSource : EventSource
    {
        #region Keywords / Tasks /Opcodes / Channels
        public static class Keywords
        {
            public const EventKeywords Kwd1 = (EventKeywords)1;
        }
        public static class Tasks
        {
            public const EventTask Task1 = (EventTask)1;
        }
        public static class Opcodes
        {
            public const EventOpcode Op1 = (EventOpcode)15;
        }
        #endregion
    }

    public abstract class AbstractWithEventsEventSource : EventSource
    {
        [Event(1)]
        public void WriteInteger(int n)
        { WriteEvent(1, n); }
    }

    public interface ILogging
    {
        void Error(int errorCode, string msg);
    }

    public sealed class ImplementsInterfaceEventSource : EventSource, ILogging
    {
        public static MyLoggingEventSource Log = new MyLoggingEventSource();

        [Event(1)]
        void ILogging.Error(int errorCode, string msg)
        { WriteEvent(1, errorCode, msg); }
    }
}
