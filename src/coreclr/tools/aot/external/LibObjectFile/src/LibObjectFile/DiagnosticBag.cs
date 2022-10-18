// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile
{
    /// <summary>
    /// A container for <see cref="DiagnosticMessage"/> used for error reporting while reading/writing object files.
    /// </summary>
    [DebuggerDisplay("Count = {Messages.Count}, HasErrors = {" + nameof(HasErrors) + "}")]
    public class DiagnosticBag
    {
        private readonly List<DiagnosticMessage> _messages;

        public DiagnosticBag()
        {
            _messages = new List<DiagnosticMessage>();
        }

        /// <summary>
        /// List of messages.
        /// </summary>
        public IReadOnlyList<DiagnosticMessage> Messages => _messages;

        /// <summary>
        /// If this instance contains error messages.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Clear all messages.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            HasErrors = false;
        }

        /// <summary>
        /// Copy all the <see cref="Messages"/> in this bag to another bag.
        /// </summary>
        /// <param name="diagnostics">The diagnostics receiving the copy of the <see cref="Messages"/></param>
        public void CopyTo(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            foreach (var diagnosticMessage in Messages)
            {
                diagnostics.Log(diagnosticMessage);
            }
        }
        
        /// <summary>
        /// Logs the specified <see cref="DiagnosticMessage"/>.
        /// </summary>
        /// <param name="message">The diagnostic message</param>
        public void Log(DiagnosticMessage message)
        {
            if (message.Message == null) throw new InvalidOperationException($"{nameof(DiagnosticMessage)}.{nameof(DiagnosticMessage.Message)} cannot be null");
            _messages.Add(message);
            if (message.Kind == DiagnosticKind.Error)
            {
                HasErrors = true;
            }
        }

        /// <summary>
        /// Log an error <see cref="DiagnosticMessage"/>.
        /// </summary>
        /// <param name="id">The identifier of the diagnostic.</param>
        /// <param name="message">The text of the message</param>
        /// <param name="context">An optional context</param>
        public void Error(DiagnosticId id, string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Error, id, message, context));
        }

        /// <summary>
        /// Log an error <see cref="DiagnosticMessage"/>.
        /// </summary>
        /// <param name="id">The identifier of the diagnostic.</param>
        /// <param name="message">The text of the message</param>
        /// <param name="context">An optional context</param>
        public void Warning(DiagnosticId id, string message, object context = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Log(new DiagnosticMessage(DiagnosticKind.Warning, id, message, context));
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var diagnosticMessage in Messages)
            {
                builder.AppendLine(diagnosticMessage.ToString());
            }

            return builder.ToString();
        }
    }
}