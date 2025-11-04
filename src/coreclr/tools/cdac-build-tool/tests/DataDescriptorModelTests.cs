// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool.Tests;

public class DataDescriptorModelTests
{
    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    [Fact]
    public void Builder_WithEmptyBaseline_ProducesFullModel()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            // Create an empty baseline
            var emptyBaselinePath = Path.Combine(tempDir, "empty.jsonc");
            File.WriteAllText(emptyBaselinePath, @"
// the empty baseline data descriptor
{
    ""version"": 0
}");

            var builder = new DataDescriptorModel.Builder(tempDir);
            builder.SetBaseline("empty");

            // Add some test data
            var typeBuilder = builder.AddOrUpdateType("TestType", 16);
            typeBuilder.AddOrUpdateField("Field1", "uint32", 0);
            typeBuilder.AddOrUpdateField("Field2", "pointer", 8);

            builder.AddOrUpdateGlobal("TestGlobal", "uint32", DataDescriptorModel.GlobalValue.MakeDirect(42));
            builder.AddOrUpdateContract("TestContract", 1);

            var model = builder.Build();

            // Verify all items are in the model
            Assert.Equal("empty", model.Baseline);
            Assert.Contains("TestType", model.Types.Keys);
            Assert.Contains("TestGlobal", model.Globals.Keys);
            Assert.Contains("TestContract", model.Contracts.Keys);

            var testType = model.Types["TestType"];
            Assert.Equal(16, testType.Size);
            Assert.Equal(2, testType.Fields.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Builder_WithOverrideBaselineName_UsesOverride()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            // Create empty baselines (only empty baselines are supported for now)
            var emptyPath = Path.Combine(tempDir, "empty.jsonc");
            File.WriteAllText(emptyPath, "{ \"version\": 0 }");

            var baseline2Path = Path.Combine(tempDir, "baseline2.jsonc");
            File.WriteAllText(baseline2Path, "{ \"version\": 0 }");

            // Create builder with override baseline name set to "baseline2"
            var builder = new DataDescriptorModel.Builder(tempDir, "baseline2");

            // Try to set empty, but baseline2 should be used due to override
            builder.SetBaseline("empty");

            // Add a type
            builder.AddOrUpdateType("TestType", 8);

            var model = builder.Build();

            // Verify baseline2 was used as the baseline name (override worked)
            Assert.Equal("baseline2", model.Baseline);
            // With empty baselines, type should be included in output
            Assert.Contains("TestType", model.Types.Keys);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Builder_WithoutOverride_UsesScrapedBaseline()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            // Create an empty baseline
            var emptyPath = Path.Combine(tempDir, "empty.jsonc");
            File.WriteAllText(emptyPath, "{ \"version\": 0 }");

            // Create builder without override
            var builder = new DataDescriptorModel.Builder(tempDir, null);

            // Set baseline from scraped data
            builder.SetBaseline("empty");

            // Add a type
            builder.AddOrUpdateType("TestType", 16);

            var model = builder.Build();

            // Verify empty baseline was used (no override)
            Assert.Equal("empty", model.Baseline);
            Assert.Contains("TestType", model.Types.Keys);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Builder_ThrowsOnUnknownBaseline()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var builder = new DataDescriptorModel.Builder(tempDir);

            Assert.Throws<InvalidOperationException>(() => builder.SetBaseline("nonexistent"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
