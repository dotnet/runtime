// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Mono
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MonoDomainSetup
    {
        #region Sync with object-internals.h
        private string? application_base;
        private string? application_name;
        private string? cache_path;
        private string? configuration_file;
        private string? dynamic_base;
        private string? license_file;
        private string? private_bin_path;
        private string? private_bin_path_probe;
        private string? shadow_copy_directories;
        private string? shadow_copy_files;
        private bool publisher_policy;
        private bool path_changed;
        private int loader_optimization;
        private bool disallow_binding_redirects;
        private bool disallow_code_downloads;

        private object? _activationArguments;
        private object? domain_initializer;
        private object? application_trust;
        private string[]? domain_initializer_args;

        private bool disallow_appbase_probe;
        private byte[]? configuration_bytes;

        private byte[]? serialized_non_primitives;
        #endregion

        public MonoDomainSetup()
        {
        }
    }
}
