// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.Extensions.Logging.LoggerMessage;

namespace Microsoft.Extensions.Logging
{
    internal class LogValuesMetadataBase
    {
        private readonly LogPropertyInfo[]? _metadata;

        public LogValuesMetadataBase(string format, LogLevel level, EventId eventId, object[]?[]? metadata)
        {
            ThrowHelper.ThrowIfNull(format);

            OriginalFormat = format;
            _ = MessageFormatHelper.Parse(format, out _metadata);
            if (metadata != null && _metadata != null)
            {
                for (int i = 0; i < _metadata.Length; i++)
                {
                    _metadata[i].Metadata = metadata[i];
                }
            }

            LogLevel = level;
            EventId = eventId;
        }

        public string OriginalFormat { get; private set; }
        public int PropertyCount => _metadata != null ? _metadata.Length : 0;
        public string GetValueName(int index) => _metadata![index].Name;
        public LogPropertyInfo GetPropertyInfo(int index) => _metadata![index];
        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
    }

    internal class LogValuesMetadata : LogValuesMetadataBase, ILogMetadata<LogValues>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> _)
        {
            return Visit;

            static void Visit(ref LogValues value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
            }
        }

        public Func<LogValues, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1> : LogValuesMetadataBase, ILogMetadata<LogValues<T1>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            return Visit;

            void Visit(ref LogValues<T1> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1>, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1, T2> : LogValuesMetadataBase, ILogMetadata<LogValues<T1, T2>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1, T2>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = visitorFactory.GetPropertyVisitor<T2>();
            return Visit;

            void Visit(ref LogValues<T1, T2> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
                visit1(1, value._value1, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1, T2>, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1, T2, T3> : LogValuesMetadataBase, ILogMetadata<LogValues<T1, T2, T3>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1, T2, T3>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = visitorFactory.GetPropertyVisitor<T2>();
            VisitPropertyAction<T3, TCookie> visit2 = visitorFactory.GetPropertyVisitor<T3>();
            return Visit;

            void Visit(ref LogValues<T1, T2, T3> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
                visit1(1, value._value1, ref spanCookie, ref cookie);
                visit2(2, value._value2, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1, T2, T3>, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1, T2, T3, T4> : LogValuesMetadataBase, ILogMetadata<LogValues<T1, T2, T3, T4>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1, T2, T3, T4>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = visitorFactory.GetPropertyVisitor<T2>();
            VisitPropertyAction<T3, TCookie> visit2 = visitorFactory.GetPropertyVisitor<T3>();
            VisitPropertyAction<T4, TCookie> visit3 = visitorFactory.GetPropertyVisitor<T4>();
            return Visit;

            void Visit(ref LogValues<T1, T2, T3, T4> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
                visit1(1, value._value1, ref spanCookie, ref cookie);
                visit2(2, value._value2, ref spanCookie, ref cookie);
                visit3(3, value._value3, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1, T2, T3, T4>, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1, T2, T3, T4, T5> : LogValuesMetadataBase, ILogMetadata<LogValues<T1, T2, T3, T4, T5>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1, T2, T3, T4, T5>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = visitorFactory.GetPropertyVisitor<T2>();
            VisitPropertyAction<T3, TCookie> visit2 = visitorFactory.GetPropertyVisitor<T3>();
            VisitPropertyAction<T4, TCookie> visit3 = visitorFactory.GetPropertyVisitor<T4>();
            VisitPropertyAction<T5, TCookie> visit4 = visitorFactory.GetPropertyVisitor<T5>();
            return Visit;

            void Visit(ref LogValues<T1, T2, T3, T4, T5> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
                visit1(1, value._value1, ref spanCookie, ref cookie);
                visit2(2, value._value2, ref spanCookie, ref cookie);
                visit3(3, value._value3, ref spanCookie, ref cookie);
                visit4(4, value._value4, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1, T2, T3, T4, T5>, Exception?, string> MessageFormatter { get; }
    }

    internal class LogValuesMetadata<T1, T2, T3, T4, T5, T6> : LogValuesMetadataBase, ILogMetadata<LogValues<T1, T2, T3, T4, T5, T6>>
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata)
        {
            MessageFormatter = LogMetadataExtensions.CreateStringMessageFormatter(this);
        }

        public VisitPropertyListAction<LogValues<T1, T2, T3, T4, T5, T6>, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> visitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = visitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = visitorFactory.GetPropertyVisitor<T2>();
            VisitPropertyAction<T3, TCookie> visit2 = visitorFactory.GetPropertyVisitor<T3>();
            VisitPropertyAction<T4, TCookie> visit3 = visitorFactory.GetPropertyVisitor<T4>();
            VisitPropertyAction<T5, TCookie> visit4 = visitorFactory.GetPropertyVisitor<T5>();
            VisitPropertyAction<T6, TCookie> visit5 = visitorFactory.GetPropertyVisitor<T6>();
            return Visit;

            void Visit(ref LogValues<T1, T2, T3, T4, T5, T6> value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value._value0, ref spanCookie, ref cookie);
                visit1(1, value._value1, ref spanCookie, ref cookie);
                visit2(2, value._value2, ref spanCookie, ref cookie);
                visit3(3, value._value3, ref spanCookie, ref cookie);
                visit4(4, value._value4, ref spanCookie, ref cookie);
                visit5(5, value._value5, ref spanCookie, ref cookie);
            }
        }

        public Func<LogValues<T1, T2, T3, T4, T5, T6>, Exception?, string> MessageFormatter { get; }
    }
}
