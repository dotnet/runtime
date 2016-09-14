// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.ComponentModel;

namespace System.Xml
{
    [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class XmlXapResolver : XmlResolver
    {
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public XmlXapResolver() { }

        //can't mark obsolete because we get warning as error in the 
        //build system. however, since the object cannot be constructed, 
        //the third party dev cannot somehow write this line of code
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            //we throw what the user could naturally expect if networking capabilities weren't available
            //which is a reasonable thing in designer mode
            throw new XmlException(SR.Xml_XapResolverCannotOpenUri, absoluteUri.ToString(), default(Exception), null);
        }

        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RegisterApplicationResourceStreamResolver(IApplicationResourceStreamResolver appStreamResolver) { }
    }
}
