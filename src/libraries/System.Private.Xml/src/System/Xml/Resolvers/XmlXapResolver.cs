// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml {
    using System;
    using System.IO;
    using System.Diagnostics;

    //
    // Application stream resolver interface. See XmlXapResolver.s_appStreamResolver for more comments.
    //
    [System.Runtime.CompilerServices.FriendAccessAllowed] // used from System.Windows.dll
    internal interface IApplicationResourceStreamResolver {
        Stream GetApplicationResourceStream(Uri relativeUri);
    }

    /// <summary>
    /// Resolves resources from the application package = xap
    /// Only applicable to Silverlight
    /// </summary>
    public class XmlXapResolver : XmlResolver {

        //
        // Application stream resolver interface. 
        //
        // This interface is set on the XmlXapResolver class by the host initialization code in System.Windows.dll 
        // via the RegisterApplicationResourceStreamResolver method. It is set to a interface whose GetApplicationResourceStream returns
        // Application.GetResourceStream(uri).Stream; We cannot call this API directly because that would create 
        // a circular dependency within the Silverlight core assemblies.
        private static IApplicationResourceStreamResolver s_appStreamResolver;

        [System.Runtime.CompilerServices.FriendAccessAllowed] // used from System.Windows.dll
        internal static void RegisterApplicationResourceStreamResolver(IApplicationResourceStreamResolver appStreamResolver) {
            if (appStreamResolver == null) {
                throw new ArgumentNullException(nameof(appStreamResolver));
            }
            s_appStreamResolver = appStreamResolver;
        }

        /// <summary>
        /// Public constructor
        /// </summary>
        public XmlXapResolver() {
        }

        public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {
            if (absoluteUri == null) {
                throw new ArgumentNullException(nameof(absoluteUri));
            }

            if (s_appStreamResolver == null) {
                Debug.Assert(false, "No IApplicationResourceStreamResolver is registered on the XmlXapResolver.");
                throw new XmlException(SR.Xml_InternalError, string.Empty);
            }

            if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(Object)) {
                // Note that even though the parameter is called absoluteUri we will only accept
                //   relative Uris here. The base class's ResolveUri can create a relative uri
                //   if no baseUri is specified.
                // Check the argument for common schemes (http, file) and throw exception with nice error message.
                Stream stream;
                try {
                    stream = s_appStreamResolver.GetApplicationResourceStream(absoluteUri);
                }
                catch (ArgumentException e) {
                    throw new XmlException(SR.Xml_XapResolverCannotOpenUri, absoluteUri.ToString(), e, null);
                }
                if (stream == null) {
                    throw new XmlException(SR.Xml_CannotFindFileInXapPackage, absoluteUri.ToString());
                }
                return stream;
            }
            else {
                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }
        }
    }
}
