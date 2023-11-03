// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace WasmAppBuilder {
    public sealed class LogAdapter {
        public bool HasLoggedErrors {
            get => Helper?.HasLoggedErrors ?? _HasLoggedErrors;
        }
        private bool _HasLoggedErrors;
        private TaskLoggingHelper? Helper;
        private TextWriter? Output, ErrorOutput;

        public LogAdapter (TaskLoggingHelper helper) {
            Helper = helper;
            Output = null;
            ErrorOutput = null;
        }

        public LogAdapter () {
            Helper = null;
            Output = Console.Out;
            ErrorOutput = Console.Error;
        }

        private static string AutoFormat (string s, object[] o) {
            if ((o?.Length ?? 0) > 0)
                return string.Format(s!, o!);
            else
                return s;
        }

        public void LogMessage (string s, params object[] o) {
            Helper?.LogMessage(s, o);
            Output?.WriteLine(AutoFormat(s, o));
        }

        public void LogMessage (MessageImportance mi, string s, params object[] o) {
            Helper?.LogMessage(mi, s, o);
            Output?.WriteLine(AutoFormat(s, o));
        }

        public void Info (string code, string message, params object[] args) {
            Helper?.LogMessage(null, code, null, null, 0, 0, 0, 0, MessageImportance.Low, message, args);
            Output?.WriteLine($"info : {code}: {AutoFormat(message, args)}");
        }

        public void Warning (string code, string message, params object[] args) {
            Helper?.LogWarning(null, code, null, null, 0, 0, 0, 0, message, args);
            ErrorOutput?.WriteLine($"warning : {code}: {AutoFormat(message, args)}");
        }

        public void Error (string message) {
            Helper?.LogError(message);
            ErrorOutput?.WriteLine($"error : {message}");
            _HasLoggedErrors = true;
        }

        public void Error (string code, string message, params object[] args) {
            Helper?.LogError(null, code, null, null, 0, 0, 0, 0, message, args);
            ErrorOutput?.WriteLine($"error : {code}: {AutoFormat(message, args)}");
            _HasLoggedErrors = true;
        }
    }
}
