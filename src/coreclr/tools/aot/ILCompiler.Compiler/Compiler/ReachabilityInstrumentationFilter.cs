// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// An <see cref="ILProvider"/> that filters methods based on a filter in a file. Methods not specified in
    /// the filter are replaced with a failfast call.
    /// </summary>
    public class ReachabilityInstrumentationFilter : ILProvider
    {
        private readonly Dictionary<Guid, bool[]> _reachabilityInfo = new Dictionary<Guid, bool[]>();
        private readonly ILProvider _nestedProvider;

        public ReachabilityInstrumentationFilter(string profileDataFileName, ILProvider nestedProvider)
        {
            _nestedProvider = nestedProvider;

            // Read the file

            using BinaryReader reader = new BinaryReader(File.OpenRead(profileDataFileName));

            int length = reader.ReadInt32();
            if (length != (int)reader.BaseStream.Length)
                throw new IOException("Invalid header");

            Span<byte> guidBytes = stackalloc byte[16];
            while (true)
            {
                if (reader.Read(guidBytes) != guidBytes.Length)
                    break;

                int numTokens = reader.ReadInt32();

                bool[] tokenStates = new bool[numTokens];
                if (reader.Read(MemoryMarshal.Cast<bool, byte>(tokenStates.AsSpan())) != numTokens)
                    throw new IOException("Unexpected end of file");

                _reachabilityInfo.Add(new Guid(guidBytes), tokenStates);
            }
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            MethodIL nestedIL = _nestedProvider.GetMethodIL(method);

            if (method is not EcmaMethod ecmaMethod || nestedIL is null)
            {
                return nestedIL;
            }

            Guid mvid = ecmaMethod.MetadataReader.GetGuid(ecmaMethod.MetadataReader.GetModuleDefinition().Mvid);
            int rowNumber = MetadataTokens.GetRowNumber(ecmaMethod.Handle);
            if (!_reachabilityInfo.TryGetValue(mvid, out bool[] tokenStates) || rowNumber >= tokenStates.Length)
                //throw new Exception("Mismatched profile data");
                return nestedIL;

            // Method is present in the profile, return unmodified
            if (tokenStates[rowNumber])
                return nestedIL;

            // Method not present: stub it out
            var emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();
            codeStream.EmitCallThrowHelper(emit, method.Context.SystemModule.GetKnownType("System.Runtime"u8, "InternalCalls"u8).GetKnownMethod("RhpFallbackFailFast"u8, null));
            return emit.Link(method);
        }
    }
}
