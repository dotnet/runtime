// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace WebAssemblyInfo
{
    public class GenericContext
    {
        public GenericParameterHandleCollection Parameters { get; }
        public GenericParameterHandleCollection TypeParameters { get; }
        public MetadataReader Reader { get; }

        public GenericContext(GenericParameterHandleCollection parameters, GenericParameterHandleCollection typeParameters, MetadataReader reader)
        {
            Parameters = parameters;
            TypeParameters = typeParameters;
            Reader = reader;
        }
    }
}
