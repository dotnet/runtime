// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler
{
    /// <summary>
    /// A root provider that provides a runtime configuration blob that influences runtime behaviors.
    /// See RhConfigValues.h for allowed values.
    /// </summary>
    public class RuntimeKnobsRootProvider : ICompilationRootProvider
    {
        private readonly IEnumerable<string> _runtimeKnobs;

        public RuntimeKnobsRootProvider(IEnumerable<string> runtimeKnobs)
        {
            _runtimeKnobs = runtimeKnobs;
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            rootProvider.RootReadOnlyDataBlob(GetRuntimeKnobsBlob(), 4, "Runtime configuration knobs", "g_compilerEmbeddedKnobsBlob");
        }

        protected byte[] GetRuntimeKnobsBlob()
        {
            const int HeaderSize = 4;

            ArrayBuilder<byte> options = default(ArrayBuilder<byte>);

            // Reserve space for the header
            options.ZeroExtend(HeaderSize);

            foreach (string option in _runtimeKnobs)
            {
                byte[] optionBytes = System.Text.Encoding.UTF8.GetBytes(option);
                options.Append(optionBytes);

                // Emit a null to separate the next option
                options.Add(0);
            }

            byte[] result = options.ToArray();

            int length = options.Count - HeaderSize;

            // Encode the size of the blob into the header
            result[0] = (byte)length;
            result[1] = (byte)(length >> 8);
            result[2] = (byte)(length >> 0x10);
            result[3] = (byte)(length >> 0x18);

            return result;
        }
    }
}
