// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Xml
{
    public abstract partial class XmlResolver
    {
        public virtual Task<Object> GetEntityAsync(Uri absoluteUri,
                                             string role,
                                             Type ofObjectToReturn)
        {
            throw new NotImplementedException();
        }
    }
}