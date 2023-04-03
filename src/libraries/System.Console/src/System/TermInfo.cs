// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System
{
    /// <summary>Provides access to and processing of a terminfo database.</summary>
    internal static partial class TermInfo
    {
        /// <summary>Provides support for evaluating parameterized terminfo database format strings.</summary>
        internal static class ParameterizedStrings
        {
            /// <summary>A cached stack to use to avoid allocating a new stack object for every evaluation.</summary>
            [ThreadStatic]
            private static Stack<FormatParam>? t_cachedStack;

            /// <summary>A cached array of arguments to use to avoid allocating a new array object for every evaluation.</summary>
            [ThreadStatic]
            private static FormatParam[]? t_cachedOneElementArgsArray;

            /// <summary>A cached array of arguments to use to avoid allocating a new array object for every evaluation.</summary>
            [ThreadStatic]
            private static FormatParam[]? t_cachedTwoElementArgsArray;

            /// <summary>Evaluates a terminfo formatting string, using the supplied argument.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="arg">The argument to the format string.</param>
            /// <returns>The formatted string.</returns>
            public static string Evaluate(string format, FormatParam arg)
            {
                FormatParam[] args = t_cachedOneElementArgsArray ??= new FormatParam[1];

                args[0] = arg;

                return Evaluate(format, args);
            }

            /// <summary>Evaluates a terminfo formatting string, using the supplied arguments.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="arg1">The first argument to the format string.</param>
            /// <param name="arg2">The second argument to the format string.</param>
            /// <returns>The formatted string.</returns>
            public static string Evaluate(string format, FormatParam arg1, FormatParam arg2)
            {
                FormatParam[] args = t_cachedTwoElementArgsArray ??= new FormatParam[2];

                args[0] = arg1;
                args[1] = arg2;

                return Evaluate(format, args);
            }

            /// <summary>Evaluates a terminfo formatting string, using the supplied arguments.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="args">The arguments to the format string.</param>
            /// <returns>The formatted string.</returns>
            public static string Evaluate(string format, params FormatParam[] args)
            {
                ArgumentNullException.ThrowIfNull(format);
                ArgumentNullException.ThrowIfNull(args);

                // Initialize the stack to use for processing.
                Stack<FormatParam>? stack = t_cachedStack;
                if (stack == null)
                {
                    t_cachedStack = stack = new Stack<FormatParam>();
                }
                else
                {
                    stack.Clear();
                }

                // "dynamic" and "static" variables are much less often used (the "dynamic" and "static"
                // terminology appears to just refer to two different collections rather than to any semantic
                // meaning).  As such, we'll only initialize them if we really need them.
                FormatParam[]? dynamicVars = null, staticVars = null;

                int pos = 0;
                return EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);

                // EvaluateInternal may throw IndexOutOfRangeException and InvalidOperationException
                // if the format string is malformed or if it's inconsistent with the parameters provided.
            }

            /// <summary>Evaluates a terminfo formatting string, using the supplied arguments and processing data structures.</summary>
            /// <param name="format">The format string.</param>
            /// <param name="pos">The position in <paramref name="format"/> to start processing.</param>
            /// <param name="args">The arguments to the format string.</param>
            /// <param name="stack">The stack to use as the format string is evaluated.</param>
            /// <param name="dynamicVars">A lazily-initialized collection of variables.</param>
            /// <param name="staticVars">A lazily-initialized collection of variables.</param>
            /// <returns>
            /// The formatted string; this may be empty if the evaluation didn't yield any output.
            /// The evaluation stack will have a 1 at the top if all processing was completed at invoked level
            /// of recursion, and a 0 at the top if we're still inside of a conditional that requires more processing.
            /// </returns>
            private static string EvaluateInternal(
                string format, ref int pos, FormatParam[] args, Stack<FormatParam> stack,
                ref FormatParam[]? dynamicVars, ref FormatParam[]? staticVars)
            {
                // Create a StringBuilder to store the output of this processing.  We use the format's length as an
                // approximation of an upper-bound for how large the output will be, though with parameter processing,
                // this is just an estimate, sometimes way over, sometimes under.
                var output = new ValueStringBuilder(stackalloc char[256]);

                // Format strings support conditionals, including the equivalent of "if ... then ..." and
                // "if ... then ... else ...", as well as "if ... then ... else ... then ..."
                // and so on, where an else clause can not only be evaluated for string output but also
                // as a conditional used to determine whether to evaluate a subsequent then clause.
                // We use recursion to process these subsequent parts, and we track whether we're processing
                // at the same level of the initial if clause (or whether we're nested).
                bool sawIfConditional = false;

                // Process each character in the format string, starting from the position passed in.
                for (; pos < format.Length; pos++)
                {
                    // '%' is the escape character for a special sequence to be evaluated.
                    // Anything else just gets pushed to output.
                    if (format[pos] != '%')
                    {
                        output.Append(format[pos]);
                        continue;
                    }

                    // We have a special parameter sequence to process.  Now we need
                    // to look at what comes after the '%'.
                    ++pos;
                    switch (format[pos])
                    {
                        // Output appending operations
                        case '%': // Output the escaped '%'
                            output.Append('%');
                            break;
                        case 'c': // Pop the stack and output it as a char
                            output.AppendSpanFormattable((char)stack.Pop().Int32);
                            break;
                        case 's': // Pop the stack and output it as a string
                            output.Append(stack.Pop().String);
                            break;
                        case 'd': // Pop the stack and output it as an integer
                            output.AppendSpanFormattable(stack.Pop().Int32);
                            break;
                        case 'o':
                        case 'X':
                        case 'x':
                        case ':':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            // printf strings of the format "%[[:]flags][width[.precision]][doxXs]" are allowed
                            // (with a ':' used in front of flags to help differentiate from binary operations, as flags can
                            // include '-' and '+').  While above we've special-cased common usage (e.g. %d, %s),
                            // for more complicated expressions we delegate to printf.
                            int printfEnd = pos;
                            for (; printfEnd < format.Length; printfEnd++) // find the end of the printf format string
                            {
                                char ec = format[printfEnd];
                                if (ec == 'd' || ec == 'o' || ec == 'x' || ec == 'X' || ec == 's')
                                {
                                    break;
                                }
                            }
                            if (printfEnd >= format.Length)
                            {
                                throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                            }
                            string printfFormat = format.Substring(pos - 1, printfEnd - pos + 2); // extract the format string
                            if (printfFormat.Length > 1 && printfFormat[1] == ':')
                            {
                                printfFormat = printfFormat.Remove(1, 1);
                            }
                            output.Append(FormatPrintF(printfFormat, stack.Pop().Object)); // do the printf formatting and append its output
                            break;

                        // Stack pushing operations
                        case 'p': // Push the specified parameter (1-based) onto the stack
                            pos++;
                            Debug.Assert(char.IsAsciiDigit(format[pos]));
                            stack.Push(args[format[pos] - '1']);
                            break;
                        case 'l': // Pop a string and push its length
                            stack.Push(stack.Pop().String.Length);
                            break;
                        case '{': // Push integer literal, enclosed between braces
                            pos++;
                            int intLit = 0;
                            while (format[pos] != '}')
                            {
                                Debug.Assert(char.IsAsciiDigit(format[pos]));
                                intLit = (intLit * 10) + (format[pos] - '0');
                                pos++;
                            }
                            stack.Push(intLit);
                            break;
                        case '\'': // Push literal character, enclosed between single quotes
                            stack.Push((int)format[pos + 1]);
                            Debug.Assert(format[pos + 2] == '\'');
                            pos += 2;
                            break;

                        // Storing and retrieving "static" and "dynamic" variables
                        case 'P': // Pop a value and store it into either static or dynamic variables based on whether the a-z variable is capitalized
                            pos++;
                            int setIndex;
                            FormatParam[] targetVars = GetDynamicOrStaticVariables(format[pos], ref dynamicVars, ref staticVars, out setIndex);
                            targetVars[setIndex] = stack.Pop();
                            break;
                        case 'g': // Push a static or dynamic variable; which is based on whether the a-z variable is capitalized
                            pos++;
                            int getIndex;
                            FormatParam[] sourceVars = GetDynamicOrStaticVariables(format[pos], ref dynamicVars, ref staticVars, out getIndex);
                            stack.Push(sourceVars[getIndex]);
                            break;

                        // Binary operations
                        case '+':
                        case '-':
                        case '*':
                        case '/':
                        case 'm':
                        case '^': // arithmetic
                        case '&':
                        case '|':                                         // bitwise
                        case '=':
                        case '>':
                        case '<':                               // comparison
                        case 'A':
                        case 'O':                                         // logical
                            int second = stack.Pop().Int32; // it's a stack... the second value was pushed last
                            int first = stack.Pop().Int32;
                            char c = format[pos];
                            stack.Push(
                                c == '+' ? (first + second) :
                                c == '-' ? (first - second) :
                                c == '*' ? (first * second) :
                                c == '/' ? (first / second) :
                                c == 'm' ? (first % second) :
                                c == '^' ? (first ^ second) :
                                c == '&' ? (first & second) :
                                c == '|' ? (first | second) :
                                c == '=' ? AsInt(first == second) :
                                c == '>' ? AsInt(first > second) :
                                c == '<' ? AsInt(first < second) :
                                c == 'A' ? AsInt(AsBool(first) && AsBool(second)) :
                                c == 'O' ? AsInt(AsBool(first) || AsBool(second)) :
                                0); // not possible; we just validated above
                            break;

                        // Unary operations
                        case '!':
                        case '~':
                            int value = stack.Pop().Int32;
                            stack.Push(
                                format[pos] == '!' ? AsInt(!AsBool(value)) :
                                ~value);
                            break;

                        // Some terminfo files appear to have a fairly liberal interpretation of %i. The spec states that %i increments the first two arguments,
                        // but some uses occur when there's only a single argument. To make sure we accommodate these files, we increment the values
                        // of up to (but not requiring) two arguments.
                        case 'i':
                            if (args.Length > 0)
                            {
                                args[0] = 1 + args[0].Int32;
                                if (args.Length > 1)
                                    args[1] = 1 + args[1].Int32;
                            }
                            break;

                        // Conditional of the form %? if-part %t then-part %e else-part %;
                        // The "%e else-part" is optional.
                        case '?':
                            sawIfConditional = true;
                            break;
                        case 't':
                            // We hit the end of the if-part and are about to start the then-part.
                            // The if-part left its result on the stack; pop and evaluate.
                            bool conditionalResult = AsBool(stack.Pop().Int32);

                            // Regardless of whether it's true, run the then-part to get past it.
                            // If the conditional was true, output the then results.
                            pos++;
                            string thenResult = EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);
                            if (conditionalResult)
                            {
                                output.Append(thenResult);
                            }
                            Debug.Assert(format[pos] == 'e' || format[pos] == ';');

                            // We're past the then; the top of the stack should now be a Boolean
                            // indicating whether this conditional has more to be processed (an else clause).
                            if (!AsBool(stack.Pop().Int32))
                            {
                                // Process the else clause, and if the conditional was false, output the else results.
                                pos++;
                                string elseResult = EvaluateInternal(format, ref pos, args, stack, ref dynamicVars, ref staticVars);
                                if (!conditionalResult)
                                {
                                    output.Append(elseResult);
                                }

                                // Now we should be done (any subsequent elseif logic will have been handled in the recursive call).
                                if (!AsBool(stack.Pop().Int32))
                                {
                                    throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                                }
                            }

                            // If we're in a nested processing, return to our parent.
                            if (!sawIfConditional)
                            {
                                stack.Push(1);
                                return output.ToString();
                            }

                            // Otherwise, we're done processing the conditional in its entirety.
                            sawIfConditional = false;
                            break;
                        case 'e':
                        case ';':
                            // Let our caller know why we're exiting, whether due to the end of the conditional or an else branch.
                            stack.Push(AsInt(format[pos] == ';'));
                            return output.ToString();

                        // Anything else is an error
                        default:
                            throw new InvalidOperationException(SR.IO_TermInfoInvalid);
                    }
                }

                stack.Push(1);
                return output.ToString();
            }

            /// <summary>Converts an Int32 to a Boolean, with 0 meaning false and all non-zero values meaning true.</summary>
            /// <param name="i">The integer value to convert.</param>
            /// <returns>true if the integer was non-zero; otherwise, false.</returns>
            private static bool AsBool(int i) { return i != 0; }

            /// <summary>Converts a Boolean to an Int32, with true meaning 1 and false meaning 0.</summary>
            /// <param name="b">The Boolean value to convert.</param>
            /// <returns>1 if the Boolean is true; otherwise, 0.</returns>
            private static int AsInt(bool b) { return b ? 1 : 0; }

            /// <summary>Formats an argument into a printf-style format string.</summary>
            /// <param name="format">The printf-style format string.</param>
            /// <param name="arg">The argument to format.  This must be an Int32 or a String.</param>
            /// <returns>The formatted string.</returns>
            private static unsafe string FormatPrintF(string format, object arg)
            {
                Debug.Assert(arg is string || arg is int);

                // Determine how much space is needed to store the formatted string.
                string? stringArg = arg as string;
                int neededLength = stringArg != null ?
                    Interop.Sys.SNPrintF(null, 0, format, stringArg) :
                    Interop.Sys.SNPrintF(null, 0, format, (int)arg);
                if (neededLength == 0)
                {
                    return string.Empty;
                }
                if (neededLength < 0)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PrintF);
                }

                // Allocate the needed space, format into it, and return the data as a string.
                byte[] bytes = new byte[neededLength + 1]; // extra byte for the null terminator
                fixed (byte* ptr = &bytes[0])
                {
                    int length = stringArg != null ?
                        Interop.Sys.SNPrintF(ptr, bytes.Length, format, stringArg) :
                        Interop.Sys.SNPrintF(ptr, bytes.Length, format, (int)arg);
                    if (length != neededLength)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PrintF);
                    }
                }
                return Encoding.ASCII.GetString(bytes, 0, neededLength);
            }

            /// <summary>Gets the lazily-initialized dynamic or static variables collection, based on the supplied variable name.</summary>
            /// <param name="c">The name of the variable.</param>
            /// <param name="dynamicVars">The lazily-initialized dynamic variables collection.</param>
            /// <param name="staticVars">The lazily-initialized static variables collection.</param>
            /// <param name="index">The index to use to index into the variables.</param>
            /// <returns>The variables collection.</returns>
            private static FormatParam[] GetDynamicOrStaticVariables(
                char c, ref FormatParam[]? dynamicVars, ref FormatParam[]? staticVars, out int index)
            {
                if (char.IsAsciiLetterUpper(c))
                {
                    index = c - 'A';
                    return staticVars ??= new FormatParam[26]; // one slot for each letter of alphabet
                }
                else if (char.IsAsciiLetterLower(c))
                {
                    index = c - 'a';
                    return dynamicVars ??= new FormatParam[26]; // one slot for each letter of alphabet
                }
                else throw new InvalidOperationException(SR.IO_TermInfoInvalid);
            }

            /// <summary>
            /// Represents a parameter to a terminfo formatting string.
            /// It is a discriminated union of either an integer or a string,
            /// with characters represented as integers.
            /// </summary>
            public readonly struct FormatParam
            {
                /// <summary>The integer stored in the parameter.</summary>
                private readonly int _int32;
                /// <summary>The string stored in the parameter.</summary>
                private readonly string? _string; // null means an Int32 is stored

                /// <summary>Initializes the parameter with an integer value.</summary>
                /// <param name="value">The value to be stored in the parameter.</param>
                public FormatParam(int value) : this(value, null) { }

                /// <summary>Initializes the parameter with a string value.</summary>
                /// <param name="value">The value to be stored in the parameter.</param>
                public FormatParam(string? value) : this(0, value ?? string.Empty) { }

                /// <summary>Initializes the parameter.</summary>
                /// <param name="intValue">The integer value.</param>
                /// <param name="stringValue">The string value.</param>
                private FormatParam(int intValue, string? stringValue)
                {
                    _int32 = intValue;
                    _string = stringValue;
                }

                /// <summary>Implicit converts an integer into a parameter.</summary>
                public static implicit operator FormatParam(int value)
                {
                    return new FormatParam(value);
                }

                /// <summary>Implicit converts a string into a parameter.</summary>
                public static implicit operator FormatParam(string? value)
                {
                    return new FormatParam(value);
                }

                /// <summary>Gets the integer value of the parameter. If a string was stored, 0 is returned.</summary>
                public int Int32 { get { return _int32; } }

                /// <summary>Gets the string value of the parameter.  If an Int32 or a null String were stored, an empty string is returned.</summary>
                public string String { get { return _string ?? string.Empty; } }

                /// <summary>Gets the string or the integer value as an object.</summary>
                public object Object { get { return _string ?? (object)_int32; } }
            }
        }
    }
}
