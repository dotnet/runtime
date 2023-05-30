// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler
{
    /// <summary>
    /// A root provider that provides a runtime configuration blob that influences runtime behaviors.
    /// See RhConfigValues.h for allowed values.
    /// </summary>
    public class RuntimeConfigurationRootProvider : ICompilationRootProvider
    {
        private readonly string _blobName;
        private readonly IReadOnlyCollection<string> _runtimeOptions;

        public RuntimeConfigurationRootProvider(string blobName, IReadOnlyCollection<string> runtimeOptions)
        {
            _blobName = blobName;
            _runtimeOptions = runtimeOptions;
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.RootReadOnlyDataBlob(GetRuntimeOptionsBlob(), 4, "Runtime configuration information", _blobName);
        }

        private byte[] GetRuntimeOptionsBlob()
        {
            ArrayBuilder<byte> options = default(ArrayBuilder<byte>);

            int count = _runtimeOptions.Count;

            options.Add((byte)count);
            options.Add((byte)(count >> 8));
            options.Add((byte)(count >> 0x10));
            options.Add((byte)(count >> 0x18));

            foreach (string option in _runtimeOptions)
            {
                byte[] optionBytes = System.Text.Encoding.UTF8.GetBytes(option);
                options.Append(optionBytes);

                // Emit a null to separate the next option
                options.Add(0);
            }

            return options.ToArray();
        }
    }
}
