// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Resources
{
    public sealed partial class ResourceReader : System.IDisposable
    {
        [System.Security.SecurityCriticalAttribute]
        public ResourceReader(System.IO.Stream stream) { }
        public void Dispose() { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
    }
    public sealed partial class ResourceWriter : System.IDisposable
    {
        public ResourceWriter(System.IO.Stream stream) { }
        public void AddResource(string name, string value) { }
        public void Dispose() { }
        public void Generate() { }
    }
}
