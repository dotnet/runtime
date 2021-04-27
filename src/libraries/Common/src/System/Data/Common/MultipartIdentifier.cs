// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//------------------------------------------------------------------------------

using System.Text;

namespace System.Data.Common
{
    internal static class MultipartIdentifier
    {
        private const int MaxParts = 4;
        internal const int ServerIndex = 0;
        internal const int CatalogIndex = 1;
        internal const int SchemaIndex = 2;
        internal const int TableIndex = 3;

        /*
            Left quote strings need to correspond 1 to 1 with the right quote strings
            example: "ab" "cd",  passed in for the left and the right quote
            would set a or b as a starting quote character.
            If a is the starting quote char then c would be the ending quote char
            otherwise if b is the starting quote char then d would be the ending quote character.
        */
        internal static string?[] ParseMultipartIdentifier(string name, string leftQuote, string rightQuote, string property, bool ThrowOnEmptyMultipartName)
        {
            return ParseMultipartIdentifier(name, leftQuote, rightQuote, '.', MaxParts, true, property, ThrowOnEmptyMultipartName);
        }

        private enum MPIState
        {
            MPI_Value,
            MPI_ParseNonQuote,
            MPI_LookForSeparator,
            MPI_LookForNextCharOrSeparator,
            MPI_ParseQuote,
            MPI_RightQuote,
        }

        /* Core function  for parsing the multipart identifier string.
            * parameters: name - string to parse
            * leftquote:  set of characters which are valid quoting characters to initiate a quote
            * rightquote: set of characters which are valid to stop a quote, array index's correspond to the leftquote array.
            * separator:  separator to use
            * limit:      number of names to parse out
            * removequote:to remove the quotes on the returned string
            */
        private static void IncrementStringCount(string name, string?[] ary, ref int position, string property)
        {
            ++position;
            int limit = ary.Length;
            if (position >= limit)
            {
                throw ADP.InvalidMultipartNameToManyParts(property, name, limit);
            }
            ary[position] = string.Empty;
        }

        private static bool IsWhitespace(char ch)
        {
            return char.IsWhiteSpace(ch);
        }

        internal static string?[] ParseMultipartIdentifier(string name, string leftQuote, string rightQuote, char separator, int limit, bool removequotes, string property, bool ThrowOnEmptyMultipartName)
        {
            if (limit <= 0)
            {
                throw ADP.InvalidMultipartNameToManyParts(property, name, limit);
            }

            if (-1 != leftQuote.IndexOf(separator) || -1 != rightQuote.IndexOf(separator) || leftQuote.Length != rightQuote.Length)
            {
                throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
            }

            string?[] parsedNames = new string?[limit];   // return string array
            int stringCount = 0;                        // index of current string in the buffer
            MPIState state = MPIState.MPI_Value;        // Initialize the starting state

            StringBuilder sb = new StringBuilder(name.Length); // String buffer to hold the string being currently built, init the string builder so it will never be resized
            StringBuilder? whitespaceSB = null;                // String buffer to hold whitespace used when parsing nonquoted strings  'a b .  c d' = 'a b' and 'c d'
            char rightQuoteChar = ' ';                         // Right quote character to use given the left quote character found.
            for (int index = 0; index < name.Length; ++index)
            {
                char testchar = name[index];
                switch (state)
                {
                    case MPIState.MPI_Value:
                        {
                            int quoteIndex;
                            if (IsWhitespace(testchar))
                            {    // Is White Space then skip the whitespace
                                continue;
                            }
                            else
                            if (testchar == separator)
                            {  // If we found a separator, no string was found, initialize the string we are parsing to Empty and the next one to Empty.
                               // This is NOT a redundant setting of string.Empty it solves the case where we are parsing ".foo" and we should be returning null, null, empty, foo
                                parsedNames[stringCount] = string.Empty;
                                IncrementStringCount(name, parsedNames, ref stringCount, property);
                            }
                            else
                            if (-1 != (quoteIndex = leftQuote.IndexOf(testchar)))
                            { // If we are a left quote
                                rightQuoteChar = rightQuote[quoteIndex]; // record the corresponding right quote for the left quote
                                sb.Length = 0;
                                if (!removequotes)
                                {
                                    sb.Append(testchar);
                                }
                                state = MPIState.MPI_ParseQuote;
                            }
                            else
                            if (-1 != rightQuote.IndexOf(testchar))
                            { // If we shouldn't see a right quote
                                throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
                            }
                            else
                            {
                                sb.Length = 0;
                                sb.Append(testchar);
                                state = MPIState.MPI_ParseNonQuote;
                            }
                            break;
                        }

                    case MPIState.MPI_ParseNonQuote:
                        {
                            if (testchar == separator)
                            {
                                parsedNames[stringCount] = sb.ToString(); // set the currently parsed string
                                IncrementStringCount(name, parsedNames, ref stringCount, property);
                                state = MPIState.MPI_Value;
                            }
                            else // Quotes are not valid inside a non-quoted name
                            if (-1 != rightQuote.IndexOf(testchar))
                            {
                                throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
                            }
                            else
                            if (-1 != leftQuote.IndexOf(testchar))
                            {
                                throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
                            }
                            else
                            if (IsWhitespace(testchar))
                            { // If it is Whitespace
                                parsedNames[stringCount] = sb.ToString(); // Set the currently parsed string
                                if (null == whitespaceSB)
                                {
                                    whitespaceSB = new StringBuilder();
                                }
                                whitespaceSB.Length = 0;
                                whitespaceSB.Append(testchar);  // start to record the whitespace, if we are parsing a name like "foo bar" we should return "foo bar"
                                state = MPIState.MPI_LookForNextCharOrSeparator;
                            }
                            else
                            {
                                sb.Append(testchar);
                            }
                            break;
                        }

                    case MPIState.MPI_LookForNextCharOrSeparator:
                        {
                            if (!IsWhitespace(testchar))
                            { // If it is not whitespace
                                if (testchar == separator)
                                {
                                    IncrementStringCount(name, parsedNames, ref stringCount, property);
                                    state = MPIState.MPI_Value;
                                }
                                else
                                { // If its not a separator and not whitespace
                                    sb.Append(whitespaceSB);
                                    sb.Append(testchar);
                                    parsedNames[stringCount] = sb.ToString(); // Need to set the name here in case the string ends here.
                                    state = MPIState.MPI_ParseNonQuote;
                                }
                            }
                            else
                            {
                                whitespaceSB!.Append(testchar);
                            }
                            break;
                        }

                    case MPIState.MPI_ParseQuote:
                        {
                            if (testchar == rightQuoteChar)
                            {    // if se are on a right quote see if we are escaping the right quote or ending the quoted string
                                if (!removequotes)
                                {
                                    sb.Append(testchar);
                                }
                                state = MPIState.MPI_RightQuote;
                            }
                            else
                            {
                                sb.Append(testchar); // Append what we are currently parsing
                            }
                            break;
                        }

                    case MPIState.MPI_RightQuote:
                        {
                            if (testchar == rightQuoteChar)
                            { // If the next char is another right quote then we were escaping the right quote
                                sb.Append(testchar);
                                state = MPIState.MPI_ParseQuote;
                            }
                            else
                            if (testchar == separator)
                            {      // If its a separator then record what we've parsed
                                parsedNames[stringCount] = sb.ToString();
                                IncrementStringCount(name, parsedNames, ref stringCount, property);
                                state = MPIState.MPI_Value;
                            }
                            else
                            if (!IsWhitespace(testchar))
                            { // If it is not whitespace we got problems
                                throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
                            }
                            else
                            {                          // It is a whitespace character so the following char should be whitespace, separator, or end of string anything else is bad
                                parsedNames[stringCount] = sb.ToString();
                                state = MPIState.MPI_LookForSeparator;
                            }
                            break;
                        }

                    case MPIState.MPI_LookForSeparator:
                        {
                            if (!IsWhitespace(testchar))
                            { // If it is not whitespace
                                if (testchar == separator)
                                { // If it is a separator
                                    IncrementStringCount(name, parsedNames, ref stringCount, property);
                                    state = MPIState.MPI_Value;
                                }
                                else
                                { // Otherwise not a separator
                                    throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
                                }
                            }
                            break;
                        }
                }
            }

            // Resolve final states after parsing the string
            switch (state)
            {
                case MPIState.MPI_Value:       // These states require no extra action
                case MPIState.MPI_LookForSeparator:
                case MPIState.MPI_LookForNextCharOrSeparator:
                    break;

                case MPIState.MPI_ParseNonQuote: // Dump what ever was parsed
                case MPIState.MPI_RightQuote:
                    parsedNames[stringCount] = sb.ToString();
                    break;

                case MPIState.MPI_ParseQuote: // Invalid Ending States
                default:
                    throw ADP.InvalidMultipartNameIncorrectUsageOfQuotes(property, name);
            }

            if (parsedNames[0] == null)
            {
                if (ThrowOnEmptyMultipartName)
                {
                    throw ADP.InvalidMultipartName(property, name); // Name is entirely made up of whitespace
                }
            }
            else
            {
                // Shuffle the parsed name, from left justification to right justification, i.e. [a][b][null][null] goes to [null][null][a][b]
                int offset = limit - stringCount - 1;
                if (offset > 0)
                {
                    for (int x = limit - 1; x >= offset; --x)
                    {
                        parsedNames[x] = parsedNames[x - offset];
                        parsedNames[x - offset] = null;
                    }
                }
            }
            return parsedNames;
        }
    }
}
