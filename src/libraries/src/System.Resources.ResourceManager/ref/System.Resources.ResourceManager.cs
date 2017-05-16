// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Resources
{
    public interface IResourceReader : System.Collections.IEnumerable, System.IDisposable
    {
        void Close();
        new System.Collections.IDictionaryEnumerator GetEnumerator();
    }
    public sealed partial class ResourceReader : IResourceReader
    {
        public ResourceReader(System.IO.Stream stream) { }
        public ResourceReader(string fileName) { }
        public void Close() { }
        public void Dispose() { }
        public void GetResourceData(string resourceName, out string resourceType, out byte[] resourceData) { throw null; }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public partial class MissingManifestResourceException : System.SystemException
    {
        public MissingManifestResourceException() { }
        public MissingManifestResourceException(string message) { }
        public MissingManifestResourceException(string message, System.Exception inner) { }
        protected MissingManifestResourceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public class MissingSatelliteAssemblyException : System.SystemException
    {
        public MissingSatelliteAssemblyException(): base()  { }
        public MissingSatelliteAssemblyException(String message): base() { }
        public MissingSatelliteAssemblyException(String message, String cultureName): base() { }
        public MissingSatelliteAssemblyException(String message, Exception inner): base() { }
        protected MissingSatelliteAssemblyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context): base() { }
        public String CultureName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed class NeutralResourcesLanguageAttribute : System.Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName) { }
        public string CultureName { get { throw null; } }
        public NeutralResourcesLanguageAttribute(string cultureName, UltimateResourceFallbackLocation location) { }
        public UltimateResourceFallbackLocation Location { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public class ResourceManager
    {
        public static readonly int HeaderVersionNumber;
        public static readonly int MagicNumber;
        protected System.Reflection.Assembly MainAssembly;
        protected ResourceManager() { }
        public ResourceManager(string baseName, System.Reflection.Assembly assembly) { }
        public ResourceManager(string baseName, System.Reflection.Assembly assembly, System.Type usingResourceSet) { }
        public ResourceManager(System.Type resourceSource) { }
        public virtual string BaseName { get { throw null; } }
        public virtual bool IgnoreCase { get { throw null; } set { } }
        public virtual System.Type ResourceSetType { get { throw null; } }
        protected static System.Globalization.CultureInfo GetNeutralResourcesLanguage(System.Reflection.Assembly a) { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, System.Globalization.CultureInfo culture) { throw null; }
        protected virtual string GetResourceFileName(System.Globalization.CultureInfo culture) { throw null; }
        public virtual ResourceSet GetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        protected static System.Version GetSatelliteContractVersion(System.Reflection.Assembly a) { throw null; }
        public System.IO.UnmanagedMemoryStream GetStream(string name) { throw null; }
        public System.IO.UnmanagedMemoryStream GetStream(string name, System.Globalization.CultureInfo culture) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, System.Globalization.CultureInfo culture) { throw null; }
        protected virtual ResourceSet InternalGetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        public virtual void ReleaseAllResources() { }
        protected UltimateResourceFallbackLocation FallbackLocation { get { throw null; } set { } }
        public static ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir, System.Type usingResourceSet) { throw null; }    
    }
    public class ResourceSet : System.IDisposable, System.Collections.IEnumerable
    {
        protected ResourceSet() { }
        public ResourceSet(System.IO.Stream stream) { }
        public ResourceSet(string fileName) { }
        public ResourceSet(IResourceReader reader) { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual Type GetDefaultReader() { throw null; }
        public virtual Type GetDefaultWriter() { throw null; }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, bool ignoreCase) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, bool ignoreCase) { throw null; }
        protected virtual void ReadResources() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed class SatelliteContractVersionAttribute : System.Attribute
    {
        public SatelliteContractVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly,
        Satellite
    }
}
