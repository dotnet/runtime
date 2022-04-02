// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ActivatorUtilitiesTests
    {
        [Theory]
        [MemberData(nameof(ActivatorUtilitiesData))]
        public void ActivatorUtilitiesShouldBeOrderAgnostic(Type instanceType)
        {
            var data = new Dictionary<string, object>();
            var serviceProvider = new FakeServiceProvider(t =>
            {
                if (t == typeof(FakeValidationStatus)) return FakeValidationStatus.Invalid;
                throw new NotImplementedException();
            });

            var instance = (IFakeValidationResult)ActivatorUtilities
                .CreateInstance(serviceProvider, instanceType, "description", data);

            Assert.Equal(FakeValidationStatus.Invalid, instance.Status);
        }

        public static TheoryData<Type> ActivatorUtilitiesData
        {
            get => new TheoryData<Type>
            {
                typeof(FakeValidationResult),
                 typeof(FakeValidationResultOps)
            };
        }
    }

    internal class FakeServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object?> _factory;

        public FakeServiceProvider(Func<Type, object?> factory)
        {
            _factory = factory;
        }

        public object? GetService(Type serviceType) => _factory(serviceType);
    }

    internal interface IFakeValidationResult
    {
        FakeValidationStatus Status { get; }
        string Description { get; }
        IReadOnlyDictionary<string, object> Data { get; }
    }

    internal class FakeValidationResult : IFakeValidationResult
    {
        [ActivatorUtilitiesConstructor]
        public FakeValidationResult(FakeValidationStatus status, string description,
           IReadOnlyDictionary<string, object> data)
        {
            Status = status;
            Description = description;
            Data = data;
        }

        public FakeValidationResult(string description, IReadOnlyDictionary<string, object> data)
            : this(FakeValidationStatus.Valid, description, data) { }

        public FakeValidationStatus Status { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
    }
    internal class FakeValidationResultOps : IFakeValidationResult
    {
        public FakeValidationResultOps(string description, IReadOnlyDictionary<string, object> data)
            : this(FakeValidationStatus.Valid, description, data) { }

        [ActivatorUtilitiesConstructor]
        public FakeValidationResultOps(FakeValidationStatus status, string description,
           IReadOnlyDictionary<string, object> data)
        {
            Status = status;
            Description = description;
            Data = data;
        }

        public FakeValidationStatus Status { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
    }

    internal enum FakeValidationStatus
    {
        Valid,
        Invalid
    }
}
