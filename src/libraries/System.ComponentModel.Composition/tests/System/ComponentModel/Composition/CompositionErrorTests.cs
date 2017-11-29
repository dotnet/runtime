// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionErrorTests
    {
        [Fact]
        public void Constructor1_NullAsMessageArgument_ShouldSetMessagePropertyToEmptyString()
        {
            var error = new CompositionError((string)null);

            Assert.Equal("", error.Description);
        }

        [Fact]
        public void Constructor2_NullAsMessageArgument_ShouldSetMessagePropertyToEmptyString()
        {
            var error = new CompositionError((string)null, ElementFactory.Create());

            Assert.Equal("", error.Description);
        }

        [Fact]
        public void Constructor3_NullAsMessageArgument_ShouldSetMessagePropertyToEmptyString()
        {
            var error = new CompositionError((string)null, new Exception());

            Assert.Equal("", error.Description);
        }

        [Fact]
        public void Constructor4_NullAsMessageArgument_ShouldSetMessagePropertyToEmptyString()
        {
            var error = new CompositionError((string)null, ElementFactory.Create(), new Exception());

            Assert.Equal("", error.Description);
        }

        [Fact]
        public void Constructor5_NullAsMessageArgument_ShouldSetMessagePropertyToEmptyString()
        {
            var error = new CompositionError(CompositionErrorId.Unknown, (string)null, ElementFactory.Create(), new Exception());

            Assert.Equal("", error.Description);
        }

        [Fact]
        public void Constructor1_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(e);
                Assert.Equal(e, exception.Description);
            });
        }

        [Fact]
        public void Constructor2_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(e, ElementFactory.Create());

                Assert.Equal(e, exception.Description);
            });
        }

        [Fact]
        public void Constructor3_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(e, new Exception());
                Assert.Equal(e, exception.Description);
            });
        }

        [Fact]
        public void Constructor4_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(e, ElementFactory.Create(), new Exception());
                Assert.Equal(e, exception.Description);
            });
        }

        [Fact]
        public void Constructor5_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();
            
            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(CompositionErrorId.Unknown, e, ElementFactory.Create(), new Exception());
                Assert.Equal(e, exception.Description);
            });
        }

        [Fact]
        public void Constructor1_ShouldSetExceptionPropertyToNull()
        {
            var error = new CompositionError("Description");

            Assert.Null(error.Exception);
        }

        [Fact]
        public void Constructor2_ShouldSetExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", ElementFactory.Create());

            Assert.Null(error.Exception);
        }

        [Fact]
        public void Constructor3_NullAsExceptionArgument_ShouldSetExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", (Exception)null);

            Assert.Null(error.Exception);
        }

        [Fact]
        public void Constructor4_NullAsExceptionArgument_ShouldSetExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", ElementFactory.Create(), (Exception)null);

            Assert.Null(error.Exception);
        }

        [Fact]
        public void Constructor5_NullAsExceptionArgument_ShouldSetExceptionPropertyToNull()
        {
            var error = new CompositionError(CompositionErrorId.Unknown, "Description", ElementFactory.Create(), (Exception)null);

            Assert.Null(error.Exception);
        }

        [Fact]
        public void Constructor3_ValueAsExceptionArgument_ShouldSetExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();
            
            Assert.All(expectations, e =>
            {
                var error = new CompositionError("Description", e);
                Assert.Same(e, error.Exception);
            });
        }

        [Fact]
        public void Constructor4_ValueAsExceptionArgument_ShouldSetExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            Assert.All(expectations, e =>
            {
                var error = new CompositionError("Description", ElementFactory.Create(), e);
                Assert.Same(e, error.Exception);
            });
        }

        [Fact]
        public void Constructor5_ValueAsExceptionArgument_ShouldSetExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            Assert.All(expectations, e =>
            {
                var error = new CompositionError(CompositionErrorId.Unknown, "Description", ElementFactory.Create(), e);
                Assert.Same(e, error.Exception);
            });
        }

        [Fact]
        public void Constructor1_ShouldSetInnerExceptionPropertyToNull()
        {
            var error = new CompositionError("Description");

            Assert.Null(error.InnerException);
        }

        [Fact]
        public void Constructor2_ShouldSetInnerExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", ElementFactory.Create());

            Assert.Null(error.InnerException);
        }

        [Fact]
        public void Constructor3_NullAsExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", (Exception)null);

            Assert.Null(error.InnerException);
        }

        [Fact]
        public void Constructor4_NullAsExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var error = new CompositionError("Description", ElementFactory.Create(), (Exception)null);

            Assert.Null(error.InnerException);
        }

        [Fact]
        public void Constructor5_NullAsExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var error = new CompositionError(CompositionErrorId.Unknown, "Description", ElementFactory.Create(), (Exception)null);

            Assert.Null(error.InnerException);
        }

        [Fact]
        public void Constructor3_ValueAsExceptionArgument_ShouldSetInnerExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            Assert.All(expectations, e =>
            {
                var error = new CompositionError("Description", e);
                Assert.Same(e, error.InnerException);
            });
        }

        [Fact]
        public void Constructor4_ValueAsExceptionArgument_ShouldSetInnerExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            Assert.All(expectations, e =>
            {
                var error = new CompositionError("Description", ElementFactory.Create(), e);
                Assert.Same(e, error.InnerException);
            });
        }

        [Fact]
        public void Constructor1_ShouldSetICompositionErrorIdPropertyToCompositionErrorIdUnknown()
        {
            var error = new CompositionError("Description");

            Assert.Equal(CompositionErrorId.Unknown, error.Id);
        }

        [Fact]
        public void Constructor2_ShouldSetICompositionErrorIdPropertyToCompositionErrorIdUnknown()
        {
            var error = new CompositionError("Description", ElementFactory.Create());

            Assert.Equal(CompositionErrorId.Unknown, error.Id);
        }

        [Fact]
        public void Constructor3_ShouldSetICompositionErrorIdPropertyToCompositionErrorIdUnknown()
        {
            var error = new CompositionError("Description", new Exception());

            Assert.Equal(CompositionErrorId.Unknown, error.Id);
        }

        [Fact]
        public void Constructor4_ShouldSetICompositionErrorIdPropertyToCompositionErrorIdUnknown()
        {
            var error = new CompositionError("Description", ElementFactory.Create(), new Exception());

            Assert.Equal(CompositionErrorId.Unknown, error.Id);
        }

        [Fact]
        public void Constructor5_ValueAsIdArgument_ShouldSetICompositionErrorIdProperty()
        {
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            Assert.All(expectations, e =>
            {
                var error = new CompositionError(e, "Description", ElementFactory.Create(), new Exception());
                Assert.Equal(e, error.Id);
            });
        }

        [Fact]
        public void Constructor1_ShouldSetElementPropertyToNull()
        {
            var exception = new CompositionError("Description");

            Assert.Null(exception.Element);
        }

        [Fact]
        public void Constructor2_NullAsElementArgument_ShouldSetElementPropertyToNull()
        {
            var exception = new CompositionError("Description", (ICompositionElement)null);

            Assert.Null(exception.Element);
        }

        [Fact]
        public void Constructor3_ShouldSetElementPropertyToNull()
        {
            var exception = new CompositionError("Description", new Exception());

            Assert.Null(exception.Element);
        }

        [Fact]
        public void Constructor4_NullAsElementArgument_ShouldSetElementPropertyToNull()
        {
            var exception = new CompositionError("Description", (ICompositionElement)null, new Exception());

            Assert.Null(exception.Element);
        }

        [Fact]
        public void Constructor5_NullAsElementArgument_ShouldSetElementPropertyToNull()
        {
            var exception = new CompositionError(CompositionErrorId.Unknown, "Description", (ICompositionElement)null, new Exception());

            Assert.Null(exception.Element);
        }

        [Fact]
        public void Constructor2_ValueAsElementArgument_ShouldSetElementProperty()
        {
            var expectations = Expectations.GetCompositionElements();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError("Description", (ICompositionElement)e);
                Assert.Same(e, exception.Element);
            });
        }

        [Fact]
        public void Constructor4_ValueAsElementArgument_ShouldSetElementProperty()
        {
            var expectations = Expectations.GetCompositionElements();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError("Description", (ICompositionElement)e, new Exception());
                Assert.Same(e, exception.Element);
            });
        }

        [Fact]
        public void Constructor5_ValueAsElementArgument_ShouldSetElementProperty()
        {
            var expectations = Expectations.GetCompositionElements();

            Assert.All(expectations, e =>
            {
                var exception = new CompositionError(CompositionErrorId.Unknown, "Description", (ICompositionElement)e, new Exception());
                Assert.Same(e, exception.Element);
            });
        }

        [Fact]
        public void ToString_ShouldReturnMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            Assert.All(expectations, e =>
            {
                var error = CreateCompositionError(e);
                Assert.Equal(error.Description, error.ToString());
            });
        }
        
        private static CompositionError CreateCompositionError()
        {
            return CreateCompositionError(CompositionErrorId.Unknown, (string)null, (ICompositionElement)null, (Exception)null);
        }

        private static CompositionError CreateCompositionError(string message)
        {
            return CreateCompositionError(CompositionErrorId.Unknown, message, (ICompositionElement)null, (Exception)null);
        }

        private static CompositionError CreateCompositionError(CompositionErrorId id)
        {
            return CreateCompositionError(id, (string)null, (ICompositionElement)null, (Exception)null);
        }

        private static CompositionError CreateCompositionError(Exception exception)
        {
            return CreateCompositionError(CompositionErrorId.Unknown, (string)null, (ICompositionElement)null, exception);
        }

        private static CompositionError CreateCompositionError(ICompositionElement element)
        {
            return CreateCompositionError(CompositionErrorId.Unknown, (string)null, element, (Exception)null);
        }

        private static CompositionError CreateCompositionError(CompositionErrorId id, string message, ICompositionElement element, Exception exception)
        {
            return new CompositionError(id, message, element, exception);
        }

        private static CompositionError CreateCompositionError(params string[] messages)
        {
            CompositionError error = null;
            foreach (string message in messages.Reverse())
            {
                CompositionException exception = null;
                if (error != null)
                {
                    exception = CreateCompositionException(error);
                }

                error = ErrorFactory.Create(message, exception);
            }

            return error;
        }

        private static CompositionError CreateCompositionErrorWithException(params string[] messages)
        {
            Exception innerException = null;
            foreach (string message in messages.Skip(1).Reverse())
            {
                innerException = new Exception(message, innerException);
            }

            return new CompositionError(messages[0], innerException);
        }

        private static CompositionError CreateCompostionErrorWithCompositionException(string message1, string message2)
        {
            var exception = CreateCompositionException(new Exception(message2));

            return new CompositionError(message1, exception);
        }

        private static CompositionException CreateCompositionException(CompositionError error)
        {
            return new CompositionException(error);
        }

        private static CompositionException CreateCompositionException(Exception innerException)
        {
            return new CompositionException("Description", innerException, null);
        }
    }
}
