// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionResultTest
    {
        [Fact]
        public void Constructor1_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult();

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor2_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult((CompositionError[])null);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor3_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult((IEnumerable<CompositionError>)null);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor2_EmptyAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult(new CompositionError[0]);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor3_EmptyAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult(Enumerable.Empty<CompositionError>());

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor2_ValuesAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult(e.ToArray());

                Assert.Equal(e, result.Errors);
            }
        }

        [Fact]
        public void Constructor3_ValuesAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult(e);

                Assert.Equal(e, result.Errors);
            }
        }

        [Fact]
        public void Constructor1_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult();

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor2_NullAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult((CompositionError[])null);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor3_NullAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult((IEnumerable<CompositionError>)null);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor2_EmptyAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult(new CompositionError[0]);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor3_EmptyAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult(Enumerable.Empty<CompositionError>());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor2_ValuesAsErrorsArgument_ShouldSetSucceededPropertyToTrueIfThereAreErrors()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult(e.ToArray());

                if (e.Count() > 0)
                {
                    Assert.False(result.Succeeded);
                }
                else
                {
                    Assert.True(result.Succeeded);
                }
            }
        }

        [Fact]
        public void Constructor3_ValuesAsErrorsArgument_ShouldSetSucceededPropertyToTrueIfThereAreErrors()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult(e);

                if (e.Count() > 0)
                {
                    Assert.False(result.Succeeded);
                }
                else
                {
                    Assert.True(result.Succeeded);
                }
            }
        }

        [Fact]
        [ActiveIssue(25498)]
        public void MergeResult_ResultWithNullErrorsAsResultArgument_ShouldReturnResultWithSameErrors()
        {
            var emptyResult = CreateCompositionResult((IEnumerable<CompositionError>)null);

            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult(e);

                var mergedResult = result.MergeResult(emptyResult);

                Assert.Equal(result, mergedResult);
            }
        }

        [Fact]
        [ActiveIssue(25498)]
        public void MergeResult_ResultWithEmptyErrorsAsResultArgument_ShouldReturnResultWithSameErrors()
        {
            var emptyResult = CreateCompositionResult(Enumerable.Empty<CompositionError>());

            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult(e);

                var mergedResult = result.MergeResult(emptyResult);

                Assert.Equal(result, mergedResult);
            }
        }

        [Fact]
        public void MergeResult_ResultWithErrorsAsResultArgument_ShouldReturnResultWithCombinedErrors()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result1 = CreateCompositionResult(e);
                var result2 = CreateCompositionResult(e);

                var mergedResult = result1.MergeResult(result2);
                var mergedErrors = result1.Errors.Concat(result2.Errors);

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.Equal(mergedResult.Succeeded, result1.Succeeded | result2.Succeeded);
            }
        }

        [Fact]
        public void MergeError_ValueAsErrorArgumentWhenMergedWithResultWithNullErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult((IEnumerable<CompositionError>)null);
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var mergedResult = result.MergeError(error);
                var mergedErrors = result.Errors.Concat(new CompositionError[] { error });

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.False(mergedResult.Succeeded);                
            }
        }

        [Fact]
        public void MergeError_ValueAsErrorArgumentWhenMergedWithResultWithEmptyErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult(Enumerable.Empty<CompositionError>());
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var mergedResult = result.MergeError(error);
                var mergedErrors = result.Errors.Concat(new CompositionError[] { error });

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.False(mergedResult.Succeeded);
            }
        }

        [Fact]
        public void MergeError_ValueAsErrorArgumentWhenMergedWithResultWithErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult(2);
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var mergedResult = result.MergeError(error);
                var mergedErrors = result.Errors.Concat(new CompositionError[] { error });

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.False(mergedResult.Succeeded);
            }
        }

        [Fact]
        public void MergeErrors_ValueAsErrorArgumentWhenMergedWithResultWithNullErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult((IEnumerable<CompositionError>)null);
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var mergedResult = result.MergeErrors(e);
                var mergedErrors = result.Errors.Concat(e);

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.Equal(!e.Any(), mergedResult.Succeeded);
            }
        }

        [Fact]
        public void MergeErrors_ValueAsErrorArgumentWhenMergedWithResultWithEmptyErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult(Enumerable.Empty<CompositionError>());
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var mergedResult = result.MergeErrors(e);
                var mergedErrors = result.Errors.Concat(e);

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.Equal(!e.Any(), mergedResult.Succeeded);
            }
        }

        [Fact]
        public void MergeErrors_ValueAsErrorArgumentWhenMergedWithResultWithErrors_ShouldReturnResultWithCombinedErrors()
        {
            var result = CreateCompositionResult(2);
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var mergedResult = result.MergeErrors(e);
                var mergedErrors = result.Errors.Concat(e);

                EqualityExtensions.CheckEquals(mergedErrors, mergedResult.Errors);
                Assert.False(mergedResult.Succeeded);
            }
        }

        [Fact]
        public void ThrowOnErrors_NullAsErrorsArgument_ShouldNotThrow()
        {
            var result = CreateCompositionResult((IEnumerable<CompositionError>)null);

            result.ThrowOnErrors();
        }

        [Fact]
        public void ThrowOnErrors_EmptyEnumerableAsErrorsArgument_ShouldNotThrow()
        {
            var result = CreateCompositionResult(Enumerable.Empty<CompositionError>());

            result.ThrowOnErrors();
        }

        [Fact]
        public void ThrowOnErrors_SingleValueAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var errorId in errorIds)
            {
                var result = CreateCompositionResult(errorId);

                CompositionAssert.ThrowsError((ErrorId)errorId, () =>
                {
                    result.ThrowOnErrors();
                });
            }
        }

        [Fact]
        public void ThrowOnErrors_TwoSameValuesAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var errorId in errorIds)
            {
                var result = CreateCompositionResult(errorId, errorId);

                CompositionAssert.ThrowsErrors((ErrorId)errorId, (ErrorId)errorId, () =>
                {
                    result.ThrowOnErrors();
                });
            }
        }

        [Fact]
        public void ThrowOnErrors_TwoDifferentValuesAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds1 = Expectations.GetEnumValues<CompositionErrorId>();
            var errorIds2 = Expectations.GetEnumValues<CompositionErrorId>();

            for (int i = 0; i < errorIds1.Count(); i++)
            {
                var errorId1 = errorIds1.ElementAt(i);
                var errorId2 = errorIds1.ElementAt(errorIds2.Count() - 1 - i);

                var result = CreateCompositionResult(errorId1, errorId2);

                CompositionAssert.ThrowsErrors((ErrorId)errorId1, (ErrorId)errorId2, () =>
                {
                    result.ThrowOnErrors();
                });
            }
        }

        [Fact]
        public void ToResultOfT_NullAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult((IEnumerable<CompositionError>)null);

            var copy = result.ToResult<string>("Value");

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResultOfT_EmptyEnumerableAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult(Enumerable.Empty<CompositionError>());

            var copy = result.ToResult<string>("Value");

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResultOfT_ValueAsErrorsArgument_ShouldReturnResultWithErrorsPropertySet()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult(e);

                var copy = result.ToResult<string>("Value");

                EqualityExtensions.CheckEquals(result.Errors, copy.Errors);
            }
        }

        [Fact]
        public void ToResultOfT_ReferenceValueAsValueArgument_ShouldReturnResultWithValuePropertySet()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult();

                var copy = result.ToResult<object>(e);

                Assert.Same(e, copy.Value);
            }
        }

        [Fact]
        public void ToResultOfT_ValueTypeValueAsValueArgument_ShouldReturnResultWithValuePropertySet()
        {
            var expectations = Expectations.GetObjectsValueTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult();

                var copy = result.ToResult<object>(e);

                Assert.Equal(e, copy.Value);
            }
        }

        [Fact]
        public void SucceededResult_ShouldSetSuccessPropertyToTrue()
        {
            var succeeded = CompositionResult.SucceededResult.Succeeded;

            Assert.True(succeeded);
        }

        [Fact]
        public void SucceededResult_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var errors = CompositionResult.SucceededResult.Errors;

            Assert.Empty(errors);
        }

        private CompositionResult CreateCompositionResult(params CompositionErrorId[] errorIds)
        {
            return new CompositionResult(errorIds.Select(id =>
            {
                return ErrorFactory.Create(id);
            }));
        }

        private CompositionResult CreateCompositionResult(int count)
        {
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            List<CompositionError> errors = new List<CompositionError>();

            foreach (var e in expectations.Take(count))
            {
                errors.Add(ErrorFactory.Create(e));
            }

            return CreateCompositionResult(errors);
        }

        private CompositionResult CreateCompositionResult(IEnumerable<CompositionError> errors)
        {
            return new CompositionResult(errors);
        }
    }
}
