// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestUtilities;

public class RandomTestCollectionOrderer : ITestCollectionOrderer
{
    private readonly IMessageSink _diagnosticMessageSink;

    public RandomTestCollectionOrderer(IMessageSink diagnosticMessageSink)
    {
        diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                                                $"Using random seed for collections: {RandomTestCaseOrderer.LazySeed.Value}"));
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        => RandomTestCaseOrderer.TryRandomize(testCollections.ToList(), _diagnosticMessageSink, out List<ITestCollection>? randomizedTests)
                    ? randomizedTests
                    : testCollections;
}
