// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: InternalsVisibleTo(InvalidFriendAssemblyName.InvalidAssemblyName)]

public class InvalidFriendAssemblyName
{
    internal const string InvalidAssemblyName = "Broken,PublicKey=00240000048000009400000006020000002400005253413100040000010001002d07581667cbf8caf9786ac8d5257eb2c77eb2643a1af1bb89d4286e27a3ff5d805408c9f1a0a392d2f478ca2b9c68e43cdcdcea2d7cf0618dd8ba48858bf5fc74c7fc2af7a29f936398c0f61a2165d08ac4fcc8e8ad04e24633b7ca31a7c13f750ae75e53cea55ced97d3289fd100dbe2661a802bf8bd2acb0b6893ef3fb79c,PublicKeyToken=282361b99ded7e8e";

    // Only CoreCLR surfaces the invalid friend name as a FileLoadException.
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime), nameof(TestLibrary.Utilities.IsNotNativeAot))]
    public static void TestEntryPoint()
    {
        // Calling the protected Object.MemberwiseClone (defined in a different assembly) forces the
        // runtime to run an accessibility check against this assembly, which parses its InternalsVisibleTo
        // metadata. Because that metadata is invalid, parsing throws and surfaces the FileLoadException.
        FileLoadException exception = Assert.Throws<FileLoadException>(() => new InvalidFriendAssemblyName().GetCopy());

        // The outer exception identifies the assembly that declared the invalid friend reference.
        Assert.Contains(nameof(InvalidFriendAssemblyName), exception.FileName, StringComparison.Ordinal);

        // The inner exception preserves the real cause: the invalid friend assembly name.
        FileLoadException inner = Assert.IsType<FileLoadException>(exception.InnerException);
        Assert.Equal(InvalidAssemblyName, inner.FileName);
        Assert.Contains("assembly name was invalid", inner.Message, StringComparison.OrdinalIgnoreCase);
    }

    public object GetCopy() => MemberwiseClone();
}
