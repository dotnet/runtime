// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    /// <summary>
    ///     The IConfigSectionHandler interface defines the contract that all configuration section handlers
    ///     must implement in order to participate in the resolution of configuration settings.
    ///     Composes and creates config objects.
    ///     This interface is implemented by config providers. Classes implementing IConfigSectionHandler
    ///     define the rules for cooking XML config into usable objects. The cooked objects can be of arbitrary
    ///     type.
    ///     Configuration is composable (e.g., config in a child directory is layered over config in a parent
    ///     directory), so, IConfigSectionHandler is supplied with the parent config as well as any number of
    ///     XML fragments.
    /// </summary>
    public interface IConfigurationSectionHandler
    {
        /// <summary>
        ///     The function is responsible for inspecting "section", "context", and "parent", and creating
        ///     a config object.
        ///     Note that "parent" is guaranteed to be an object that was returned from a Create call on the
        ///     same IConfigSectionHandler implementation. (E.g., if Create returns a Hashtable, then "parent"
        ///     is always a Hashtable if it's non-null.)
        ///     Returned objects must be immutable. In particular, it's important that the "parent" object
        ///     being passed in is not altered: if a modification must be made, then it must be cloned before
        ///     it is modified.
        /// </summary>
        /// <param name="parent">the object inherited from parent path</param>
        /// <param name="configContext">reserved, in ASP.NET used to convey virtual path of config being evaluated</param>
        /// <param name="section">the xml node rooted at the section to handle</param>
        /// <returns>a new config object</returns>
        object Create(object parent, object configContext, XmlNode section);
    }
}