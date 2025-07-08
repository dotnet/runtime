// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.XmlSerializer.Generator
{
    internal sealed class InfoTextWriter : TextWriter
    {
        private readonly TaskLoggingHelper _log;

        public InfoTextWriter(TaskLoggingHelper log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogMessage(MessageImportance.High, value);
            }
        }

        public override void WriteLine(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogMessage(MessageImportance.High, value);
            }
        }
    }

    internal sealed class WarningTextWriter : TextWriter
    {
        private readonly TaskLoggingHelper _log;

        public WarningTextWriter(TaskLoggingHelper log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogWarning(value);
            }
        }

        public override void WriteLine(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogWarning(value);
            }
        }
    }

    internal sealed class ErrorTextWriter : TextWriter
    {
        private readonly TaskLoggingHelper _log;

        public ErrorTextWriter(TaskLoggingHelper log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogError(value);
            }
        }

        public override void WriteLine(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _log.LogError(value);
            }
        }
    }
}
