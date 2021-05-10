// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Internal.TypeSystem;

namespace ILCompiler.Logging
{
    public enum MessageCategory
    {
        Error = 0,
        Warning,
        Info,
        Diagnostic,

        WarningAsError = 0xFF
    }

    public readonly struct MessageContainer
#if false
        : IComparable<MessageContainer>, IEquatable<MessageContainer>
#endif
    {
        /// <summary>
        /// Optional data with a filename, line and column that triggered the
        /// linker to output an error (or warning) message.
        /// </summary>
        public MessageOrigin? Origin { get; }

        public MessageCategory Category { get; }

        /// <summary>
        /// Further categorize the message.
        /// </summary>
        public string SubCategory { get; }

        /// <summary>
        /// Code identifier for errors and warnings reported by the IL linker.
        /// </summary>
        public int? Code { get; }

        /// <summary>
        /// User friendly text describing the error or warning.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Create an error message.
        /// </summary>
        /// <param name="text">Humanly readable message describing the error</param>
        /// <param name="code">Unique error ID. Please see https://github.com/mono/linker/blob/main/doc/error-codes.md
        /// for the list of errors and possibly add a new one</param>
        /// <param name="subcategory">Optionally, further categorize this error</param>
        /// <param name="origin">Filename, line, and column where the error was found</param>
        /// <returns>New MessageContainer of 'Error' category</returns>
        internal static MessageContainer CreateErrorMessage(string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
        {
            if (!(code >= 1000 && code <= 2000))
                throw new ArgumentOutOfRangeException(nameof(code), $"The provided code '{code}' does not fall into the error category, which is in the range of 1000 to 2000 (inclusive).");

            return new MessageContainer(MessageCategory.Error, text, code, subcategory, origin);
        }

        /// <summary>
        /// Create a warning message.
        /// </summary>
        /// <param name="context">Context with the relevant warning suppression info.</param>
        /// <param name="text">Humanly readable message describing the warning</param>
        /// <param name="code">Unique warning ID. Please see https://github.com/mono/linker/blob/main/doc/error-codes.md
        /// for the list of warnings and possibly add a new one</param>
        /// /// <param name="origin">Filename or member where the warning is coming from</param>
        /// <param name="subcategory">Optionally, further categorize this warning</param>
        /// <param name="version">Optional warning version number. Versioned warnings can be controlled with the
        /// warning wave option --warn VERSION. Unversioned warnings are unaffected by this option. </param>
        /// <returns>New MessageContainer of 'Warning' category</returns>
        internal static MessageContainer? CreateWarningMessage(Logger context, string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            //if (!(code > 2000 && code <= 6000))
            //    throw new ArgumentOutOfRangeException(nameof(code), $"The provided code '{code}' does not fall into the warning category, which is in the range of 2001 to 6000 (inclusive).");

            return CreateWarningMessageContainer(context, text, code, origin, subcategory);
        }

        private static MessageContainer? CreateWarningMessageContainer(Logger context, string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            if (context.IsWarningSuppressed(code, origin))
                return null;

            if (context.IsWarningAsError(code))
                return new MessageContainer(MessageCategory.WarningAsError, text, code, subcategory, origin);

            return new MessageContainer(MessageCategory.Warning, text, code, subcategory, origin);
        }

        /// <summary>
        /// Create a info message.
        /// </summary>
        /// <param name="text">Humanly readable message</param>
        /// <returns>New MessageContainer of 'Info' category</returns>
        public static MessageContainer CreateInfoMessage(string text)
        {
            return new MessageContainer(MessageCategory.Info, text, null);
        }

        /// <summary>
        /// Create a diagnostics message.
        /// </summary>
        /// <param name="text">Humanly readable message</param>
        /// <returns>New MessageContainer of 'Diagnostic' category</returns>
        public static MessageContainer CreateDiagnosticMessage(string text)
        {
            return new MessageContainer(MessageCategory.Diagnostic, text, null);
        }

        private MessageContainer(MessageCategory category, string text, int? code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
        {
            Code = code;
            Category = category;
            Origin = origin;
            SubCategory = subcategory;
            Text = text;
        }

        public override string ToString() => ToMSBuildString();

        public string ToMSBuildString()
        {
            const string originApp = "ILC";
            string origin = Origin?.ToString() ?? originApp;

            StringBuilder sb = new StringBuilder();
            sb.Append(origin).Append(":");

            if (!string.IsNullOrEmpty(SubCategory))
                sb.Append(" ").Append(SubCategory);

            string cat;
            switch (Category)
            {
                case MessageCategory.Error:
                case MessageCategory.WarningAsError:
                    cat = "error";
                    break;
                case MessageCategory.Warning:
                    cat = "warning";
                    break;
                default:
                    cat = "";
                    break;
            }

            if (!string.IsNullOrEmpty(cat))
            {
                sb.Append(" ")
                    .Append(cat)
                    .Append(" IL")
                    .Append(Code.Value.ToString("D4"))
                    .Append(": ");
            }
            else
            {
                sb.Append(" ");
            }

            if (Origin?.MemberDefinition != null)
            {
                if (Origin?.MemberDefinition is MethodDesc method)
                    sb.Append(method.GetDisplayName());
                else 
                    sb.Append(Origin?.MemberDefinition.ToString());

                sb.Append(": ");
            }

            // Expected output $"{FileName(SourceLine, SourceColumn)}: {SubCategory}{Category} IL{Code}: ({MemberDisplayName}: ){Text}");
            sb.Append(Text);
            return sb.ToString();
        }

#if false
        public bool Equals(MessageContainer other) =>
            (Category, Text, Code, SubCategory, Origin) == (other.Category, other.Text, other.Code, other.SubCategory, other.Origin);

        public override bool Equals(object obj) => obj is MessageContainer messageContainer && Equals(messageContainer);
        public override int GetHashCode() => (Category, Text, Code, SubCategory, Origin).GetHashCode();

        public int CompareTo(MessageContainer other)
        {
            if (Origin != null && other.Origin != null)
            {
                return Origin.Value.CompareTo(other.Origin.Value);
            }
            else if (Origin == null && other.Origin == null)
            {
                return (Code < other.Code) ? -1 : 1;
            }

            return (Origin == null) ? 1 : -1;
        }

        public static bool operator ==(MessageContainer lhs, MessageContainer rhs) => lhs.Equals(rhs);
        public static bool operator !=(MessageContainer lhs, MessageContainer rhs) => !lhs.Equals(rhs);
#endif
    }
}
