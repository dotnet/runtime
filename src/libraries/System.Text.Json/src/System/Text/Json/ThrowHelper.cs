// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        // If the exception source is this value, the serializer will re-throw as JsonException.
        public const string ExceptionSourceValueToRethrowAsJsonException = "System.Text.Json.Rethrowable";

        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException_MaxDepthMustBePositive(string parameterName)
        {
            throw GetArgumentOutOfRangeException(parameterName, SR.MaxDepthMustBePositive);
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(string parameterName, string message)
        {
            return new ArgumentOutOfRangeException(parameterName, message);
        }

        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException_CommentEnumMustBeInRange(string parameterName)
        {
            throw GetArgumentOutOfRangeException(parameterName, SR.CommentHandlingMustBeValid);
        }

        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException_ArrayIndexNegative(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, SR.ArrayIndexNegative);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_ArrayTooSmall(string paramName)
        {
            throw new ArgumentException(SR.ArrayTooSmall, paramName);
        }

        private static ArgumentException GetArgumentException(string message)
        {
            return new ArgumentException(message);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException(string message)
        {
            throw GetArgumentException(message);
        }

        public static InvalidOperationException GetInvalidOperationException_CallFlushFirst(int _buffered)
        {
            return GetInvalidOperationException(SR.Format(SR.CallFlushToAvoidDataLoss, _buffered));
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_DestinationTooShort()
        {
            throw GetArgumentException(SR.DestinationTooShort);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_PropertyNameTooLarge(int tokenLength)
        {
            throw GetArgumentException(SR.Format(SR.PropertyNameTooLarge, tokenLength));
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_ValueTooLarge(long tokenLength)
        {
            throw GetArgumentException(SR.Format(SR.ValueTooLarge, tokenLength));
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_ValueNotSupported()
        {
            throw GetArgumentException(SR.SpecialNumberValuesNotSupported);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NeedLargerSpan()
        {
            throw GetInvalidOperationException(SR.FailedToGetLargerSpan);
        }

        [DoesNotReturn]
        public static void ThrowPropertyNameTooLargeArgumentException(int length)
        {
            throw GetArgumentException(SR.Format(SR.PropertyNameTooLarge, length));
        }

        [DoesNotReturn]
        public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxUnescapedTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxUnescapedTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        [DoesNotReturn]
        public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName.Length > JsonConstants.MaxUnescapedTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        [DoesNotReturn]
        public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize)
            {
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
            else
            {
                Debug.Assert(value.Length > JsonConstants.MaxUnescapedTokenSize);
                ThrowArgumentException(SR.Format(SR.ValueTooLarge, value.Length));
            }
        }

        [DoesNotReturn]
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

        [DoesNotReturn]
        public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<byte> propertyName, int currentDepth, int maxDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth >= maxDepth)
            {
                ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, maxDepth));
            }
            else
            {
                Debug.Assert(propertyName.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException(int currentDepth, int maxDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            Debug.Assert(currentDepth >= maxDepth);
            ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, maxDepth));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException(string message)
        {
            throw GetInvalidOperationException(message);
        }

        private static InvalidOperationException GetInvalidOperationException(string message)
        {
            return new InvalidOperationException(message) { Source = ExceptionSourceValueToRethrowAsJsonException };
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_DepthNonZeroOrEmptyJson(int currentDepth)
        {
            throw GetInvalidOperationException(currentDepth);
        }

        private static InvalidOperationException GetInvalidOperationException(int currentDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth != 0)
            {
                return GetInvalidOperationException(SR.Format(SR.ZeroDepthAtEnd, currentDepth));
            }
            else
            {
                return GetInvalidOperationException(SR.EmptyJsonIsInvalid);
            }
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<char> propertyName, int currentDepth, int maxDepth)
        {
            currentDepth &= JsonConstants.RemoveFlagsBitMask;
            if (currentDepth >= maxDepth)
            {
                ThrowInvalidOperationException(SR.Format(SR.DepthTooLarge, currentDepth, maxDepth));
            }
            else
            {
                Debug.Assert(propertyName.Length > JsonConstants.MaxCharacterTokenSize);
                ThrowArgumentException(SR.Format(SR.PropertyNameTooLarge, propertyName.Length));
            }
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedArray(JsonTokenType tokenType)
        {
            return GetInvalidOperationException("array", tokenType);
        }

        public static InvalidOperationException GetInvalidOperationException_ExpectedObject(JsonTokenType tokenType)
        {
            return GetInvalidOperationException("object", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedNumber(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("number", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedBoolean(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("boolean", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedString(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("string", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedPropertyName(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("propertyName", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedStringComparison(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException(tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedComment(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("comment", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_CannotSkipOnPartial()
        {
            throw GetInvalidOperationException(SR.CannotSkip);
        }

        private static InvalidOperationException GetInvalidOperationException(string message, JsonTokenType tokenType)
        {
            return GetInvalidOperationException(SR.Format(SR.InvalidCast, tokenType, message));
        }

        private static InvalidOperationException GetInvalidOperationException(JsonTokenType tokenType)
        {
            return GetInvalidOperationException(SR.Format(SR.InvalidComparison, tokenType));
        }

        [DoesNotReturn]
        internal static void ThrowJsonElementWrongTypeException(
            JsonTokenType expectedType,
            JsonTokenType actualType)
        {
            throw GetJsonElementWrongTypeException(expectedType.ToValueKind(), actualType.ToValueKind());
        }

        internal static InvalidOperationException GetJsonElementWrongTypeException(
            JsonValueKind expectedType,
            JsonValueKind actualType)
        {
            return GetInvalidOperationException(
                SR.Format(SR.JsonElementHasWrongType, expectedType, actualType));
        }

        internal static InvalidOperationException GetJsonElementWrongTypeException(
            string expectedTypeName,
            JsonValueKind actualType)
        {
            return GetInvalidOperationException(
                SR.Format(SR.JsonElementHasWrongType, expectedTypeName, actualType));
        }

        [DoesNotReturn]
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
                case ExceptionResource.NotEnoughData:
                    message = SR.NotEnoughData;
                    break;
                case ExceptionResource.ExpectedOneCompleteToken:
                    message = SR.ExpectedOneCompleteToken;
                    break;
                case ExceptionResource.InvalidCharacterAtStartOfComment:
                    message = SR.Format(SR.InvalidCharacterAtStartOfComment, character);
                    break;
                case ExceptionResource.UnexpectedEndOfDataWhileReadingComment:
                    message = SR.Format(SR.UnexpectedEndOfDataWhileReadingComment);
                    break;
                case ExceptionResource.UnexpectedEndOfLineSeparator:
                    message = SR.Format(SR.UnexpectedEndOfLineSeparator);
                    break;
                case ExceptionResource.InvalidLeadingZeroInNumber:
                    message = SR.Format(SR.InvalidLeadingZeroInNumber, character);
                    break;
                default:
                    Debug.Fail($"The ExceptionResource enum value: {resource} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            return message;
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
        {
            throw GetInvalidOperationException(resource, currentDepth, maxDepth, token, tokenType);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_InvalidCommentValue()
        {
            throw new ArgumentException(SR.CannotWriteCommentWithEmbeddedDelimiter);
        }

        [DoesNotReturn]
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

        [DoesNotReturn]
        public static void ThrowArgumentException_InvalidUTF16(int charAsInt)
        {
            throw new ArgumentException(SR.Format(SR.CannotEncodeInvalidUTF16, $"0x{charAsInt:X2}"));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ReadInvalidUTF16(int charAsInt)
        {
            throw GetInvalidOperationException(SR.Format(SR.CannotReadInvalidUTF16, $"0x{charAsInt:X2}"));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ReadIncompleteUTF16()
        {
            throw GetInvalidOperationException(SR.CannotReadIncompleteUTF16);
        }

        public static InvalidOperationException GetInvalidOperationException_ReadInvalidUTF8(DecoderFallbackException innerException)
        {
            return GetInvalidOperationException(SR.CannotTranscodeInvalidUtf8, innerException);
        }

        public static ArgumentException GetArgumentException_ReadInvalidUTF16(EncoderFallbackException innerException)
        {
            return new ArgumentException(SR.CannotTranscodeInvalidUtf16, innerException);
        }

        public static InvalidOperationException GetInvalidOperationException(string message, Exception innerException)
        {
            InvalidOperationException ex = new InvalidOperationException(message, innerException);
            ex.Source = ExceptionSourceValueToRethrowAsJsonException;
            return ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetInvalidOperationException(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
        {
            string message = GetResourceString(resource, currentDepth, maxDepth, token, tokenType);
            InvalidOperationException ex = GetInvalidOperationException(message);
            ex.Source = ExceptionSourceValueToRethrowAsJsonException;
            return ex;
        }

        [DoesNotReturn]
        public static void ThrowOutOfMemoryException(uint capacity)
        {
            throw new OutOfMemoryException(SR.Format(SR.BufferMaximumSizeExceeded, capacity));
        }

        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ExceptionResource resource, int currentDepth, int maxDepth, byte token, JsonTokenType tokenType)
        {
            string message = "";
            switch (resource)
            {
                case ExceptionResource.MismatchedObjectArray:
                    Debug.Assert(token == JsonConstants.CloseBracket || token == JsonConstants.CloseBrace);
                    message = (tokenType == JsonTokenType.PropertyName) ?
                        SR.Format(SR.CannotWriteEndAfterProperty, (char)token) :
                        SR.Format(SR.MismatchedObjectArray, (char)token);
                    break;
                case ExceptionResource.DepthTooLarge:
                    message = SR.Format(SR.DepthTooLarge, currentDepth & JsonConstants.RemoveFlagsBitMask, maxDepth);
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
                    message = (tokenType == JsonTokenType.PropertyName) ?
                        SR.Format(SR.CannotWritePropertyAfterProperty) :
                        SR.Format(SR.CannotWritePropertyWithinArray, tokenType);
                    break;
                case ExceptionResource.CannotWriteValueAfterPrimitiveOrClose:
                    message = SR.Format(SR.CannotWriteValueAfterPrimitiveOrClose, tokenType);
                    break;
                default:
                    Debug.Fail($"The ExceptionResource enum value: {resource} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            return message;
        }

        [DoesNotReturn]
        public static void ThrowFormatException()
        {
            throw new FormatException { Source = ExceptionSourceValueToRethrowAsJsonException };
        }

        public static void ThrowFormatException(NumericType numericType)
        {
            string message = "";

            switch (numericType)
            {
                case NumericType.Byte:
                    message = SR.FormatByte;
                    break;
                case NumericType.SByte:
                    message = SR.FormatSByte;
                    break;
                case NumericType.Int16:
                    message = SR.FormatInt16;
                    break;
                case NumericType.Int32:
                    message = SR.FormatInt32;
                    break;
                case NumericType.Int64:
                    message = SR.FormatInt64;
                    break;
                case NumericType.UInt16:
                    message = SR.FormatUInt16;
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

            throw new FormatException(message) { Source = ExceptionSourceValueToRethrowAsJsonException };
        }

        [DoesNotReturn]
        public static void ThrowFormatException(DataType dataType)
        {
            string message = "";

            switch (dataType)
            {
                case DataType.Boolean:
                case DataType.DateOnly:
                case DataType.DateTime:
                case DataType.DateTimeOffset:
                case DataType.TimeOnly:
                case DataType.TimeSpan:
                case DataType.Guid:
                case DataType.Version:
                    message = SR.Format(SR.UnsupportedFormat, dataType);
                    break;
                case DataType.Base64String:
                    message = SR.CannotDecodeInvalidBase64;
                    break;
                default:
                    Debug.Fail($"The DataType enum value: {dataType} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            throw new FormatException(message) { Source = ExceptionSourceValueToRethrowAsJsonException };
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ExpectedChar(JsonTokenType tokenType)
        {
            throw GetInvalidOperationException("char", tokenType);
        }

        [DoesNotReturn]
        public static void ThrowObjectDisposedException_Utf8JsonWriter()
        {
            throw new ObjectDisposedException(nameof(Utf8JsonWriter));
        }

        [DoesNotReturn]
        public static void ThrowObjectDisposedException_JsonDocument()
        {
            throw new ObjectDisposedException(nameof(JsonDocument));
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
        CannotWriteValueAfterPrimitiveOrClose,
        CannotWritePropertyWithinArray,
        ExpectedJsonTokens,
        TrailingCommaNotAllowedBeforeArrayEnd,
        TrailingCommaNotAllowedBeforeObjectEnd,
        InvalidCharacterAtStartOfComment,
        UnexpectedEndOfDataWhileReadingComment,
        UnexpectedEndOfLineSeparator,
        ExpectedOneCompleteToken,
        NotEnoughData,
        InvalidLeadingZeroInNumber,
    }

    internal enum NumericType
    {
        Byte,
        SByte,
        Int16,
        Int32,
        Int64,
        UInt16,
        UInt32,
        UInt64,
        Single,
        Double,
        Decimal
    }

    internal enum DataType
    {
        Boolean,
        DateOnly,
        DateTime,
        DateTimeOffset,
        TimeOnly,
        TimeSpan,
        Base64String,
        Guid,
        Version,
    }
}
