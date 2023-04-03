// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILLink.Shared;

using Debug = System.Diagnostics.Debug;

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
        /// Optional data with a filename, line and column that triggered
        /// to output an error (or warning) message.
        /// </summary>
        public MessageOrigin? Origin { get; }

        public MessageCategory Category { get; }

        /// <summary>
        /// Further categorize the message.
        /// </summary>
        public string SubCategory { get; }

        /// <summary>
        /// Code identifier for errors and warnings.
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
        /// <param name="code">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
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
        /// Create an error message.
        /// </summary>
        /// <param name="origin">Filename, line, and column where the error was found</param>
        /// <param name="id">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
        /// for the list of errors and possibly add a new one</param>
        /// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
        /// <returns>New MessageContainer of 'Error' category</returns>
        internal static MessageContainer CreateErrorMessage(MessageOrigin? origin, DiagnosticId id, params string[] args)
        {
            if (!((int)id >= 1000 && (int)id <= 2000))
                throw new ArgumentOutOfRangeException(nameof(id), $"The provided code '{(int)id}' does not fall into the error category, which is in the range of 1000 to 2000 (inclusive).");

            return new MessageContainer(MessageCategory.Error, id, origin: origin, args: args);
        }

        /// <summary>
        /// Create a warning message.
        /// </summary>
        /// <param name="context">Context with the relevant warning suppression info.</param>
        /// <param name="text">Humanly readable message describing the warning</param>
        /// <param name="code">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
        /// for the list of warnings and possibly add a new one</param>
        /// /// <param name="origin">Filename or member where the warning is coming from</param>
        /// <param name="subcategory">Optionally, further categorize this warning</param>
        /// <param name="version">Optional warning version number. Versioned warnings can be controlled with the
        /// warning wave option --warn VERSION. Unversioned warnings are unaffected by this option. </param>
        /// <returns>New MessageContainer of 'Warning' category</returns>
        internal static MessageContainer? CreateWarningMessage(Logger context, string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            if (!(code > 2000 && code <= 6000))
                throw new ArgumentOutOfRangeException(nameof(code), $"The provided code '{code}' does not fall into the warning category, which is in the range of 2001 to 6000 (inclusive).");

            return CreateWarningMessageContainer(context, text, code, origin, subcategory);
        }

        /// <summary>
        /// Create a warning message.
        /// </summary>
        /// <param name="context">Context with the relevant warning suppression info.</param>
        /// <param name="origin">Filename or member where the warning is coming from</param>
        /// <param name="id">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
        /// for the list of warnings and possibly add a new one</param>
        /// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
        /// <returns>New MessageContainer of 'Warning' category</returns>
        internal static MessageContainer? CreateWarningMessage(Logger context, MessageOrigin origin, DiagnosticId id, params string[] args)
        {
            if (!((int)id > 2000 && (int)id <= 6000))
                throw new ArgumentOutOfRangeException(nameof(id), $"The provided code '{(int)id}' does not fall into the warning category, which is in the range of 2001 to 6000 (inclusive).");

            return CreateWarningMessageContainer(context, origin, id, id.GetDiagnosticSubcategory(), args);
        }

        private static MessageContainer? CreateWarningMessageContainer(Logger context, string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            if (context.IsWarningSuppressed(code, origin))
                return null;

            if (context.IsWarningSubcategorySuppressed(subcategory))
                return null;

            if (TryLogSingleWarning(context, code, origin, subcategory))
                return null;

            if (Logger.IsWarningAsError(code))
                return new MessageContainer(MessageCategory.WarningAsError, text, code, subcategory, origin);

            return new MessageContainer(MessageCategory.Warning, text, code, subcategory, origin);
        }

        private static MessageContainer? CreateWarningMessageContainer(Logger context, MessageOrigin origin, DiagnosticId id, string subcategory, params string[] args)
        {
            if (context.IsWarningSuppressed((int)id, origin))
                return null;

            if (context.IsWarningSubcategorySuppressed(subcategory))
                return null;

            if (TryLogSingleWarning(context, (int)id, origin, subcategory))
                return null;

            if (Logger.IsWarningAsError((int)id))
                return new MessageContainer(MessageCategory.WarningAsError, id, subcategory, origin, args);

            return new MessageContainer(MessageCategory.Warning, id, subcategory, origin, args);
        }

        private static bool TryLogSingleWarning(Logger context, int code, MessageOrigin origin, string subcategory)
        {
            if (subcategory != MessageSubCategory.AotAnalysis && subcategory != MessageSubCategory.TrimAnalysis)
                return false;

            var declaringType = origin.MemberDefinition switch
            {
                TypeDesc type => type,
                MethodDesc method => method.OwningType,
                FieldDesc field => field.OwningType,
#if !READYTORUN
                PropertyPseudoDesc property => property.OwningType,
                EventPseudoDesc @event => @event.OwningType,
#endif
                _ => null,
            };

            ModuleDesc declaringAssembly = (declaringType as MetadataType)?.Module ?? (origin.MemberDefinition as ModuleDesc);
            Debug.Assert(declaringAssembly != null);
            if (declaringAssembly == null)
                return false;

            // Any IL2026 warnings left in an assembly with an IsTrimmable attribute are considered intentional
            // and should not be collapsed, so that the user-visible RUC message gets printed.
            if (code == 2026 && IsTrimmableAssembly(declaringAssembly))
                return false;

            if (context.IsSingleWarn(declaringAssembly, subcategory))
                return true;

            return false;
        }

        private static bool IsTrimmableAssembly(ModuleDesc assembly)
        {
            if (assembly is EcmaAssembly ecmaAssembly)
            {
                foreach (var attribute in ecmaAssembly.GetDecodedCustomAttributes("System.Reflection", "AssemblyMetadataAttribute"))
                {
                    if (attribute.FixedArguments.Length != 2)
                        continue;

                    if (!attribute.FixedArguments[0].Type.IsString
                        || !((string)(attribute.FixedArguments[0].Value)).Equals("IsTrimmable", StringComparison.Ordinal))
                        continue;

                    if (!attribute.FixedArguments[1].Type.IsString)
                        continue;

                    string value = (string)attribute.FixedArguments[1].Value;

                    if (value.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        //LogWarning($"Invalid AssemblyMetadata(\"IsTrimmable\", \"{args[1].Value}\") attribute in assembly '{assembly.Name.Name}'. Value must be \"True\"", 2102, GetAssemblyLocation(assembly));
                    }
                }
            }

            return false;
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

        private MessageContainer(MessageCategory category, DiagnosticId id, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null, params string[] args)
        {
            Code = (int)id;
            Category = category;
            Origin = origin;
            SubCategory = subcategory;
            Text = new DiagnosticString(id).GetMessage(args);
        }

        public override string ToString() => ToMSBuildString();

        public string ToMSBuildString()
        {
            const string originApp = "ILC";
            string origin = Origin?.ToString() ?? originApp;

            StringBuilder sb = new StringBuilder();
            sb.Append(origin).Append(':');

            if (!string.IsNullOrEmpty(SubCategory))
                sb.Append(' ').Append(SubCategory);

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
                sb.Append(' ')
                    .Append(cat)
                    .Append(" IL")
                    .Append(Code.Value.ToString("D4"))
                    .Append(": ");
            }
            else
            {
                sb.Append(' ');
            }

            if (Origin?.MemberDefinition != null)
            {
                sb.Append(Origin?.MemberDefinition?.GetDisplayName() ?? Origin?.MemberDefinition?.ToString());
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
