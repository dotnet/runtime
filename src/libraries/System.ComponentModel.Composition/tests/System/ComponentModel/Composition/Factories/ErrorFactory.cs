// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Text;

namespace System.ComponentModel.Composition.Factories
{
    internal static partial class ErrorFactory
    {
        public static CompositionError Create(ICompositionElement element)
        {
            return Create(CompositionErrorId.Unknown, (string)null, element, (Exception)null);
        }

        public static CompositionError Create(Exception exception)
        {
            return Create(CompositionErrorId.Unknown, (string)null, (ICompositionElement)null, exception);
        }

        public static CompositionError Create(string message)
        {
            return Create(CompositionErrorId.Unknown, message, (ICompositionElement)null, (Exception)null);
        }

        public static CompositionError Create(string message, Exception exception)
        {
            return Create(CompositionErrorId.Unknown, message, (ICompositionElement)null, exception);
        }

        public static CompositionError Create(CompositionErrorId errorId)
        {
            return Create(errorId, errorId.ToString(), (ICompositionElement)null, (Exception)null);
        }

        public static CompositionError Create(CompositionErrorId errorId, string message, Exception exception)
        {
            return new CompositionError(errorId, message, (ICompositionElement)null, exception);
        }

        public static CompositionError Create(CompositionErrorId errorId, string message, ICompositionElement element, Exception exception)
        {
            return new CompositionError(errorId, message, element, exception);
        }

        public static IEnumerable<CompositionError> CreateFromDsl(string format)
        {
            CompositionException exception = (CompositionException)CreateFromDslCore(format);

            return exception.Errors;
        }

        private static Exception CreateFromDslCore(string format)
        {
            List<Tuple<string, string>> identifiers = new List<Tuple<string,string>>();

            StringBuilder token = new StringBuilder();
            StringReader reader = new StringReader(format);

            while (reader.Peek() != -1)
            {
                char c = (char)reader.Read();
                if (c == '|')
                {
                    AddIdentifier(identifiers, token);
                    continue;
                }

                if (c == '(')
                {
                    string dsl = ReadToNextMatchingParenthesis(reader);

                    AddIdentifier(identifiers, token, dsl);
                    continue;
                }

                token.Append(c);
            }

            AddIdentifier(identifiers, token);

            return CreateFromList(identifiers);
        }

        private static Exception CreateFromList(List<Tuple<string, string>> identifiers)
        {
            List<CompositionError> errors = new List<CompositionError>();
            Exception exception = null;
            foreach (var identifier in identifiers)
            {
                Exception innerException = null;
                if (identifier.Item2 != null)
                {
                    innerException = CreateFromDslCore(identifier.Item2);
                }

                if (identifier.Item1 == "Exception")
                {
                    //Assert.IsNull(exception);
                    exception = new Exception(identifier.Item1, innerException);
                }
                else
                {
                    //Assert.AreEqual("Error", identifier.Item1);

                    errors.Add(Create(identifier.Item1, innerException));
                }                
            }

            if (errors.Count == 0)
            {   
                return exception;
            }

            return new CompositionException("", exception, errors);
        }

        private static void AddIdentifier(List<Tuple<string, string>> identifiers, StringBuilder identifier)
        {
            AddIdentifier(identifiers, identifier, (string)null);
        }

        private static void AddIdentifier(List<Tuple<string, string>> identifiers, StringBuilder identifier, string dsl)
        {
            if (identifier.Length == 0)
                return;

            identifiers.Add(new Tuple<string, string>(identifier.ToString(), dsl));

            identifier.Length = 0;
        }

        private static IEnumerable<CompositionError> CreateFromList(List<string> errors)
        {
            foreach (string value in errors)
            {
                yield return Create(value);
            }
        }

        private static string ReadToNextMatchingParenthesis(StringReader reader)
        {
            Stack<char> parenthesis = new Stack<char>();
            parenthesis.Push('(');

            StringBuilder builder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                char c = (char)reader.Read();
                if (c == '(')
                {
                    parenthesis.Push(c);

                    if (parenthesis.Count == 1)
                    {
                        continue;
                    }
                }

                if (c == ')')
                {
                    char pop = parenthesis.Pop();
                    //Assert.AreEqual('(', pop);

                    if (parenthesis.Count == 0)
                    {
                        break;
                    }
                }
                
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
