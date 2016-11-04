// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
