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
    public class CompositionResultOfTTest
    {
        [Fact]
        public void Constructor1_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>();

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor2_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>("Value");

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor3_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>((CompositionError[])null);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor4_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>((IEnumerable<CompositionError>)null);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor5_NullAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>("Value", (IEnumerable<CompositionError>)null);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor3_EmptyAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>(new CompositionError[0]);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor4_EmptyAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>(Enumerable.Empty<CompositionError>());

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor5_EmptyAsErrorsArgument_ShouldSetErrorsPropertyToEmptyEnumerable()
        {
            var result = new CompositionResult<string>("Value", Enumerable.Empty<CompositionError>());

            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Constructor3_ValuesAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>(e.ToArray());

                Assert.Equal(e, result.Errors);
            }
        }

        [Fact]
        public void Constructor4_ValuesAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>(e);

                Assert.Equal(e, result.Errors);
            }
        }

        [Fact]
        public void Constructor5_ValuesAsErrorsArgument_ShouldSetErrorsProperty()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>("Value", e);

                Assert.Equal(e, result.Errors);
            }
        }

        [Fact]
        public void Constructor1_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>();

            Assert.True(result.Succeeded);
        }

[Fact]
        public void Constructor2_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>("Value");

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor3_NullAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>((CompositionError[])null);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor4_NullAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>((IEnumerable<CompositionError>)null);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor5_NullAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>("Value", (IEnumerable<CompositionError>)null);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor3_EmptyAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>(new CompositionError[0]);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor4_EmptyAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>(Enumerable.Empty<CompositionError>());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor5_EmptyAsErrorsArgument_ShouldSetSucceededPropertyToTrue()
        {
            var result = new CompositionResult<string>("Value", Enumerable.Empty<CompositionError>());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Constructor3_ValuesAsErrorsArgument_ShouldSetSucceededPropertyToTrueIfThereAreErrors()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>(e.ToArray());

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
        public void Constructor4_ValuesAsErrorsArgument_ShouldSetSucceededPropertyToTrueIfThereAreErrors()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>(e);

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
        public void Constructor5_ValuesAsErrorsArgument_ShouldSetSucceededPropertyToTrueIfThereAreErrors()
        {
            var errors = Expectations.GetCompositionErrors();

            foreach (var e in errors)
            {
                var result = new CompositionResult<string>("Value", e);

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
        public void ToResult_NullAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult<string>((IEnumerable<CompositionError>)null);

            var copy = result.ToResult();

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResult_EmptyEnumerableAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult<string>(Enumerable.Empty<CompositionError>());

            var copy = result.ToResult();

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResult_ValueAsErrorsArgument_ShouldReturnResultWithErrorsPropertySet()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<string>(e);

                var copy = result.ToResult();

                EqualityExtensions.CheckEquals(result.Errors, copy.Errors);
            }
        }

        [Fact]
        public void ToResultOfT_NullAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult<string>((IEnumerable<CompositionError>)null);

            var copy = result.ToResult<string>();

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResultOfT_EmptyEnumerableAsErrorsArgument_ShouldReturnResultWithEmptyErrorsProperty()
        {
            var result = CreateCompositionResult<string>(Enumerable.Empty<CompositionError>());

            var copy = result.ToResult<string>();

            Assert.Empty(copy.Errors);
        }

        [Fact]
        public void ToResultOfT_ValueAsErrorsArgument_ShouldReturnResultWithErrorsPropertySet()
        {
            var expectations = Expectations.GetCompositionErrors();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<string>(e);

                var copy = result.ToResult<string>();

                EqualityExtensions.CheckEquals(result.Errors, copy.Errors);
            }
        }

        [Fact]
        public void ToResultOfT_ReferenceValueAsValueArgument_ShouldReturnResultWithNullValueProperty()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<object>(e);

                var copy = result.ToResult<object>();

                Assert.Null(copy.Value);                
            }
        }

        [Fact]
        public void ToResultOfT_ValueTypeValueAsValueArgument_ShouldReturnResultWithNullValueProperty()
        {
            var expectations = Expectations.GetObjectsValueTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<ValueType>(e);

                var copy = result.ToResult<ValueType>();

                Assert.Null(copy.Value);
            }
        }

        [Fact]
        public void Value_NullAsErrorsArgumentAndValueAsValueArgument_ShouldReturnValue()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<object>(e, (IEnumerable<CompositionError>)null);

                Assert.Equal(e, result.Value);
            }
        }

        [Fact]
        public void Value_EmptyEnumerableAsErrorsArgumentAndValueAsValueArgument_ShouldReturnValue()
        {
            var expectations = Expectations.GetObjectsReferenceTypes();

            foreach (var e in expectations)
            {
                var result = CreateCompositionResult<object>(e, Enumerable.Empty<CompositionError>());

                Assert.Equal(e, result.Value);
            }
        }

        [Fact]
        public void Value_SingleValueAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var errorId in errorIds)
            {
                var result = CreateCompositionResult<string>(errorId);

                CompositionAssert.ThrowsError((ErrorId)errorId, () =>
                {
                    var value = result.Value;
                });
            }
        }

        [Fact]
        public void Value_TwoSameValuesAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds = Expectations.GetEnumValues<CompositionErrorId>();

            foreach (var errorId in errorIds)
            {
                var result = CreateCompositionResult<string>(errorId, errorId);

                CompositionAssert.ThrowsErrors((ErrorId)errorId, (ErrorId)errorId, () =>
                {
                    var value = result.Value;
                });
            }
        }

        [Fact]
        public void Value_TwoDifferentValuesAsErrorsArgument_ShouldThrowComposition()
        {
            var errorIds1 = Expectations.GetEnumValues<CompositionErrorId>();
            var errorIds2 = Expectations.GetEnumValues<CompositionErrorId>();

            for (int i = 0; i < errorIds1.Count(); i++)
            {
                var errorId1 = errorIds1.ElementAt(i);
                var errorId2 = errorIds1.ElementAt(errorIds2.Count() - 1 - i);

                var result = CreateCompositionResult<string>(errorId1, errorId2);

                CompositionAssert.ThrowsErrors((ErrorId)errorId1, (ErrorId)errorId2, () =>
                {
                    var value = result.Value;
                });
            }
        }

        private CompositionResult<T> CreateCompositionResult<T>(params CompositionErrorId[] errorIds)
        {
            return new CompositionResult<T>(errorIds.Select(id =>
            {
                return ErrorFactory.Create(id);
            }));
        }

        private CompositionResult<T> CreateCompositionResult<T>(int count)
        {
            var expectations = Expectations.GetEnumValues<CompositionErrorId>();

            List<CompositionError> errors = new List<CompositionError>();

            foreach (var e in expectations.Take(count))
            {
                errors.Add(ErrorFactory.Create(e));
            }

            return CreateCompositionResult<T>(errors);
        }

        private CompositionResult<T> CreateCompositionResult<T>(IEnumerable<CompositionError> errors)
        {
            return new CompositionResult<T>(errors);
        }

        private CompositionResult<T> CreateCompositionResult<T>(T value)
        {
            return new CompositionResult<T>(value);
        }

        private CompositionResult<T> CreateCompositionResult<T>(T value, IEnumerable<CompositionError> errors)
        {
            return new CompositionResult<T>(value, errors);
        }
    }
}
