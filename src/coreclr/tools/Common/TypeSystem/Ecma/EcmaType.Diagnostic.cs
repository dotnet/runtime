// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// Override of MetadataType that uses actual Ecma335 metadata.
    /// </summary>
    public sealed partial class EcmaType
    {
        public override string DiagnosticName
        {
            get
            {
                try
                {
                    return Name;
                }
                catch
                {
                    return $"TypeDef({MetadataReader.GetToken(Handle):x8})";
                }
            }
        }
        public override string DiagnosticNamespace
        {
            get
            {
                try
                {
                    return Namespace;
                }
                catch
                {
                    return ""; // If namespace throws, then Name will as well, and it will attach the token as the name instead.
                }
            }
        }
    }
}
