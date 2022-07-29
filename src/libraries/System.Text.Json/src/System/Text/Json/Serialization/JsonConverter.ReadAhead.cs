// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    public abstract partial class JsonConverter
    {
        /// <summary>
        /// Perform a Read() and if read-ahead is required, also read-ahead (to the end of the current JSON level).
        /// </summary>
        // AggressiveInlining used since this method is on a hot path and short. The optionally called
        // method DoSingleValueReadWithReadAhead is not inlined.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SingleValueReadWithReadAhead(bool requiresReadAhead, ref Utf8JsonReader reader, ref ReadStack state)
        {
            bool readAhead = requiresReadAhead && state.ReadAhead;
            if (!readAhead)
            {
                return reader.Read();
            }

            return DoSingleValueReadWithReadAhead(ref reader, ref state);
        }

        internal static bool DoSingleValueReadWithReadAhead(ref Utf8JsonReader reader, ref ReadStack state)
        {
            // When we're reading ahead we always have to save the state as we don't know if the next token
            // is an opening object or an array brace.
            Utf8JsonReader restore = reader;

            if (!reader.Read())
            {
                return false;
            }

            // Perform the actual read-ahead.
            JsonTokenType tokenType = reader.TokenType;
            if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                // Attempt to skip to make sure we have all the data we need.
                bool complete = reader.TrySkip();

                // We need to restore the state in all cases as we need to be positioned back before
                // the current token to either attempt to skip again or to actually read the value.
                reader = restore;

                if (!complete)
                {
                    // Couldn't read to the end of the object, exit out to get more data in the buffer.
                    return false;
                }

                // Success, requeue the reader to the start token.
                reader.ReadWithVerify();
                Debug.Assert(tokenType == reader.TokenType);
            }

            return true;
        }
    }
}
