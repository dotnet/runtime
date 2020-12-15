// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// An exception as a result of a parse error in a regular expression <see cref="RegularExpressions"/>, with
    /// detailed information in the <see cref="Error"/> and <see cref="Offset"/> properties.
    /// </summary>
    [Serializable]
    public sealed class RegexParseException : ArgumentException
    {
        /// <summary>Gets the error that happened during parsing.</summary>
        public RegexParseError Error { get; }

        /// <summary>Gets the zero-based character offset in the regular expression pattern where the parse error occurs.</summary>
        public int Offset { get; }

        internal RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            Error = error;
            Offset = offset;
        }

        internal RegexParseException(string pattern, RegexParseError error, int offset) : base(MakeMessage(pattern, error, offset))
        {
            Error = error;
            Offset = offset;
        }

        /// <summary>
        /// Construct a <see cref="RegexParseException"/> that creates a default message based on the given <see cref="RegexParseError"/> value.
        /// </summary>
        /// <param name="pattern">The pattern of the regular expression.</param>
        /// <param name="error">The <see cref="RegexParseError"/> value detailing the type of parse error.</param>
        /// <param name="offset">The zero-based offset in the regular expression where the parse error occurs.</param>
        private static string MakeMessage(string pattern, RegexParseError error, int offset)
        {
            string message;
            switch (error)
            {
                case RegexParseError.Unknown:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.Generic);
                    break;
                case RegexParseError.AlternationHasTooManyConditions:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasTooManyConditions);
                    break;
                case RegexParseError.AlternationHasMalformedCondition:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasMalformedCondition);
                    break;
                case RegexParseError.InvalidUnicodePropertyEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InvalidUnicodePropertyEscape);
                    break;
                case RegexParseError.MalformedUnicodePropertyEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MalformedUnicodePropertyEscape);
                    break;
                case RegexParseError.UnrecognizedEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedEscape);
                    break;
                case RegexParseError.UnrecognizedControlCharacter:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedControlCharacter);
                    break;
                case RegexParseError.MissingControlCharacter:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MissingControlCharacter);
                    break;
                case RegexParseError.InsufficientOrInvalidHexDigits:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InsufficientOrInvalidHexDigits);
                    break;
                case RegexParseError.QuantifierOrCaptureGroupOutOfRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.QuantifierOrCaptureGroupOutOfRange);
                    break;
                case RegexParseError.UndefinedNamedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UndefinedNamedReferenceNoPlaceholder);
                    break;
                case RegexParseError.UndefinedNumberedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UndefinedNumberedReferenceNoPlaceholder);
                    break;
                case RegexParseError.MalformedNamedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MalformedNamedReference);
                    break;
                case RegexParseError.UnescapedEndingBackslash:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnescapedEndingBackslash);
                    break;
                case RegexParseError.UnterminatedComment:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnterminatedComment);
                    break;
                case RegexParseError.InvalidGroupingConstruct:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InvalidGroupingConstruct);
                    break;
                case RegexParseError.AlternationHasNamedCapture:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasNamedCapture);
                    break;
                case RegexParseError.AlternationHasComment:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasComment);
                    break;
                case RegexParseError.AlternationHasMalformedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasMalformedReferenceNoPlaceholder);
                    break;
                case RegexParseError.AlternationHasUndefinedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationHasUndefinedReferenceNoPlaceholder);
                    break;
                case RegexParseError.CaptureGroupNameInvalid:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.CaptureGroupNameInvalid);
                    break;
                case RegexParseError.CaptureGroupOfZero:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.CaptureGroupOfZero);
                    break;
                case RegexParseError.UnterminatedBracket:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnterminatedBracket);
                    break;
                case RegexParseError.ExclusionGroupNotLast:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.ExclusionGroupNotLast);
                    break;
                case RegexParseError.ReversedCharacterRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.ReversedCharacterRange);
                    break;
                case RegexParseError.ShorthandClassInCharacterRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.ShorthandClassInCharacterRangeNoPlaceholder);
                    break;
                case RegexParseError.InsufficientClosingParentheses:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InsufficientClosingParentheses);
                    break;
                case RegexParseError.ReversedQuantifierRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.ReversedQuantifierRange);
                    break;
                case RegexParseError.NestedQuantifiersNotParenthesized:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.NestedQuantifiersNotParenthesizedNoPlaceholder);
                    break;
                case RegexParseError.QuantifierAfterNothing:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.QuantifierAfterNothing);
                    break;
                case RegexParseError.InsufficientOpeningParentheses:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InsufficientOpeningParentheses);
                    break;
                case RegexParseError.UnrecognizedUnicodeProperty:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedUnicodePropertyNoPlaceholder);
                    break;
                default:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.Generic);
                    break;
            }

            return message;
        }

        private RegexParseException(SerializationInfo info, StreamingContext context)
        {
            // It means someone modified the payload.
            throw new NotImplementedException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.SetType(typeof(ArgumentException)); // To maintain serialization support with .NET Framework.
        }
    }
}
