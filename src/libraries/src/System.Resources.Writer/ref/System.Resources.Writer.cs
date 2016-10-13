// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Resources
{
    public interface IResourceWriter : IDisposable
    {
        void AddResource(string name, string value);
        void AddResource(string name, object value);
        void AddResource(string name, byte[] value);
        void Close();
        void Generate();
    }
    public sealed class ResourceWriter : IResourceWriter
    {
        public ResourceWriter(string fileName) { }
        public ResourceWriter(System.IO.Stream stream) { }
        public void AddResource(string name, string value) { }
        public void AddResource(string name, object value) { }
        public void AddResource(string name, byte[] value) { }
        public void AddResource(string name, System.IO.Stream value) { }
        public void AddResource(string name, System.IO.Stream value, bool closeAfterWrite) { }
        public void AddResourceData(string name, string typeName, byte[] serializedData) { }
        public void Close() { }
        public void Dispose() { }
        public void Generate() { }
        public System.Func<System.Type, string> TypeNameConverter { get { throw null; } set { } }
    }
}
