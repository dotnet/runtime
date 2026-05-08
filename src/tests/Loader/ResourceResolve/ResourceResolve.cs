// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Xunit;

[ConditionalClass(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotNativeAot))]
public unsafe class ResourceResolve
{
    [Fact]
    [SkipOnMono("AssemblyRef manifest resource is not supported")]
    public static void AssemblyRef()
    {
        string resourceName = "MyResource";
        Assembly assembly = typeof(ResourceResolve).Assembly;

        // Manifest resource is not in the current assembly
        Stream stream = assembly.GetManifestResourceStream(resourceName);
        Assert.Null(stream);

        // Handler returns assembly with a manifest resource assembly ref that
        // points to another assembly with the resource
        ResolveEventHandler handler = (sender, args) =>
        {
            if (args.Name == resourceName && args.RequestingAssembly == assembly)
                return Assembly.Load("ManifestResourceAssemblyRef");

            return null;
        };
        AppDomain.CurrentDomain.ResourceResolve += handler;
        stream = assembly.GetManifestResourceStream(resourceName);
        AppDomain.CurrentDomain.ResourceResolve -= handler;
        Assert.NotNull(stream);

        // Verify that the stream matches the expected one in the resource assembly
        Assembly resourceAssembly = Assembly.Load("ResourceAssembly");
        Stream expected = resourceAssembly.GetManifestResourceStream(resourceName);
        Assert.Equal(expected.Length, stream.Length);
        Span<byte> expectedBytes = new byte[expected.Length];
        expected.Read(expectedBytes);
        Span<byte> streamBytes = new byte[stream.Length];
        stream.Read(streamBytes);
        Assert.Equal(expectedBytes, streamBytes);
    }
}
