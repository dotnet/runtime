// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Resources
{
    public partial interface IResourceReader : System.Collections.IEnumerable, System.IDisposable
    {
        void Close();
        new System.Collections.IDictionaryEnumerator GetEnumerator();
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class MissingManifestResourceException : System.SystemException
    {
        public MissingManifestResourceException() { }
        protected MissingManifestResourceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingManifestResourceException(string message) { }
        public MissingManifestResourceException(string message, System.Exception inner) { }
    }
    public partial class MissingSatelliteAssemblyException : System.SystemException
    {
        public MissingSatelliteAssemblyException() { }
        protected MissingSatelliteAssemblyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingSatelliteAssemblyException(string message) { }
        public MissingSatelliteAssemblyException(string message, System.Exception inner) { }
        public MissingSatelliteAssemblyException(string message, string cultureName) { }
        public string CultureName { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class NeutralResourcesLanguageAttribute : System.Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName) { }
        public NeutralResourcesLanguageAttribute(string cultureName, System.Resources.UltimateResourceFallbackLocation location) { }
        public string CultureName { get { throw null; } }
        public System.Resources.UltimateResourceFallbackLocation Location { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class ResourceManager
    {
        public static readonly int HeaderVersionNumber;
        public static readonly int MagicNumber;
        protected System.Reflection.Assembly MainAssembly;
        protected ResourceManager() { }
        public ResourceManager(string baseName, System.Reflection.Assembly assembly) { }
        public ResourceManager(string baseName, System.Reflection.Assembly assembly, System.Type usingResourceSet) { }
        public ResourceManager(System.Type resourceSource) { }
        public virtual string BaseName { get { throw null; } }
        protected System.Resources.UltimateResourceFallbackLocation FallbackLocation { get { throw null; } set { } }
        public virtual bool IgnoreCase { get { throw null; } set { } }
        public virtual System.Type ResourceSetType { get { throw null; } }
        public static System.Resources.ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir, System.Type usingResourceSet) { throw null; }
        protected static System.Globalization.CultureInfo GetNeutralResourcesLanguage(System.Reflection.Assembly a) { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, System.Globalization.CultureInfo culture) { throw null; }
        protected virtual string GetResourceFileName(System.Globalization.CultureInfo culture) { throw null; }
        public virtual System.Resources.ResourceSet GetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        protected static System.Version GetSatelliteContractVersion(System.Reflection.Assembly a) { throw null; }
        public System.IO.UnmanagedMemoryStream GetStream(string name) { throw null; }
        public System.IO.UnmanagedMemoryStream GetStream(string name, System.Globalization.CultureInfo culture) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, System.Globalization.CultureInfo culture) { throw null; }
        protected virtual System.Resources.ResourceSet InternalGetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        public virtual void ReleaseAllResources() { }
    }
    public sealed partial class ResourceReader : System.Collections.IEnumerable, System.IDisposable, System.Resources.IResourceReader
    {
        public ResourceReader(System.IO.Stream stream) { }
        public ResourceReader(string fileName) { }
        public void Close() { }
        public void Dispose() { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public void GetResourceData(string resourceName, out string resourceType, out byte[] resourceData) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class ResourceSet : System.Collections.IEnumerable, System.IDisposable
    {
        protected ResourceSet() { }
        public ResourceSet(System.IO.Stream stream) { }
        public ResourceSet(System.Resources.IResourceReader reader) { }
        public ResourceSet(string fileName) { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Type GetDefaultReader() { throw null; }
        public virtual System.Type GetDefaultWriter() { throw null; }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, bool ignoreCase) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, bool ignoreCase) { throw null; }
        protected virtual void ReadResources() { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class SatelliteContractVersionAttribute : System.Attribute
    {
        public SatelliteContractVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly = 0,
        Satellite = 1,
    }
}
