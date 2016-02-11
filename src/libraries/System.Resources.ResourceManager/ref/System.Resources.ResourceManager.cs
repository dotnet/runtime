// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Resources
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public partial class MissingManifestResourceException : System.Exception
    {
        public MissingManifestResourceException() { }
        public MissingManifestResourceException(string message) { }
        public MissingManifestResourceException(string message, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class NeutralResourcesLanguageAttribute : System.Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName) { }
        public string CultureName { get { return default(string); } }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public partial class ResourceManager
    {
        public ResourceManager(string baseName, System.Reflection.Assembly assembly) { }
        public ResourceManager(System.Type resourceSource) { }
        public string GetString(string name) { return default(string); }
        public virtual string GetString(string name, System.Globalization.CultureInfo culture) { return default(string); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class SatelliteContractVersionAttribute : System.Attribute
    {
        public SatelliteContractVersionAttribute(string version) { }
        public string Version { get { return default(string); } }
    }
}
