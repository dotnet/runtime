// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    [Serializable]
    public class CompositionExceptionTests
    {
        [Fact]
        public void Constructor1_ShouldSetMessagePropertyToDefault()
        {
            var exception = new CompositionException();

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor2_NullAsMessageArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new CompositionException((string)null);

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor3_EmptyEnumerableAsErrorsArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new CompositionException(Enumerable.Empty<CompositionError>());

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor4_NullAsMessageArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new CompositionException((string)null, new Exception());

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor5_NullAsMessageArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new CompositionException((string)null, new Exception(), Enumerable.Empty<CompositionError>());

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor2_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var exception = new CompositionException(e);

                Assert.Equal(e, exception.Message);
            }
        }

        [Fact]
        public void Constructor4_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var exception = new CompositionException(e, new Exception());

                Assert.Equal(e, exception.Message);
            }
        }

        [Fact]
        public void Constructor5_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var exception = new CompositionException(e, new Exception(), Enumerable.Empty<CompositionError>());

                Assert.Equal(e, exception.Message);
            }
        }

        [Fact]
        public void Constructor1_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new CompositionException();

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor2_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new CompositionException("Message");

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor3_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new CompositionException(Enumerable.Empty<CompositionError>());

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor4_NullAsInnerExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new CompositionException("Message", (Exception)null);

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor5_NullAsInnerExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new CompositionException("Message", (Exception)null, Enumerable.Empty<CompositionError>());

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor4_ValueAsInnerExceptionArgument_ShouldSetInnerExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            foreach (var e in expectations)
            {
                var exception = new CompositionException("Message", e);

                Assert.Same(e, exception.InnerException);
            }
        }

        [Fact]
        public void Constructor5_ValueAsInnerExceptionArgument_ShouldSetInnerExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            foreach (var e in expectations)
            {
                var exception = new CompositionException("Message", e, Enumerable.Empty<CompositionError>());

                Assert.Same(e, exception.InnerException);
            }
        }

        [Fact]
        public void Constructor2_ArrayWithNullAsErrorsArgument_ShouldThrowArgument()
        {
            var errors = new CompositionError[] { null };

            Assert.Throws<ArgumentException>("errors", () =>
            {
                new CompositionException(errors);
            });
        }

        [Fact]
        public void Constructor5_ArrayWithNullAsErrorsArgument_ShouldThrowArgument()
        {
            var errors = new CompositionError[] { null };

            Assert.Throws<ArgumentException>("errors", () =>
            {
                new CompositionException("Message", new Exception(), errors);
            });
        }

        [Fact]
        public void Constructor1_ShouldSetErrorsPropertyToEmpty()
        {
            var exception = new CompositionException();

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor2_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var exception = new CompositionException((IEnumerable<CompositionError>)null);

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor2_EmptyEnumerableAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var exception = new CompositionException(Enumerable.Empty<CompositionError>());

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor2_ValueAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var exception = new CompositionException(e);

                EqualityExtensions.CheckEquals(e, exception.Errors);
            }
        }

        [Fact]
        public void Constructor2_ArrayAsAsErrorsArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var error = CreateCompositionError();
            var errors = new CompositionError[] { error };

            var exception = new CompositionException(errors);

            errors[0] = null;

            EnumerableAssert.AreEqual(exception.Errors, error);
        }

        [Fact]
        public void Constructor3_ShouldSetErrorsPropertyToEmpty()
        {
            var exception = new CompositionException();

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor4_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var exception = new CompositionException("Message", new Exception());

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor5_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var exception = new CompositionException("Message", new Exception(), (IEnumerable<CompositionError>)null);

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor5_EmptyEnumerableAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var exception = new CompositionException("Message", new Exception(), Enumerable.Empty<CompositionError>());

            Assert.Empty(exception.Errors);
        }

        [Fact]
        public void Constructor5_ValueAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var exception = new CompositionException("Message", new Exception(), e);

                EqualityExtensions.CheckEquals(e, exception.Errors);
            }
        }

        [Fact]
        public void Constructor5_ArrayAsAsErrorsArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var error = CreateCompositionError();
            var errors = new CompositionError[] { error };

            var exception = new CompositionException("Message", new Exception(), errors);

            errors[0] = null;

            EnumerableAssert.AreEqual(exception.Errors, error);
        }

        [Fact]
        public void Message_ShouldIncludeElementGraph()
        {
            var expectations = new ExpectationCollection<CompositionError, string>();
            CompositionError error = null;

            error = CreateCompositionErrorWithElementChain(1);
            expectations.Add(error, GetElementGraphString(error));

            error = CreateCompositionErrorWithElementChain(2);
            expectations.Add(error, GetElementGraphString(error));

            error = CreateCompositionErrorWithElementChain(3);
            expectations.Add(error, GetElementGraphString(error));

            error = CreateCompositionErrorWithElementChain(10);
            expectations.Add(error, GetElementGraphString(error));

            foreach (var e in expectations)
            {
                var exception = CreateCompositionException(new CompositionError[] { e.Input });

                string result = exception.ToString();
                string expected = FixMessage(e.Output);
                AssertExtensions.Contains(result, expected);
            }
        }

        [Fact]
        public void Message_ShouldIncludeErrors()
        {
            var expectations = new ExpectationCollection<IEnumerable<CompositionError>, string>();
            expectations.Add(ErrorFactory.CreateFromDsl("Error"), "1<Separator> Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error|Error"), "1<Separator> Error|2<Separator> Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error|Error|Error"), "1<Separator> Error|2<Separator> Error|3<Separator> Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error)"), "1<Separator> Error|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error|Error)"), "1<Separator> Error|<Prefix>Error|2<Separator> Error|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error|Error|Error)"), "1<Separator> Error|<Prefix>Error|2<Separator> Error|<Prefix>Error|3<Separator> Error|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error(Exception))"), "1<Separator> Exception|<Prefix>Error|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error|Exception)"), "1<Separator> Error|<Prefix>Error|2<Separator> Exception|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Exception)"), "1<Separator> Exception|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Exception(Exception))"), "1<Separator> Exception|<Prefix>Exception|<Prefix>Error");
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error(Exception)|Error)"), "1<Separator> Exception|<Prefix>Error|<Prefix>Error|2<Separator> Error|<Prefix>Error");

            foreach (var e in expectations)
            {
                var exception = CreateCompositionException(e.Input);

                AssertMessage(exception, e.Output.Split('|'));
            }
        }

        [Fact]
        public void Messsage_ShouldIncludeCountOfRootCauses()
        {
            var expectations = new ExpectationCollection<IEnumerable<CompositionError>, int>();
            expectations.Add(ErrorFactory.CreateFromDsl("Error"), 1);
            expectations.Add(ErrorFactory.CreateFromDsl("Error|Error"), 2);
            expectations.Add(ErrorFactory.CreateFromDsl("Error|Error|Error"), 3);
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error)"), 1);
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error)|Error(Error)"), 2);
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error|Error)"), 2);
            expectations.Add(ErrorFactory.CreateFromDsl("Error(Error|Error|Exception)"), 3);

            foreach (var e in expectations)
            {
                var exception = CreateCompositionException(e.Input);

                AssertMessage(exception, e.Output, CultureInfo.CurrentCulture);
            }
        }

        [Fact]
        public void Message_ShouldFormatCountOfRootCausesUsingTheCurrentCulture()
        {
            var cultures = Expectations.GetCulturesForFormatting();

            foreach (var culture in cultures)
            {
                using (new CurrentCultureContext(culture))
                {
                    var errors = CreateCompositionErrors(1000);
                    var exception = CreateCompositionException(errors);
                    AssertMessage(exception, 1000, culture);

                    errors = CreateCompositionErrors(1);
                    exception = CreateCompositionException(errors);
                    AssertMessage(exception, 1, culture);
                }
            }
        }

        private string GetElementGraphString(CompositionError error)
        {
            StringBuilder writer = new StringBuilder();
            ICompositionElement element = error.Element;
            writer.AppendFormat(CultureInfo.CurrentCulture, SR.CompositionException_ElementPrefix, element.DisplayName);

            while ((element = element.Origin) != null)
            {
                writer.AppendFormat(CultureInfo.CurrentCulture, SR.CompositionException_OriginFormat, SR.CompositionException_OriginSeparator, element.DisplayName);
            }

            return writer.ToString();
        }

        private void AssertMessage(CompositionException exception, int rootCauseCount, CultureInfo culture)
        {
            using (StringReader reader = new StringReader(exception.Message))
            {
                string line = reader.ReadLine();

                if (rootCauseCount == 1)
                {
                    Assert.True(line.Contains(SR.CompositionException_SingleErrorWithSinglePath));
                }
                else
                {
                    Assert.True(
                        line.Contains(string.Format(CultureInfo.CurrentCulture, SR.CompositionException_SingleErrorWithMultiplePaths, rootCauseCount)) ||
                        line.Contains(string.Format(CultureInfo.CurrentCulture, SR.CompositionException_MultipleErrorsWithMultiplePaths, rootCauseCount))
                        );
                }
            }
        }

        private void AssertMessage(CompositionException exception, string[] expected)
        {
            using (StringReader reader = new StringReader(exception.Message))
            {
                // Skip header
                reader.ReadLine();

                foreach (string expect in expected)
                {
                    // Skip blank line
                    reader.ReadLine();
                    Assert.Equal(FixMessage(expect), reader.ReadLine());
                }
            }
        }

        private string FixMessage(string expect)
        {
            string fixedPrefix = expect.Replace("<Prefix>", SR.CompositionException_ErrorPrefix + " ");
            string fixedSeparator = fixedPrefix.Replace("<Separator>", SR.CompositionException_PathsCountSeparator);
            return fixedSeparator.Replace("<OriginSeparator>", SR.CompositionException_OriginSeparator);
        }

        private static CompositionError CreateCompositionError()
        {
            return CreateCompositionError("Description");
        }

        private static CompositionError CreateCompositionError(string message)
        {
            return new CompositionError(message);
        }

        private static CompositionError CreateCompositionErrorWithElementChain(int count)
        {
            return new CompositionError("Description", ElementFactory.CreateChain(count));
        }

        private static CompositionError[] CreateCompositionErrors(int count)
        {
            CompositionError[] errors = new CompositionError[count];

            for (int i = 0; i < count; i++)
            {
                errors[i] = CreateCompositionError("Description" + (i + 1));
            }

            return errors;
        }

        private static CompositionException CreateCompositionException()
        {
            return CreateCompositionException((string)null, (Exception)null, (IEnumerable<CompositionError>)null);
        }

        private static CompositionException CreateCompositionException(string message)
        {
            return CreateCompositionException(message, (Exception)null, (IEnumerable<CompositionError>)null);
        }

        private static CompositionException CreateCompositionException(IEnumerable<CompositionError> errors)
        {
            return CreateCompositionException((string)null, (Exception)null, errors);
        }

        private static CompositionException CreateCompositionException(Exception innerException)
        {
            return CreateCompositionException((string)null, innerException, (IEnumerable<CompositionError>)null);
        }

        private static CompositionException CreateCompositionException(string message, Exception innerException, IEnumerable<CompositionError> errors)
        {
            return new CompositionException(message, innerException, errors);
        }
    }
}
