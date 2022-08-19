// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.UnitTesting;
using Xunit;

namespace System.UnitTesting
{
    public static class CompositionAssert
    {
        public static void ThrowsPart(Action action)
        {
            ThrowsPart(RetryMode.Retry, action);
        }

        public static void ThrowsPart(RetryMode retry, Action action)
        {
            ThrowsPart(new CompositionErrorExpectation { Id = (ErrorId)CompositionErrorId.Unknown }, retry, action);
        }

        public static void ThrowsPart(ICompositionElement element, RetryMode retry, Action action)
        {
            ThrowsPart(new CompositionErrorExpectation { Id = (ErrorId)CompositionErrorId.Unknown, Element = element }, retry, action);
        }

        public static void ThrowsPart<TInner>(Action action)
            where TInner : Exception
        {
            ThrowsPart<TInner>(RetryMode.Retry, action);
        }

        public static void ThrowsPart<TInner>(RetryMode retry, Action action)
            where TInner : Exception
        {
            ThrowsPart(new CompositionErrorExpectation { Id = (ErrorId)CompositionErrorId.Unknown, InnerExceptionType = typeof(TInner) }, retry, action);
        }

        private static void ThrowsPart(CompositionErrorExpectation expectation, RetryMode retry, Action action)
        {
            ExceptionAssert.Throws<ComposablePartException>(retry, action, (thrownException, retryCount) =>
            {
                AssertCore(retryCount, "ComposablePartException", thrownException, expectation);
            });
        }

        public static void ThrowsError(ErrorId id, Action action)
        {
            ThrowsError(new CompositionErrorExpectation { Id = id }, RetryMode.Retry, action);
        }

        public static void ThrowsError(ErrorId id, ErrorId innerId, Action action)
        {
            ThrowsError(id, innerId, RetryMode.Retry, action);
        }

        public static void ThrowsError(ErrorId id, ErrorId innerId, RetryMode retry, Action action)
        {
            ThrowsError(GetExpectation(id, innerId), retry, action);
        }

        public static void ThrowsError(ErrorId id, ErrorId innerId, ErrorId innerInnerId, RetryMode retry, Action action)
        {
            ThrowsError(GetExpectation(id, innerId, innerInnerId), retry, action);
        }

        public static void ThrowsError(ErrorId id, RetryMode retry, Action action)
        {
            ThrowsError(new CompositionErrorExpectation { Id = id, }, retry, action);
        }

        private static void ThrowsError(CompositionErrorExpectation expectation, RetryMode retry, Action action)
        {
            ThrowsErrors(new CompositionErrorExpectation[] { expectation }, retry, action);
        }

        public static void ThrowsErrors(ErrorId id1, ErrorId id2, Action action)
        {
            ThrowsErrors(id1, id2, RetryMode.Retry, action);
        }

        public static void ThrowsErrors(ErrorId id1, ErrorId id2, RetryMode retry, Action action)
        {
            ThrowsErrors(new ErrorId[] { id1, id2 }, retry, action);
        }

        public static void ThrowsErrors(ErrorId[] ids, RetryMode retry, Action action)
        {
            CompositionErrorExpectation[] expectations = new CompositionErrorExpectation[ids.Length];
            for (int i = 0; i < expectations.Length; i++)
            {
                expectations[i] = new CompositionErrorExpectation { Id = ids[i] };
            }

            ThrowsErrors(expectations, retry, action);
        }

        public static void ThrowsChangeRejectedError(ErrorId id, RetryMode retry, Action action)
        {
            ThrowsChangeRejectedError(new CompositionErrorExpectation { Id = id, }, retry, action);
        }

        public static void ThrowsChangeRejectedError(ErrorId id, ErrorId innerId, RetryMode retry, Action action)
        {
            ThrowsChangeRejectedError(GetExpectation(id, innerId), retry, action);
        }

        public static void ThrowsChangeRejectedError(ErrorId id, ErrorId innerId, ErrorId innerInnerId, RetryMode retry, Action action)
        {
            ThrowsChangeRejectedError(GetExpectation(id, innerId, innerInnerId), retry, action);
        }

        private static void ThrowsChangeRejectedError(CompositionErrorExpectation expectation, RetryMode retry, Action action)
        {
            ThrowsChangeRejectedErrors(new CompositionErrorExpectation[] { expectation }, retry, action);
        }

        private static void ThrowsChangeRejectedErrors(CompositionErrorExpectation[] expectations, RetryMode retry, Action action)
        {
            ExceptionAssert.Throws<ChangeRejectedException>(retry, action, (thrownException, retryCount) =>
            {
                AssertCore(retryCount, "CompositionException", thrownException, expectations);
            });
        }

        private static void ThrowsErrors(CompositionErrorExpectation[] expectations, RetryMode retry, Action action)
        {
            ExceptionAssert.Throws<CompositionException>(retry, action, (thrownException, retryCount) =>
            {
                AssertCore(retryCount, "CompositionException", thrownException, expectations);
            });
        }

        private static void AssertCore(int retryCount, string prefix, CompositionException exception, CompositionErrorExpectation[] expectations)
        {
            Assert.Equal(exception.Errors.Count, expectations.Length);

            for (int i = 0; i < exception.Errors.Count; i++)
            {
                AssertCore(retryCount, prefix + ".Errors[" + i + "]", exception.Errors[i], expectations[i]);
            }
        }

        private static void AssertCore(int retryCount, string prefix, ComposablePartException error, CompositionErrorExpectation expectation)
        {
            if (expectation.ElementSpecified)
            {
                AssertCore(retryCount, prefix, "Element", expectation.Element, error.Element);
            }

            if (expectation.InnerExceptionSpecified)
            {
                AssertCore(retryCount, prefix, "InnerException", expectation.InnerException, error.InnerException);
            }

            if (expectation.InnerExceptionTypeSpecified)
            {
                AssertCore(retryCount, prefix, "InnerException.GetType()", expectation.InnerExceptionType, error.InnerException == null ? null : error.InnerException.GetType());
            }

            if (expectation.InnerExpectationsSpecified)
            {
                var innerError = error.InnerException as ComposablePartException;
                if (innerError != null)
                {
                    Assert.Equal(1, expectation.InnerExpectations.Length);
                    AssertCore(retryCount, prefix + ".InnerException", innerError, expectation.InnerExpectations[0]);
                }
                else
                {
                    AssertCore(retryCount, prefix + ".InnerException", (CompositionException)error.InnerException, expectation.InnerExpectations);
                }
            }
        }

        private static void AssertCore(int retryCount, string prefix, CompositionError error, CompositionErrorExpectation expectation)
        {
            if (expectation.IdSpecified)
            {
                AssertCore(retryCount, prefix, "Id", expectation.Id, (ErrorId)error.Id);
            }

            if (expectation.ElementSpecified)
            {
                AssertCore(retryCount, prefix, "Element", expectation.Element, error.Element);
            }

            if (expectation.InnerExceptionSpecified)
            {
                AssertCore(retryCount, prefix, "InnerException", expectation.InnerException, error.InnerException);
            }

            if (expectation.InnerExceptionTypeSpecified)
            {
                AssertCore(retryCount, prefix, "InnerException.GetType()", expectation.InnerExceptionType, error.InnerException == null ? null : error.InnerException.GetType());
            }

            if (expectation.InnerExpectationsSpecified)
            {
                var innerError = error.InnerException as ComposablePartException;
                if (innerError != null)
                {
                    Assert.Equal(1, expectation.InnerExpectations.Length);
                    AssertCore(retryCount, prefix + ".InnerException", innerError, expectation.InnerExpectations[0]);
                }
                else
                {
                    AssertCore(retryCount, prefix + ".InnerException", (CompositionException)error.InnerException, expectation.InnerExpectations);
                }
            }
        }

        private static void AssertCore<T>(int retryCount, string prefix, string propertyName, T expected, T actual)
        {
            Assert.Equal(expected, actual);
        }

        private static CompositionErrorExpectation GetExpectation(params ErrorId[] ids)
        {
            var parent = new CompositionErrorExpectation() { Id = ids[0] };
            var expectation = parent;

            for (int i = 1; i < ids.Length; i++)
            {
                expectation.InnerExpectations = new CompositionErrorExpectation[] { new CompositionErrorExpectation() { Id = ids[i] } };
                expectation = expectation.InnerExpectations[0];
            }

            return parent;
        }

        private static ErrorId GetRootErrorId(CompositionException exception)
        {
            Assert.True(exception.Errors.Count == 1);

            return GetRootErrorId(exception.Errors[0]);
        }

        private static ErrorId GetRootErrorId(object error)
        {
            //
            // Get the InnerException from the error object
            // Can be one of two types currently a ComposablePartException or a CompositionError
            // Done this clunky way to avoid shipping dead code to retrieve it from ICompositionException
            //
            Exception exception = null;
            if (error is ComposablePartException)
            {
                exception = ((ComposablePartException)error).InnerException;
            }
            else if (error is CompositionException)
            {
                exception = ((CompositionException)error).InnerException;
            }
            else
            {
                throw new NotImplementedException();
            }

            ComposablePartException composablePartException = exception as ComposablePartException;
            if (composablePartException != null)
            {
                return GetRootErrorId(composablePartException);
            }

            CompositionException composition = exception as CompositionException;
            if (composition != null)
            {
                return GetRootErrorId(composition);
            }

            throw new NotImplementedException();
        }

        private class CompositionErrorExpectation
        {
            private ErrorId _id;
            private Exception _innerException;
            private Type _innerExceptionType;
            private ICompositionElement _element;
            private CompositionErrorExpectation[] _innerExpectations;

            public ErrorId Id
            {
                get { return _id; }
                set
                {
                    _id = value;
                    IdSpecified = true;
                }
            }

            public Exception InnerException
            {
                get { return _innerException; }
                set
                {
                    _innerException = value;
                    InnerExceptionSpecified = true;
                }
            }

            public Type InnerExceptionType
            {
                get { return _innerExceptionType; }
                set
                {
                    _innerExceptionType = value;
                    InnerExceptionTypeSpecified = true;
                }
            }

            public ICompositionElement Element
            {
                get { return _element; }
                set
                {
                    _element = value;
                    ElementSpecified = true;
                }
            }

            public CompositionErrorExpectation[] InnerExpectations
            {
                get { return _innerExpectations; }
                set
                {
                    _innerExpectations = value;
                    InnerExpectationsSpecified = true;
                }
            }

            public bool IdSpecified
            {
                get;
                private set;
            }

            public bool InnerExceptionSpecified
            {
                get;
                private set;
            }

            public bool InnerExceptionTypeSpecified
            {
                get;
                private set;
            }

            public bool ElementSpecified
            {
                get;
                private set;
            }

            public bool InnerExpectationsSpecified
            {
                get;
                private set;
            }
        }
    }
}
