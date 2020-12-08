// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AssemblyResolveTestApp
{
    public class PublicClassSample
    {
        public PublicClassSample() { }
        public PublicClassSample(int param) { }
    }

    class PrivateClassSample
    {
        public PrivateClassSample() { }
        public PrivateClassSample(int param) { }
    }

    public class PublicClassNoDefaultConstructorSample
    {
        public PublicClassNoDefaultConstructorSample(int param) { }
    }
}
