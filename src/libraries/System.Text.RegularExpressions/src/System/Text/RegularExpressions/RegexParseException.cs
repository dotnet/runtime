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

        /// <summary>Gets the offset in the supplied pattern.</summary>
        public int Offset { get; }

        internal RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            Error = error;
            Offset = offset;
        }

        /// <summary>
        /// Construct a custom RegexParseException that creates a default message based on the given <see cref="RegexParseError"/> value.
        /// </summary>
        /// <param name="pattern">The pattern of the regular expression.</param>
        /// <param name="error">The <see cref="RegexParseError"/> value detailing the type of parse error.</param>
        /// <param name="offset">The offset in the regular expression where the parse error occurs.</param>
        public RegexParseException(string pattern, RegexParseError error, int offset) : base(MakeMessage(pattern, error, offset))
        {
            Error = error;
            Offset = offset;
        }

        private static string MakeMessage(string pattern, RegexParseError error, int offset)
        {
            string message;
            switch (error)
            {
                case RegexParseError.Unknown:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.Unknown);
                    break;
                case RegexParseError.AlternationHasTooManyConditions:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.TooManyAlternates);
                    break;
                case RegexParseError.AlternationHasMalformedCondition:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.IllegalCondition);
                    break;
                case RegexParseError.InvalidUnicodePropertyEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.IncompleteSlashP);
                    break;
                case RegexParseError.MalformedUnicodePropertyEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MalformedSlashP);
                    break;
                case RegexParseError.UnrecognizedEscape:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedEscape);
                    break;
                case RegexParseError.UnrecognizedControlCharacter:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedControl);
                    break;
                case RegexParseError.MissingControlCharacter:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MissingControl);
                    break;
                case RegexParseError.InsufficientOrInvalidHexDigits:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.TooFewHex);
                    break;
                case RegexParseError.QuantifierOrCaptureGroupOutOfRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.CaptureGroupOutOfRange);
                    break;
                case RegexParseError.UndefinedNamedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UndefinedNameRef);
                    break;
                case RegexParseError.UndefinedNumberedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UndefinedBackref);
                    break;
                case RegexParseError.MalformedNamedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MalformedNameRef);
                    break;
                case RegexParseError.UnescapedEndingBackslash:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.IllegalEndEscape);
                    break;
                case RegexParseError.UnterminatedComment:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnterminatedComment);
                    break;
                case RegexParseError.InvalidGroupingConstruct:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnrecognizedGrouping);
                    break;
                case RegexParseError.AlternationHasNamedCapture:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationCantCapture);
                    break;
                case RegexParseError.AlternationHasComment:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.AlternationCantHaveComment);
                    break;
                case RegexParseError.AlternationHasMalformedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.MalformedReference);
                    break;
                case RegexParseError.AlternationHasUndefinedReference:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UndefinedReference);
                    break;
                case RegexParseError.CaptureGroupNameInvalid:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.InvalidGroupName);
                    break;
                case RegexParseError.CaptureGroupOfZero:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.CapnumNotZero);
                    break;
                case RegexParseError.UnterminatedBracket:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnterminatedBracket);
                    break;
                case RegexParseError.ExclusionGroupNotLast:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.SubtractionMustBeLast);
                    break;
                case RegexParseError.ReversedCharacterRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.ReversedCharRange);
                    break;
                case RegexParseError.ShorthandClassInCharacterRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.BadClassInCharRange);
                    break;
                case RegexParseError.InsufficientClosingParentheses:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.NotEnoughParens);
                    break;
                case RegexParseError.ReversedQuantifierRange:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.IllegalRange);
                    break;
                case RegexParseError.NestedQuantifiersNotParenthesized:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.NestedQuantify);
                    break;
                case RegexParseError.QuantifierAfterNothing:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.QuantifyAfterNothing);
                    break;
                case RegexParseError.InsufficientOpeningParentheses:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.TooManyParens);
                    break;
                case RegexParseError.UnrecognizedUnicodeProperty:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.UnknownProperty);
                    break;
                default:
                    message = SR.Format(SR.MakeException, pattern, offset, SR.Unknown);
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
