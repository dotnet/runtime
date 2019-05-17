// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        public static ArgumentException GetArgumentException_MaxDepthMustBePositive()
        {
            return GetArgumentException(SR.MaxDepthMustBePositive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetArgumentException(string message)
        {
            return new ArgumentException(message);
        }

        public static void ThrowArgumentException(string message)
        {
            throw GetArgumentException(message);
        }

        public static InvalidOperationException GetInvalidOperationException_CallFlushFirst(int _buffered)
        {
            return new InvalidOperationException(SR.Format(SR.CallFlushToAvoidDataLoss, _buffered));
        }

        public static void ThrowArgumentException_PropertyNameTooLarge(int tokenLength)
        {
            throw GetArgumentException(SR.Format(SR.PropertyNameTooLarge, tokenLength));
        }

        public static void ThrowArgumentException_ValueTooLarge(int tokenLength)
        {
            throw GetArgumentException(SR.Format(SR.ValueTooLarge, tokenLength));
        }

        public static void ThrowArgumentException_ValueNotSupported()
        {
            throw GetArgumentException(SR.SpecialNumberValuesNotSupported);
        }

        public static void ThrowInvalidOperationException_NeedLargerSpan()
        {
            throw GetInvalidOperationException(SR.FailedToGetLargerSpan);
        }

        public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName.Length > JsonConstants.MaxTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<byte> propertyName, int currentDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth >= JsonConstants.MaxWriterDepth)
            {
                ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, JsonConstants.MaxWriterDepth));
            }
            else
            {
                Debug.Assert(propertyName.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
        }

        public static void ThrowInvalidOperationException(int currentDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            Debug.Assert(currentDepth >= JsonConstants.MaxWriterDepth);
            ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, JsonConstants.MaxWriterDepth));
        }

        public static void ThrowInvalidOperationException(string message)
        {
            throw GetInvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetInvalidOperationException(string message)
        {
            return new InvalidOperationException(message);
        }

        public static void ThrowInvalidOperationException_DepthNonZeroOrEmptyJson(int currentDepth)
        {
            throw GetInvalidOperationException(currentDepth);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetInvalidOperationException(int currentDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth != 0)
            {
                return new InvalidOperationException(SR.Format(SR.ZeroDepthAtEnd, currentDepth));
            }
            else
            {
                return new InvalidOperationException(SR.EmptyJsonIsInvalid);
            }
        }

        public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<char> propertyName, int currentDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth >= JsonConstants.MaxWriterDepth)
            {
                ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, JsonConstants.MaxWriterDepth));
            }
            else
            {
                Debug.Assert(propertyName.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedNumber(JsonTokenType tokenType)
        {
            return GetInvalidOperationException("number", tokenType);
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedBoolean(JsonTokenType tokenType)
        {
            return GetInvalidOperationException("boolean", tokenType);
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedString(JsonTokenType tokenType)
        {
            return GetInvalidOperationException("string", tokenType);
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedStringComparison(JsonTokenType tokenType)
        {
            return GetInvalidOperationException(tokenType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetInvalidOperationException(string message, JsonTokenType tokenType)
        {
            return new InvalidOperationException(SR.Format(SR.InvalidCast, tokenType, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetInvalidOperationException(JsonTokenType tokenType)
        {
            return new InvalidOperationException(SR.Format(SR.InvalidComparison, tokenType));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static InvalidOperationException GetJsonElementWrongTypeException(
            JsonTokenType expectedType,
            JsonTokenType actualType)
        {
            return new InvalidOperationException(
                SR.Format(SR.JsonElementHasWrongType, expectedType.ToValueType(), actualType.ToValueType()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static InvalidOperationException GetJsonElementWrongTypeException(
            string expectedTypeName,
            JsonTokenType actualType)
        {
            return new InvalidOperationException(
                SR.Format(SR.JsonElementHasWrongType, expectedTypeName, actualType.ToValueType()));
        }

        public static void ThrowJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte = default, ReadOnlySpan<byte> bytes = default)
        {
            throw GetJsonReaderException(ref json, resource, nextByte, bytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static JsonException GetJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, ReadOnlySpan<byte> bytes)
        {
            string message = GetResourceString(ref json, resource, nextByte, JsonHelpers.Utf8GetString(bytes));

            long lineNumber = json.CurrentState._lineNumber;
            long bytePositionInLine = json.CurrentState._bytePositionInLine;

            message += $" LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
            return new JsonReaderException(message, lineNumber, bytePositionInLine);
        }

        private static bool IsPrintable(byte value) => value >= 0x20 && value < 0x7F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetPrintableString(byte value)
        {
            return IsPrintable(value) ? ((char)value).ToString() : $"0x{value:X2}";
        }

        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, string characters)
        {
            string character = GetPrintableString(nextByte);

            string message = "";
            switch (resource)
            {
                case ExceptionResource.ArrayDepthTooLarge:
                    message = SR.Format(SR.ArrayDepthTooLarge, json.CurrentState.Options.MaxDepth);
                    break;
                case ExceptionResource.MismatchedObjectArray:
                    message = SR.Format(SR.MismatchedObjectArray, character);
                    break;
                case ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd:
                    message = SR.TrailingCommaNotAllowedBeforeArrayEnd;
                    break;
                case ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd:
                    message = SR.TrailingCommaNotAllowedBeforeObjectEnd;
                    break;
                case ExceptionResource.EndOfStringNotFound:
                    message = SR.EndOfStringNotFound;
                    break;
                case ExceptionResource.RequiredDigitNotFoundAfterSign:
                    message = SR.Format(SR.RequiredDigitNotFoundAfterSign, character);
                    break;
                case ExceptionResource.RequiredDigitNotFoundAfterDecimal:
                    message = SR.Format(SR.RequiredDigitNotFoundAfterDecimal, character);
                    break;
                case ExceptionResource.RequiredDigitNotFoundEndOfData:
                    message = SR.RequiredDigitNotFoundEndOfData;
                    break;
                case ExceptionResource.ExpectedEndAfterSingleJson:
                    message = SR.Format(SR.ExpectedEndAfterSingleJson, character);
                    break;
                case ExceptionResource.ExpectedEndOfDigitNotFound:
                    message = SR.Format(SR.ExpectedEndOfDigitNotFound, character);
                    break;
                case ExceptionResource.ExpectedNextDigitEValueNotFound:
                    message = SR.Format(SR.ExpectedNextDigitEValueNotFound, character);
                    break;
                case ExceptionResource.ExpectedSeparatorAfterPropertyNameNotFound:
                    message = SR.Format(SR.ExpectedSeparatorAfterPropertyNameNotFound, character);
                    break;
                case ExceptionResource.ExpectedStartOfPropertyNotFound:
                    message = SR.Format(SR.ExpectedStartOfPropertyNotFound, character);
                    break;
                case ExceptionResource.ExpectedStartOfPropertyOrValueNotFound:
                    message = SR.ExpectedStartOfPropertyOrValueNotFound;
                    break;
                case ExceptionResource.ExpectedStartOfPropertyOrValueAfterComment:
                    message = SR.Format(SR.ExpectedStartOfPropertyOrValueAfterComment, character);
                    break;
                case ExceptionResource.ExpectedStartOfValueNotFound:
                    message = SR.Format(SR.ExpectedStartOfValueNotFound, character);
                    break;
                case ExceptionResource.ExpectedValueAfterPropertyNameNotFound:
                    message = SR.ExpectedValueAfterPropertyNameNotFound;
                    break;
                case ExceptionResource.FoundInvalidCharacter:
                    message = SR.Format(SR.FoundInvalidCharacter, character);
                    break;
                case ExceptionResource.InvalidEndOfJsonNonPrimitive:
                    message = SR.Format(SR.InvalidEndOfJsonNonPrimitive, json.TokenType);
                    break;
                case ExceptionResource.ObjectDepthTooLarge:
                    message = SR.Format(SR.ObjectDepthTooLarge, json.CurrentState.Options.MaxDepth);
                    break;
                case ExceptionResource.ExpectedFalse:
                    message = SR.Format(SR.ExpectedFalse, characters);
                    break;
                case ExceptionResource.ExpectedNull:
                    message = SR.Format(SR.ExpectedNull, characters);
                    break;
                case ExceptionResource.ExpectedTrue:
                    message = SR.Format(SR.ExpectedTrue, characters);
                    break;
                case ExceptionResource.InvalidCharacterWithinString:
                    message = SR.Format(SR.InvalidCharacterWithinString, character);
                    break;
                case ExceptionResource.InvalidCharacterAfterEscapeWithinString:
                    message = SR.Format(SR.InvalidCharacterAfterEscapeWithinString, character);
                    break;
                case ExceptionResource.InvalidHexCharacterWithinString:
                    message = SR.Format(SR.InvalidHexCharacterWithinString, character);
                    break;
                case ExceptionResource.EndOfCommentNotFound:
                    message = SR.EndOfCommentNotFound;
                    break;
                case ExceptionResource.ZeroDepthAtEnd:
                    message = SR.Format(SR.ZeroDepthAtEnd);
                    break;
                case ExceptionResource.ExpectedJsonTokens:
                    message = SR.ExpectedJsonTokens;
                    break;
                default:
                    Debug.Fail($"The ExceptionResource enum value: {resource} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            return message;
        }

        public static void ThrowInvalidOperationException(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
        {
            throw GetInvalidOperationException(resource, currentDepth, token, tokenType);
        }

        public static void ThrowArgumentException_InvalidCommentValue()
        {
            throw new ArgumentException(SR.CannotWriteCommentWithEmbeddedDelimiter);
        }

        public static void ThrowArgumentException_InvalidUTF8(ReadOnlySpan<byte> value)
        {
            var builder = new StringBuilder();

            int printFirst10 = Math.Min(value.Length, 10);

            for (int i = 0; i < printFirst10; i++)
            {
                byte nextByte = value[i];
                if (IsPrintable(nextByte))
                {
                    builder.Append((char)nextByte);
                }
                else
                {
                    builder.Append($"0x{nextByte:X2}");
                }
            }

            if (printFirst10 < value.Length)
            {
                builder.Append("...");
            }

            throw new ArgumentException(SR.Format(SR.CannotEncodeInvalidUTF8, builder));
        }

        public static void ThrowArgumentException_InvalidUTF16(int charAsInt)
        {
            throw new ArgumentException(SR.Format(SR.CannotEncodeInvalidUTF16, $"0x{charAsInt:X2}"));
        }

        public static void ThrowInvalidOperationException_ReadInvalidUTF16(int charAsInt)
        {
            throw new InvalidOperationException(SR.Format(SR.CannotReadInvalidUTF16, $"0x{charAsInt:X2}"));
        }

        public static void ThrowInvalidOperationException_ReadInvalidUTF16()
        {
            throw new InvalidOperationException(SR.CannotReadIncompleteUTF16);
        }

        public static InvalidOperationException GetInvalidOperationException_ReadInvalidUTF8(DecoderFallbackException innerException)
        {
            return new InvalidOperationException(SR.CannotTranscodeInvalidUtf8, innerException);
        }

        public static ArgumentException GetArgumentException_ReadInvalidUTF16(EncoderFallbackException innerException)
        {
            return new ArgumentException(SR.CannotTranscodeInvalidUtf16, innerException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetInvalidOperationException(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
        {
            string message = GetResourceString(resource, currentDepth, token, tokenType);
            return new InvalidOperationException(message);
        }

        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
        {
            string message = "";
            switch (resource)
            {
                case ExceptionResource.MismatchedObjectArray:
                    Debug.Assert(token == JsonConstants.CloseBracket || token == JsonConstants.CloseBrace);
                    message = SR.Format(SR.MismatchedObjectArray, (char)token);
                    break;
                case ExceptionResource.DepthTooLarge:
                    message = SR.Format(SR.DepthTooLarge, currentDepth & JsonConstants.RemoveFlagsBitMask, JsonConstants.MaxWriterDepth);
                    break;
                case ExceptionResource.CannotStartObjectArrayWithoutProperty:
                    message = SR.Format(SR.CannotStartObjectArrayWithoutProperty, tokenType);
                    break;
                case ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose:
                    message = SR.Format(SR.CannotStartObjectArrayAfterPrimitiveOrClose, tokenType);
                    break;
                case ExceptionResource.CannotWriteValueWithinObject:
                    message = SR.Format(SR.CannotWriteValueWithinObject, tokenType);
                    break;
                case ExceptionResource.CannotWritePropertyWithinArray:
                    message = SR.Format(SR.CannotWritePropertyWithinArray, tokenType);
                    break;
                case ExceptionResource.CannotWriteValueAfterPrimitive:
                    message = SR.Format(SR.CannotWriteValueAfterPrimitive, tokenType);
                    break;
                default:
                    Debug.Fail($"The ExceptionResource enum value: {resource} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            return message;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FormatException GetFormatException(NumericType numericType)
        {
            string message = "";

            switch (numericType)
            {
                case NumericType.Int32:
                    message = SR.FormatInt32;
                    break;
                case NumericType.Int64:
                    message = SR.FormatInt64;
                    break;
                case NumericType.UInt32:
                    message = SR.FormatUInt32;
                    break;
                case NumericType.UInt64:
                    message = SR.FormatUInt64;
                    break;
                case NumericType.Single:
                    message = SR.FormatSingle;
                    break;
                case NumericType.Double:
                    message = SR.FormatDouble;
                    break;
                case NumericType.Decimal:
                    message = SR.FormatDecimal;
                    break;
                default:
                    Debug.Fail($"The NumericType enum value: {numericType} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }
            return new FormatException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FormatException GetFormatException(DateType dateType)
        {
            string message = "";

            switch (dateType)
            {
                case DateType.DateTime:
                    message = SR.FormatDateTime;
                    break;
                case DateType.DateTimeOffset:
                    message = SR.FormatDateTimeOffset;
                    break;
                default:
                    Debug.Fail($"The DateType enum value: {dateType} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }
            return new FormatException(message);
        }
    }

    internal enum ExceptionResource
    {
        ArrayDepthTooLarge,
        EndOfCommentNotFound,
        EndOfStringNotFound,
        RequiredDigitNotFoundAfterDecimal,
        RequiredDigitNotFoundAfterSign,
        RequiredDigitNotFoundEndOfData,
        ExpectedEndAfterSingleJson,
        ExpectedEndOfDigitNotFound,
        ExpectedFalse,
        ExpectedNextDigitEValueNotFound,
        ExpectedNull,
        ExpectedSeparatorAfterPropertyNameNotFound,
        ExpectedStartOfPropertyNotFound,
        ExpectedStartOfPropertyOrValueNotFound,
        ExpectedStartOfPropertyOrValueAfterComment,
        ExpectedStartOfValueNotFound,
        ExpectedTrue,
        ExpectedValueAfterPropertyNameNotFound,
        FoundInvalidCharacter,
        InvalidCharacterWithinString,
        InvalidCharacterAfterEscapeWithinString,
        InvalidHexCharacterWithinString,
        InvalidEndOfJsonNonPrimitive,
        MismatchedObjectArray,
        ObjectDepthTooLarge,
        ZeroDepthAtEnd,
        DepthTooLarge,
        CannotStartObjectArrayWithoutProperty,
        CannotStartObjectArrayAfterPrimitiveOrClose,
        CannotWriteValueWithinObject,
        CannotWriteValueAfterPrimitive,
        CannotWritePropertyWithinArray,
        ExpectedJsonTokens,
        TrailingCommaNotAllowedBeforeArrayEnd,
        TrailingCommaNotAllowedBeforeObjectEnd,
    }

    internal enum NumericType
    {
        Int32,
        Int64,
        UInt32,
        UInt64,
        Single,
        Double,
        Decimal
    }

    internal enum DateType
    {
        DateTime,
        DateTimeOffset
    }
}
