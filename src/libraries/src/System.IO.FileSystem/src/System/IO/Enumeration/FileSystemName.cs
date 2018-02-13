// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace System.IO.Enumeration
{
    /// <summary>
    /// Provides methods for matching file system names.
    /// </summary>
    public static class FileSystemName
    {
        // [MS - FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression
        // https://msdn.microsoft.com/en-us/library/ff469270.aspx
        private static readonly char[] s_wildcardChars =
        {
            '\"', '<', '>', '*', '?'
        };

        private static readonly char[] s_simpleWildcardChars =
        {
            '*', '?'
        };

        /// <summary>
        /// Change '*' and '?' to '&lt;', '&gt;' and '"' to match Win32 behavior. For compatibility, Windows
        /// changes some wildcards to provide a closer match to historical DOS 8.3 filename matching.
        /// </summary>
        public static string TranslateDosExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression) || expression == "*" || expression == "*.*")
                return "*";

            bool modified = false;
            Span<char> stackSpace = stackalloc char[expression.Length];
            ValueStringBuilder sb = new ValueStringBuilder(stackSpace);
            int length = expression.Length;
            for (int i = 0; i < length; i++)
            {
                char c = expression[i];
                switch (c)
                {
                    case '.':
                        modified = true;
                        if (i > 1 && i == length - 1 && expression[i - 1] == '*')
                        {
                            sb.Length--;
                            sb.Append('<'); // DOS_STAR (ends in *.)
                        }
                        else if (i < length - 1 && (expression[i + 1] == '?' || expression[i + 1] == '*'))
                        {
                            sb.Append('\"'); // DOS_DOT
                        }
                        else
                        {
                            sb.Append('.');
                        }
                        break;
                    case '?':
                        modified = true;
                        sb.Append('>'); // DOS_QM
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return modified ? sb.ToString() : expression;
        }

        /// <summary>
        /// Return true if the given expression matches the given name. Supports the following wildcards:
        /// '*', '?', '&lt;', '&gt;', '"'. The backslash character '\' escapes.
        /// </summary>
        /// <param name="expression">The expression to match with, such as "*.foo".</param>
        /// <param name="name">The name to check against the expression.</param>
        /// <param name="ignoreCase">True to ignore case (default).</param>
        /// <remarks>
        /// This is based off of System.IO.PatternMatcher used in FileSystemWatcher, which is based off
        /// of RtlIsNameInExpression, which defines the rules for matching DOS wildcards ('*', '?', '&lt;', '&gt;', '"').
        /// 
        /// Like PatternMatcher, matching will not line up with Win32 behavior unless you transform the expression
        /// using <see cref="TranslateDosExpression(string)"/>
        /// </remarks>
        public static bool MatchesDosExpression(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase = true)
        {
            return MatchPattern(expression, name, ignoreCase, useExtendedWildcards: true);
        }

        /// <summary>
        /// Return true if the given expression matches the given name. '*' and '?' are wildcards, '\' escapes.
        /// </summary>
        public static bool MatchesSimpleExpression(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase = true)
        {
            return MatchPattern(expression, name, ignoreCase, useExtendedWildcards: false);
        }

        private static bool MatchPattern(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase, bool useExtendedWildcards)
        {
            // The idea behind the algorithm is pretty simple. We keep track of all possible locations
            // in the regular expression that are matching the name. When the name has been exhausted,
            // if one of the locations in the expression is also just exhausted, the name is in the
            // language defined by the regular expression.

            if (expression.Length == 0 || name.Length == 0)
                return false;

            if (expression[0] == '*')
            {
                // Just * matches everything
                if (expression.Length == 1)
                    return true;

                ReadOnlySpan<char> expressionEnd = expression.Slice(1);
                if (expressionEnd.IndexOfAny(useExtendedWildcards ? s_wildcardChars : s_simpleWildcardChars) == -1)
                {
                    // Handle the special case of a single starting *, which essentially means "ends with"

                    // If the name doesn't have enough characters to match the remaining expression, it can't be a match.
                    if (name.Length < expressionEnd.Length)
                        return false;

                    // See if we end with the expression
                    return name.EndsWithOrdinal(expressionEnd, ignoreCase);
                }
            }

            int nameOffset = 0;
            int expressionOffset;

            int priorMatch;
            int currentMatch;
            int priorMatchCount;
            int matchCount = 1;

            char nameChar = '\0';
            char expressionChar;

            Span<int> temp = stackalloc int[0];
            Span<int> currentMatches = stackalloc int[16];
            Span<int> priorMatches = stackalloc int[16];
            priorMatches.Clear();

            int maxState = expression.Length * 2;
            int currentState;
            bool nameFinished = false;

            //  Walk through the name string, picking off characters.  We go one
            //  character beyond the end because some wild cards are able to match
            //  zero characters beyond the end of the string.
            //
            //  With each new name character we determine a new set of states that
            //  match the name so far.  We use two arrays that we swap back and forth
            //  for this purpose.  One array lists the possible expression states for
            //  all name characters up to but not including the current one, and other
            //  array is used to build up the list of states considering the current
            //  name character as well.  The arrays are then switched and the process
            //  repeated.
            //
            //  There is not a one-to-one correspondence between state number and
            //  offset into the expression. State numbering is not continuous.
            //  This allows a simple conversion between state number and expression
            //  offset.  Each character in the expression can represent one or two
            //  states.  * and DOS_STAR generate two states: expressionOffset * 2 and
            //  expressionOffset * 2 + 1.  All other expression characters can produce
            //  only a single state.  Thus expressionOffset = currentState / 2.

            while (!nameFinished)
            {
                if (nameOffset < name.Length)
                {
                    // Not at the end of the name. Grab the current character and move the offset forward.
                    nameChar = name[nameOffset++];
                }
                else
                {
                    // At the end of the name. If the expression is exhausted, exit.
                    if (priorMatches[matchCount - 1] == maxState)
                        break;

                    nameFinished = true;
                }

                // Now, for each of the previous stored expression matches, see what
                // we can do with this name character.
                priorMatch = 0;
                currentMatch = 0;
                priorMatchCount = 0;

                while (priorMatch < matchCount)
                {
                    // We have to carry on our expression analysis as far as possible for each
                    // character of name, so we loop here until the expression stops matching.

                    expressionOffset = (priorMatches[priorMatch++] + 1) / 2;

                    while (expressionOffset < expression.Length)
                    {
                        currentState = expressionOffset * 2;
                        expressionChar = expression[expressionOffset];

                        // We may be about to exhaust the local space for matches,
                        // so we have to reallocate if this is the case.
                        if (currentMatch >= currentMatches.Length - 2)
                        {
                            int newSize = currentMatches.Length * 2;
                            temp = new int[newSize];
                            currentMatches.CopyTo(temp);
                            currentMatches = temp;

                            temp = new int[newSize];
                            priorMatches.CopyTo(temp);
                            priorMatches = temp;
                        }

                        if (expressionChar == '*')
                        {
                            // '*' matches any character zero or more times.
                            goto MatchZeroOrMore;
                        }
                        else if (useExtendedWildcards && expressionChar == '<')
                        {
                            // '<' (DOS_STAR) matches any character except '.' zero or more times.

                            // If we are at a period, determine if we are allowed to
                            // consume it, i.e. make sure it is not the last one.

                            bool notLastPeriod = false;
                            if (!nameFinished && nameChar == '.')
                            {
                                for (int offset = nameOffset; offset < name.Length; offset++)
                                {
                                    if (name[offset] == '.')
                                    {
                                        notLastPeriod = true;
                                        break;
                                    }
                                }
                            }

                            if (nameFinished || nameChar != '.' || notLastPeriod)
                            {
                                goto MatchZeroOrMore;
                            }
                            else
                            {
                                // We are at a period.  We can only match zero
                                // characters (i.e. the epsilon transition).
                                goto MatchZero;
                            }
                        }
                        else
                        {
                            // The remaining expression characters all match by consuming a character,
                            // so we need to force the expression and state forward.
                            currentState += 2;

                            if (useExtendedWildcards && expressionChar == '>')
                            {
                                // '>' (DOS_QM) is the most complicated. If the name is finished,
                                // we can match zero characters. If this name is a '.', we
                                // don't match, but look at the next expression.  Otherwise
                                // we match a single character.
                                if (nameFinished || nameChar == '.')
                                    goto NextExpressionCharacter;

                                currentMatches[currentMatch++] = currentState;
                                goto ExpressionFinished;
                            }
                            else if (useExtendedWildcards && expressionChar == '"')
                            {
                                // A '"' (DOS_DOT) can match either a period, or zero characters
                                // beyond the end of name.
                                if (nameFinished)
                                {
                                    goto NextExpressionCharacter;
                                }
                                else if (nameChar == '.')
                                {
                                    currentMatches[currentMatch++] = currentState;
                                }
                                goto ExpressionFinished;
                            }
                            else
                            {
                                if (expressionChar == '\\')
                                {
                                    // Escape character, try to move the expression forward again and match literally.
                                    if (++expressionOffset == expression.Length)
                                    {
                                        currentMatches[currentMatch++] = maxState;
                                        goto ExpressionFinished;
                                    }

                                    currentState = expressionOffset * 2 + 2;
                                    expressionChar = expression[expressionOffset];
                                }

                                // From this point on a name character is required to even
                                // continue, let alone make a match.
                                if (nameFinished) goto ExpressionFinished;

                                if (expressionChar == '?')
                                {
                                    // If this expression was a '?' we can match it once.
                                    currentMatches[currentMatch++] = currentState;
                                }
                                else if (ignoreCase
                                    ? char.ToUpperInvariant(expressionChar) == char.ToUpperInvariant(nameChar)
                                    : expressionChar == nameChar)
                                {
                                    // Matched a non-wildcard character
                                    currentMatches[currentMatch++] = currentState;
                                }

                                goto ExpressionFinished;
                            }
                        }

                        MatchZeroOrMore:
                            currentMatches[currentMatch++] = currentState;
                        MatchZero:
                            currentMatches[currentMatch++] = currentState + 1;
                        NextExpressionCharacter:
                            if (++expressionOffset == expression.Length)
                                currentMatches[currentMatch++] = maxState;
                    } // while (expressionOffset < expression.Length)

                    ExpressionFinished:

                    // Prevent duplication in the destination array.
                    //
                    // Each of the arrays is monotonically increasing and non-duplicating, thus we skip
                    // over any source element in the source array if we just added the same element to
                    // the destination array. This guarantees non-duplication in the destination array.

                    if ((priorMatch < matchCount) && (priorMatchCount < currentMatch))
                    {
                        while (priorMatchCount < currentMatch)
                        {
                            int previousLength = priorMatches.Length;
                            while ((priorMatch < previousLength) && (priorMatches[priorMatch] < currentMatches[priorMatchCount]))
                            {
                                priorMatch++;
                            }
                            priorMatchCount++;
                        }
                    }
                } // while (sourceCount < matchesCount)

                // If we found no matches in the just finished iteration it's time to bail.
                if (currentMatch == 0)
                    return false;

                // Swap the meaning the two arrays
                temp = priorMatches;
                priorMatches = currentMatches;
                currentMatches = temp;

                matchCount = currentMatch;
            } // while (!nameFinished)

            currentState = priorMatches[matchCount - 1];

            return currentState == maxState;
        }
    }
}
