// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
#if ES_BUILD_STANDALONE
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
#endif
    internal struct EventDescriptor
    {
        #region private
        [FieldOffset(0)]
        private readonly int m_traceloggingId;
        [FieldOffset(0)]
        private readonly ushort m_id;
        [FieldOffset(2)]
        private readonly byte m_version;
        [FieldOffset(3)]
        private readonly byte m_channel;
        [FieldOffset(4)]
        private readonly byte m_level;
        [FieldOffset(5)]
        private readonly byte m_opcode;
        [FieldOffset(6)]
        private readonly ushort m_task;
        [FieldOffset(8)]
        private readonly long m_keywords;
        #endregion

        public EventDescriptor(
                int traceloggingId,
                byte level,
                byte opcode,
                long keywords
                )
        {
            this.m_id = 0;
            this.m_version = 0;
            this.m_channel = 0;
            this.m_traceloggingId = traceloggingId;
            this.m_level = level;
            this.m_opcode = opcode;
            this.m_task = 0;
            this.m_keywords = keywords;
        }

        public EventDescriptor(
                int id,
                byte version,
                byte channel,
                byte level,
                byte opcode,
                int task,
                long keywords
                )
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (id > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(id), SR.Format(SR.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
            }

            m_traceloggingId = 0;
            m_id = (ushort)id;
            m_version = version;
            m_channel = channel;
            m_level = level;
            m_opcode = opcode;
            m_keywords = keywords;

            if (task < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(task), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (task > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(task), SR.Format(SR.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
            }

            m_task = (ushort)task;
        }

        public int EventId => m_id;
        public byte Version => m_version;
        public byte Channel => m_channel;
        public byte Level => m_level;
        public byte Opcode => m_opcode;
        public int Task => m_task;
        public long Keywords => m_keywords;

        internal int TraceLoggingId => m_traceloggingId;

        public override bool Equals(object? obj) =>
            obj is EventDescriptor ed && Equals(ed);

        public override int GetHashCode() =>
            m_id ^ m_version ^ m_channel ^ m_level ^ m_opcode ^ m_task ^ (int)m_keywords;

        public bool Equals(EventDescriptor other) =>
            m_id == other.m_id &&
            m_version == other.m_version &&
            m_channel == other.m_channel &&
            m_level == other.m_level &&
            m_opcode == other.m_opcode &&
            m_task == other.m_task &&
            m_keywords == other.m_keywords;

        public static bool operator ==(EventDescriptor event1, EventDescriptor event2) =>
            event1.Equals(event2);

        public static bool operator !=(EventDescriptor event1, EventDescriptor event2) =>
            !event1.Equals(event2);
    }
}
