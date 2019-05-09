// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    public class ManifestResourceInfo
    {
        public ManifestResourceInfo(Assembly containingAssembly,
                                      string containingFileName,
                                      ResourceLocation resourceLocation)
        {
            ReferencedAssembly = containingAssembly;
            FileName = containingFileName;
            ResourceLocation = resourceLocation;
        }

        public virtual Assembly ReferencedAssembly { get; }
        public virtual string FileName { get; }
        public virtual ResourceLocation ResourceLocation { get; }
    }
}

