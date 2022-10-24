// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SetupLinkerDescriptorFile : BaseMetadataAttribute
    {
        public SetupLinkerDescriptorFile(string relativePathToFile, string destinationFileName = null)
        {
        }
    }
}
