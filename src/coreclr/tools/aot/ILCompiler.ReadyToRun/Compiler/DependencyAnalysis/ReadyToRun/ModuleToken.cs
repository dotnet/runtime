// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This helper structure encapsulates a module-qualified token.
    /// </summary>
    public struct ModuleToken : IEquatable<ModuleToken>
    {
        public readonly IEcmaModule Module;
        public readonly mdToken Token;

        public ModuleToken(IEcmaModule module, mdToken token)
        {
            Module = module;
            Token = token;
        }
        public ModuleToken(IEcmaModule module, EntityHandle entityHandle)
        {
            Module = module;
            Token = (mdToken)MetadataTokens.GetToken(entityHandle);
        }

        public ModuleToken(IEcmaModule module, Handle handle)
        {
            Module = module;
            Token = (mdToken)MetadataTokens.GetToken(handle);
        }

        public bool IsNull => Module == null;

        public override int GetHashCode()
        {
            return IsNull ? 0 : Module.GetHashCode() ^ unchecked((int)(31 * (uint)Token));
        }

        public override string ToString()
        {
            if (IsNull)
            {
                return "default(ModuleToken)";
            }
            return Module.ToString() + ":" + ((uint)Token).ToString("X8");
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleToken moduleToken &&
                Equals(moduleToken);
        }

        public bool Equals(ModuleToken moduleToken)
        {
            return Module == moduleToken.Module && Token == moduleToken.Token;
        }

        public int CompareTo(ModuleToken other)
        {
            int result = ((int)Token).CompareTo((int)other.Token);
            if (result != 0)
                return result;

            return Module.CompareTo(other.Module);
        }

        public MetadataReader MetadataReader => Module.MetadataReader;

        public CorTokenType TokenType => SignatureBuilder.TypeFromToken(Token);

        public uint TokenRid => SignatureBuilder.RidFromToken(Token);

        public Handle Handle => MetadataTokens.Handle((int)Token);
    }
}
