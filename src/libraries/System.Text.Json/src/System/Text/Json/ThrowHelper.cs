// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static class ThrowHelper
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetInvalidOperationException(string message, JsonTokenType tokenType)
        {
            return new InvalidOperationException(SR.Format(SR.InvalidCast, tokenType, message));
        }

        public static void ThrowJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte = default, ReadOnlySpan<byte> bytes = default)
        {
            throw GetJsonReaderException(ref json, resource, nextByte, bytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static JsonReaderException GetJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, ReadOnlySpan<byte> bytes)
        {
            string message = GetResourceString(ref json, resource, nextByte, Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Length));

            long lineNumber = json.CurrentState._lineNumber;
            long bytePositionInLine = json.CurrentState._bytePositionInLine;

            message += $" LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
            return new JsonReaderException(message, lineNumber, bytePositionInLine);
        }

        private static bool IsPrintable(byte value) => value >= 0x20 && value < 0x7F;

        // This function will convert an ExceptionResource enum value to the resource string.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, string characters)
        {
            string character = IsPrintable(nextByte) ? ((char)nextByte).ToString() : $"0x{nextByte:X2}";

            string message = "";
            switch (resource)
            {
                case ExceptionResource.ArrayDepthTooLarge:
                    message = SR.Format(SR.ArrayDepthTooLarge, json.CurrentDepth + 1, json.CurrentState.MaxDepth);
                    break;
                case ExceptionResource.MismatchedObjectArray:
                    message = SR.Format(SR.MismatchedObjectArray, character);
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
                    message = SR.Format(SR.ObjectDepthTooLarge, json.CurrentDepth + 1, json.CurrentState.MaxDepth);
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
                    message = SR.Format(SR.ZeroDepthAtEnd, json.CurrentDepth);
                    break;
                default:
                    Debug.Fail($"The ExceptionResource enum value: {resource} is not part of the switch. Add the appropriate case and exception message.");
                    break;
            }

            return message;
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
    }
}
