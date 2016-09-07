// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static class PatternMatcher
    {
        /// <devdoc>
        ///     Private constants (directly from C header files)
        /// </devdoc>
        private const int MATCHES_ARRAY_SIZE = 16;
        private const char ANSI_DOS_STAR = '>';
        private const char ANSI_DOS_QM = '<';
        private const char DOS_DOT = '"';

        /// <devdoc>
        ///     Tells whether a given name matches the expression given with a strict (i.e. UNIX like)
        ///     semantics.  This code is a port of unmanaged code.  Original code comment follows:
        ///
        ///    Routine Description:
        ///
        ///        This routine compares a Dbcs name and an expression and tells the caller
        ///        if the name is in the language defined by the expression.  The input name
        ///        cannot contain wildcards, while the expression may contain wildcards.
        ///
        ///        Expression wild cards are evaluated as shown in the nondeterministic
        ///        finite automatons below.  Note that ~* and ~? are DOS_STAR and DOS_QM.
        ///
        ///
        ///                 ~* is DOS_STAR, ~? is DOS_QM, and ~. is DOS_DOT
        ///
        ///
        ///                                           S
        ///                                        &lt;-----&lt;
        ///                                     X  |     |  e       Y
        ///                 X * Y ==       (0)-----&gt;-(1)-&gt;-----(2)-----(3)
        ///
        ///
        ///                                          S-.
        ///                                        &lt;-----&lt;
        ///                                     X  |     |  e       Y
        ///                 X ~* Y ==      (0)-----&gt;-(1)-&gt;-----(2)-----(3)
        ///
        ///
        ///
        ///                                    X     S     S     Y
        ///                 X ?? Y ==      (0)---(1)---(2)---(3)---(4)
        ///
        ///
        ///
        ///                                    X     .        .      Y
        ///                 X ~.~. Y ==    (0)---(1)----(2)------(3)---(4)
        ///                                       |      |________|
        ///                                       |           ^   |
        ///                                       |_______________|
        ///                                          ^EOF or .^
        ///
        ///
        ///                                    X     S-.     S-.     Y
        ///                 X ~?~? Y ==    (0)---(1)-----(2)-----(3)---(4)
        ///                                       |      |________|
        ///                                       |           ^   |
        ///                                       |_______________|
        ///                                          ^EOF or .^
        ///
        ///
        ///
        ///             where S is any single character
        ///
        ///                   S-. is any single character except the final .
        ///
        ///                   e is a null character transition
        ///
        ///                   EOF is the end of the name string
        ///
        ///        In words:
        ///
        ///            * matches 0 or more characters.
        ///
        ///            ? matches exactly 1 character.
        ///
        ///            DOS_STAR matches 0 or more characters until encountering and matching
        ///                the final . in the name.
        ///
        ///            DOS_QM matches any single character, or upon encountering a period or
        ///                end of name string, advances the expression to the end of the
        ///                set of contiguous DOS_QMs.
        ///
        ///            DOS_DOT matches either a . or zero characters beyond name string.
        ///
        ///    Arguments:
        ///
        ///        Expression - Supplies the input expression to check against
        ///
        ///        Name - Supplies the input name to check for.
        ///
        ///    Return Value:
        ///
        ///        BOOLEAN - TRUE if Name is an element in the set of strings denoted
        ///            by the input Expression and FALSE otherwise.
        ///
        /// </devdoc>
        public static bool StrictMatchPattern(string expression, string name)
        {
            //
            //  The idea behind the algorithm is pretty simple.  We keep track of
            //  all possible locations in the regular expression that are matching
            //  the name.  If when the name has been exhausted one of the locations
            //  in the expression is also just exhausted, the name is in the language
            //  defined by the regular expression.
            //

            if (name == null || name.Length == 0 || expression == null || expression.Length == 0)
            {
                return false;
            }

            //
            //  Special case by far the most common wild card search of * or *.*
            //

            if (expression.Equals("*") || expression.Equals("*.*"))
            {
                return true;
            }

            // If this class is ever exposed for generic use,
            // we need to make sure that name doesn't contain wildcards. Currently
            // the only component that calls this method is FileSystemWatcher and
            // it will never pass a name that contains a wildcard.


            //
            //  Also special case expressions of the form *X.  With this and the prior
            //  case we have covered virtually all normal queries.
            //
            if (expression[0] == '*' && expression.IndexOf('*', 1) == -1)
            {
                int rightLength = expression.Length - 1;
                // if name is shorter that the stuff to the right of * in expression, we don't
                // need to do the string compare, otherwise we compare rightlength characters
                // and the end of both strings.
                if (name.Length >= rightLength && 
                    string.Compare(expression, 1, name, name.Length - rightLength, rightLength, PathInternal.StringComparison) == 0)
                {
                    return true;
                }
            }

            //
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
            //  offset into the expression.  This is evident from the NFAs in the
            //  initial comment to this function.  State numbering is not continuous.
            //  This allows a simple conversion between state number and expression
            //  offset.  Each character in the expression can represent one or two
            //  states.  * and DOS_STAR generate two states: ExprOffset*2 and
            //  ExprOffset*2 + 1.  All other expression characters can produce only
            //  a single state.  Thus ExprOffset = State/2.
            //
            //
            //  Here is a short description of the variables involved:
            //
            //  NameOffset  - The offset of the current name char being processed.
            //
            //  ExprOffset  - The offset of the current expression char being processed.
            //
            //  SrcCount    - Prior match being investigated with current name char
            //
            //  DestCount   - Next location to put a matching assuming current name char
            //
            //  NameFinished - Allows one more iteration through the Matches array
            //                 after the name is exhausted (to come *s for example)
            //
            //  PreviousDestCount - This is used to prevent entry duplication, see comment
            //
            //  PreviousMatches   - Holds the previous set of matches (the Src array)
            //
            //  CurrentMatches    - Holds the current set of matches (the Dest array)
            //
            //  AuxBuffer, LocalBuffer - the storage for the Matches arrays
            //

            //
            //  Set up the initial variables
            //
            int nameOffset;
            int exprOffset;
            int length;

            int srcCount;
            int destCount;
            int previousDestCount;
            int matchesCount;

            char nameChar = '\0';
            char exprChar = '\0';

            int[] previousMatches = new int[MATCHES_ARRAY_SIZE];
            int[] currentMatches = new int[MATCHES_ARRAY_SIZE];

            int maxState;
            int currentState;

            bool nameFinished = false;

            previousMatches[0] = 0;
            matchesCount = 1;

            nameOffset = 0;
            maxState = expression.Length * 2;

            while (!nameFinished)
            {
                if (nameOffset < name.Length)
                {
                    nameChar = name[nameOffset];
                    nameOffset++;
                }
                else
                {
                    nameFinished = true;

                    //
                    //  if we have already exhausted the expression, C#.  Don't
                    //  continue.
                    //
                    if (previousMatches[matchesCount - 1] == maxState)
                    {
                        break;
                    }
                }

                //
                //  Now, for each of the previous stored expression matches, see what
                //  we can do with this name character.
                //
                srcCount = 0;
                destCount = 0;
                previousDestCount = 0;

                while (srcCount < matchesCount)
                {
                    //
                    //  We have to carry on our expression analysis as far as possible
                    //  for each character of name, so we loop here until the
                    //  expression stops matching.  A clue here is that expression
                    //  cases that can match zero or more characters end with a
                    //  continue, while those that can accept only a single character
                    //  end with a break.
                    //
                    exprOffset = ((previousMatches[srcCount++] + 1) / 2);
                    length = 0;

                    while (true)
                    {
                        if (exprOffset == expression.Length)
                        {
                            break;
                        }

                        //
                        //  The first time through the loop we don't want
                        //  to increment ExprOffset.
                        //

                        exprOffset += length;

                        currentState = exprOffset * 2;

                        if (exprOffset == expression.Length)
                        {
                            currentMatches[destCount++] = maxState;
                            break;
                        }

                        exprChar = expression[exprOffset];
                        length = 1;

                        //
                        //  We may be about to exhaust the local
                        //  space for ExpressionMatches[][], so we have to allocate
                        //  some pool if this is the case.
                        //

                        if (destCount >= MATCHES_ARRAY_SIZE - 2)
                        {
                            int newSize = currentMatches.Length * 2;
                            int[] tmp = new int[newSize];
                            Array.Copy(currentMatches, 0, tmp, 0, currentMatches.Length);
                            currentMatches = tmp;

                            tmp = new int[newSize];
                            Array.Copy(previousMatches, 0, tmp, 0, previousMatches.Length);
                            previousMatches = tmp;
                        }

                        //
                        //  * matches any character zero or more times.
                        //

                        if (exprChar == '*')
                        {
                            currentMatches[destCount++] = currentState;
                            currentMatches[destCount++] = (currentState + 1);
                            continue;
                        }

                        //
                        //  DOS_STAR matches any character except . zero or more times.
                        //

                        if (exprChar == ANSI_DOS_STAR)
                        {
                            bool iCanEatADot = false;

                            //
                            //  If we are at a period, determine if we are allowed to
                            //  consume it, i.e. make sure it is not the last one.
                            //
                            if (!nameFinished && (nameChar == '.'))
                            {
                                char tmpChar;
                                int offset;

                                int nameLength = name.Length;
                                for (offset = nameOffset; offset < nameLength; offset++)
                                {
                                    tmpChar = name[offset];
                                    length = 1;

                                    if (tmpChar == '.')
                                    {
                                        iCanEatADot = true;
                                        break;
                                    }
                                }
                            }

                            if (nameFinished || (nameChar != '.') || iCanEatADot)
                            {
                                currentMatches[destCount++] = currentState;
                                currentMatches[destCount++] = (currentState + 1);
                                continue;
                            }
                            else
                            {
                                //
                                //  We are at a period.  We can only match zero
                                //  characters (i.e. the epsilon transition).
                                //
                                currentMatches[destCount++] = (currentState + 1);
                                continue;
                            }
                        }

                        //
                        //  The following expression characters all match by consuming
                        //  a character, thus force the expression, and thus state
                        //  forward.
                        //
                        currentState += length * 2;

                        //
                        //  DOS_QM is the most complicated.  If the name is finished,
                        //  we can match zero characters.  If this name is a '.', we
                        //  don't match, but look at the next expression.  Otherwise
                        //  we match a single character.
                        //
                        if (exprChar == ANSI_DOS_QM)
                        {
                            if (nameFinished || (nameChar == '.'))
                            {
                                continue;
                            }

                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  A DOS_DOT can match either a period, or zero characters
                        //  beyond the end of name.
                        //
                        if (exprChar == DOS_DOT)
                        {
                            if (nameFinished)
                            {
                                continue;
                            }

                            if (nameChar == '.')
                            {
                                currentMatches[destCount++] = currentState;
                                break;
                            }
                        }

                        //
                        //  From this point on a name character is required to even
                        //  continue, let alone make a match.
                        //
                        if (nameFinished)
                        {
                            break;
                        }

                        //
                        //  If this expression was a '?' we can match it once.
                        //
                        if (exprChar == '?')
                        {
                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  Finally, check if the expression char matches the name char
                        //
                        
                        if (PathInternal.IsCaseSensitive ? 
                            (exprChar == nameChar) : 
                            (char.ToUpperInvariant(exprChar) == char.ToUpperInvariant(nameChar)))
                        {
                            currentMatches[destCount++] = currentState;
                            break;
                        }

                        //
                        //  The expression didn't match so go look at the next
                        //  previous match.
                        //

                        break;
                    }


                    //
                    //  Prevent duplication in the destination array.
                    //
                    //  Each of the arrays is monotonically increasing and non-
                    //  duplicating, thus we skip over any source element in the src
                    //  array if we just added the same element to the destination
                    //  array.  This guarantees non-duplication in the dest. array.
                    //

                    if ((srcCount < matchesCount) && (previousDestCount < destCount))
                    {
                        while (previousDestCount < destCount)
                        {
                            int previousLength = previousMatches.Length;
                            while ((srcCount < previousLength) && (previousMatches[srcCount] < currentMatches[previousDestCount]))
                            {
                                srcCount += 1;
                            }
                            previousDestCount += 1;
                        }
                    }
                }

                //
                //  If we found no matches in the just finished iteration, it's time
                //  to bail.
                //

                if (destCount == 0)
                {
                    return false;
                }

                //
                //  Swap the meaning the two arrays
                //

                {
                    int[] tmp;

                    tmp = previousMatches;

                    previousMatches = currentMatches;

                    currentMatches = tmp;
                }

                matchesCount = destCount;
            }

            currentState = previousMatches[matchesCount - 1];

            return currentState == maxState;
        }
    }
}
