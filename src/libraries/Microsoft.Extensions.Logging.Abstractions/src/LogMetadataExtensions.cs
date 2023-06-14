// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging
{

    public delegate void FormatLogMessage<TState>(ref TState state, IBufferWriter<byte> bufferWriter);

    public static class LogMetadataExtensions
    {
        public static FormatLogMessage<TState> CreateMessageFormatter<TState>(this ILogMetadata<TState> metadata)
        {
            //TODO: this doesn't check if the names are out of order or mismatched in number
            //That never happens for LoggerMessage metadata from source compiled loggers because the generator screens for it
            //but it can happen elsewhere
            InternalCompositeFormat format = MessageFormatHelper.Parse(metadata.OriginalFormat);
            VisitPropertyListAction<TState, FormattingState> visitProperties = metadata.CreatePropertyListVisitor(new FormattingPropertyVisitorFactory());
            return FormatMessage;

            void FormatMessage(ref TState state, IBufferWriter<byte> bufferWriter)
            {
                FormattingState formattingState = new FormattingState(format, bufferWriter);
                formattingState.Init(out Span<byte> span);
                visitProperties(ref state, ref span, ref formattingState);
                formattingState.Finish(ref span);
            }
        }

        private class Slot { public PooledByteBufferWriter? Buffer; }
        private static ThreadLocal<Slot?> t_slot = new ThreadLocal<Slot?>();

        public static Func<TState, Exception?, string> CreateStringMessageFormatter<TState>(this ILogMetadata<TState> metadata)
        {
            InternalCompositeFormat format = MessageFormatHelper.Parse(metadata.OriginalFormat);
            VisitPropertyListAction<TState, FormattingState> visitProperties = metadata.CreatePropertyListVisitor(new FormattingPropertyVisitorFactory());
            return FormatMessage;

            string FormatMessage(TState state, Exception? exception)
            {
                Slot? tstate = t_slot.Value;
                if (tstate == null)
                {
                    tstate = new Slot();
                    t_slot.Value = tstate;
                }
                PooledByteBufferWriter buffer = tstate.Buffer ?? new PooledByteBufferWriter(256);
                tstate.Buffer = null;
                FormattingState formattingState = new FormattingState(format, buffer);
                formattingState.Init(out Span<byte> span);
                visitProperties(ref state, ref span, ref formattingState);
                formattingState.Finish(ref span);
                string ret = MemoryMarshal.Cast<byte, char>(buffer.WrittenMemory.Span).ToString();
                buffer.Clear();
                tstate.Buffer = buffer;
                return ret;
            }
        }
    }

    internal class FormattingPropertyVisitorFactory : IPropertyVisitorFactory<FormattingState>
    {
        public VisitPropertyAction<PropType, FormattingState> GetPropertyVisitor<PropType>()
        {
            return VisitProperty;

            static void VisitProperty(int propIndex, PropType value, ref Span<byte> span, ref FormattingState formattingState)
            {
                formattingState.AppendPropertyUtf16(ref span, value);
            }
        }
        public VisitSpanPropertyAction<FormattingState> GetSpanPropertyVisitor()
        {
            return VisitSpanProperty;
            static void VisitSpanProperty(int propIndex, scoped ReadOnlySpan<char> value, ref Span<byte> span, ref FormattingState formattingState)
            {
                formattingState.AppendSpanPropertyUtf16(ref span, value);
            }
        }
    }
}
