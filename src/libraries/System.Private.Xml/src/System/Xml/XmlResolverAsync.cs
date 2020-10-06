// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Xml
{
    public abstract partial class XmlResolver
    {
        public virtual Task<object> GetEntityAsync(Uri absoluteUri,
                                             string? role,
                                             Type? ofObjectToReturn)
        {
            throw new NotImplementedException();
        }
    }
}
