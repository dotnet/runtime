// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaGenericParameter
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
                    return $"GenericParam({MetadataReader.GetToken(Handle):x8})";
                }
            }
        }
    }
}
