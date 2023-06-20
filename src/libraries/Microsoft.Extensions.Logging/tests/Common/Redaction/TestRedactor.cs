// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Logging.Tests.Redaction
{

    internal sealed class TestRedactorProvider : IRedactorProvider
    {
        public Redactor GetRedactor(DataClassification classification) => new TestRedactor(classification);
    }

    internal sealed class TestRedactor : Redactor
    {
        private readonly string _redactedText;

        public TestRedactor(DataClassification classification)
        {
            _redactedText = $"[Redacted - {classification.Value.ToString(CultureInfo.InvariantCulture)}]";
        }

        override public int GetRedactedLength(ReadOnlySpan<char> source) => _redactedText.Length;

        override public int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            _redactedText.AsSpan().CopyTo(destination);
            return _redactedText.Length;
        }
    }
}
