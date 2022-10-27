﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{

    /// <summary>
    /// Used to define arguments to pass to the linker.
    /// 
    /// Don't use this attribute to setup single character flags.  These flags do a poor job of communicating their purpose
    /// and although we need to continue to support the usages that exist today, that doesn't mean we need to make our tests harder to read
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SetupLinkerArgumentAttribute : BaseMetadataAttribute
    {
        public SetupLinkerArgumentAttribute(string flag, params string[] values)
        {
            if (string.IsNullOrEmpty(flag))
                throw new ArgumentNullException(nameof(flag));
        }
    }
}
