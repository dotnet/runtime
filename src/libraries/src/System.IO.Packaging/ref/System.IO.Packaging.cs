// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public partial class FileFormatException : System.FormatException
    {
        public FileFormatException() { }
        public FileFormatException(string message) { }
        public FileFormatException(string message, System.Exception innerException) { }
        public FileFormatException(System.Uri sourceUri) { }
        public FileFormatException(System.Uri sourceUri, System.Exception innerException) { }
        public FileFormatException(System.Uri sourceUri, string message) { }
        public FileFormatException(System.Uri sourceUri, string message, System.Exception innerException) { }
        public System.Uri SourceUri { get { return default(System.Uri); } }
    }
}
namespace System.IO.Packaging
{
    public enum CompressionOption
    {
        Fast = 2,
        Maximum = 1,
        Normal = 0,
        NotCompressed = -1,
        SuperFast = 3,
    }
    public enum EncryptionOption
    {
        None = 0,
        RightsManagement = 1,
    }
    public abstract partial class Package : System.IDisposable
    {
        protected Package(System.IO.FileAccess openFileAccess) { }
        public System.IO.FileAccess FileOpenAccess { get { return default(System.IO.FileAccess); } }
        public System.IO.Packaging.PackageProperties PackageProperties { get { return default(System.IO.Packaging.PackageProperties); } }
        public void Close() { }
        public System.IO.Packaging.PackagePart CreatePart(System.Uri partUri, string contentType) { return default(System.IO.Packaging.PackagePart); }
        public System.IO.Packaging.PackagePart CreatePart(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { return default(System.IO.Packaging.PackagePart); }
        protected abstract System.IO.Packaging.PackagePart CreatePartCore(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption);
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType) { return default(System.IO.Packaging.PackageRelationship); }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType, string id) { return default(System.IO.Packaging.PackageRelationship); }
        public void DeletePart(System.Uri partUri) { }
        protected abstract void DeletePartCore(System.Uri partUri);
        public void DeleteRelationship(string id) { }
        protected virtual void Dispose(bool disposing) { }
        public void Flush() { }
        protected abstract void FlushCore();
        public System.IO.Packaging.PackagePart GetPart(System.Uri partUri) { return default(System.IO.Packaging.PackagePart); }
        protected abstract System.IO.Packaging.PackagePart GetPartCore(System.Uri partUri);
        public System.IO.Packaging.PackagePartCollection GetParts() { return default(System.IO.Packaging.PackagePartCollection); }
        protected abstract System.IO.Packaging.PackagePart[] GetPartsCore();
        public System.IO.Packaging.PackageRelationship GetRelationship(string id) { return default(System.IO.Packaging.PackageRelationship); }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationships() { return default(System.IO.Packaging.PackageRelationshipCollection); }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationshipsByType(string relationshipType) { return default(System.IO.Packaging.PackageRelationshipCollection); }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream, System.IO.FileMode packageMode) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(string path) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess) { return default(System.IO.Packaging.Package); }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess, System.IO.FileShare packageShare) { return default(System.IO.Packaging.Package); }
        public virtual bool PartExists(System.Uri partUri) { return default(bool); }
        public bool RelationshipExists(string id) { return default(bool); }
        void System.IDisposable.Dispose() { }
    }
    public abstract partial class PackagePart
    {
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri) { }
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri, string contentType) { }
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { }
        public System.IO.Packaging.CompressionOption CompressionOption { get { return default(System.IO.Packaging.CompressionOption); } }
        public string ContentType { get { return default(string); } }
        public System.IO.Packaging.Package Package { get { return default(System.IO.Packaging.Package); } }
        public System.Uri Uri { get { return default(System.Uri); } }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType) { return default(System.IO.Packaging.PackageRelationship); }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType, string id) { return default(System.IO.Packaging.PackageRelationship); }
        public void DeleteRelationship(string id) { }
        protected virtual string GetContentTypeCore() { return default(string); }
        public System.IO.Packaging.PackageRelationship GetRelationship(string id) { return default(System.IO.Packaging.PackageRelationship); }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationships() { return default(System.IO.Packaging.PackageRelationshipCollection); }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationshipsByType(string relationshipType) { return default(System.IO.Packaging.PackageRelationshipCollection); }
        public System.IO.Stream GetStream() { return default(System.IO.Stream); }
        public System.IO.Stream GetStream(System.IO.FileMode mode) { return default(System.IO.Stream); }
        public System.IO.Stream GetStream(System.IO.FileMode mode, System.IO.FileAccess access) { return default(System.IO.Stream); }
        protected abstract System.IO.Stream GetStreamCore(System.IO.FileMode mode, System.IO.FileAccess access);
        public bool RelationshipExists(string id) { return default(bool); }
    }
    public partial class PackagePartCollection : System.Collections.Generic.IEnumerable<System.IO.Packaging.PackagePart>, System.Collections.IEnumerable
    {
        internal PackagePartCollection() { }
        public System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart>); }
        System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart> System.Collections.Generic.IEnumerable<System.IO.Packaging.PackagePart>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public abstract partial class PackageProperties : System.IDisposable
    {
        protected PackageProperties() { }
        public abstract string Category { get; set; }
        public abstract string ContentStatus { get; set; }
        public abstract string ContentType { get; set; }
        public abstract System.Nullable<System.DateTime> Created { get; set; }
        public abstract string Creator { get; set; }
        public abstract string Description { get; set; }
        public abstract string Identifier { get; set; }
        public abstract string Keywords { get; set; }
        public abstract string Language { get; set; }
        public abstract string LastModifiedBy { get; set; }
        public abstract System.Nullable<System.DateTime> LastPrinted { get; set; }
        public abstract System.Nullable<System.DateTime> Modified { get; set; }
        public abstract string Revision { get; set; }
        public abstract string Subject { get; set; }
        public abstract string Title { get; set; }
        public abstract string Version { get; set; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class PackageRelationship
    {
        internal PackageRelationship() { }
        public string Id { get { return default(string); } }
        public System.IO.Packaging.Package Package { get { return default(System.IO.Packaging.Package); } }
        public string RelationshipType { get { return default(string); } }
        public System.Uri SourceUri { get { return default(System.Uri); } }
        public System.IO.Packaging.TargetMode TargetMode { get { return default(System.IO.Packaging.TargetMode); } }
        public System.Uri TargetUri { get { return default(System.Uri); } }
    }
    public partial class PackageRelationshipCollection : System.Collections.Generic.IEnumerable<System.IO.Packaging.PackageRelationship>, System.Collections.IEnumerable
    {
        internal PackageRelationshipCollection() { }
        public System.Collections.Generic.IEnumerator<System.IO.Packaging.PackageRelationship> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.IO.Packaging.PackageRelationship>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class PackageRelationshipSelector
    {
        public PackageRelationshipSelector(System.Uri sourceUri, System.IO.Packaging.PackageRelationshipSelectorType selectorType, string selectionCriteria) { }
        public string SelectionCriteria { get { return default(string); } }
        public System.IO.Packaging.PackageRelationshipSelectorType SelectorType { get { return default(System.IO.Packaging.PackageRelationshipSelectorType); } }
        public System.Uri SourceUri { get { return default(System.Uri); } }
        public System.Collections.Generic.List<System.IO.Packaging.PackageRelationship> Select(System.IO.Packaging.Package package) { return default(System.Collections.Generic.List<System.IO.Packaging.PackageRelationship>); }
    }
    public enum PackageRelationshipSelectorType
    {
        Id = 0,
        Type = 1,
    }
    public static partial class PackUriHelper
    {
        public static readonly string UriSchemePack;
        public static int ComparePackUri(System.Uri firstPackUri, System.Uri secondPackUri) { return default(int); }
        public static int ComparePartUri(System.Uri firstPartUri, System.Uri secondPartUri) { return default(int); }
        public static System.Uri Create(System.Uri packageUri) { return default(System.Uri); }
        public static System.Uri Create(System.Uri packageUri, System.Uri partUri) { return default(System.Uri); }
        public static System.Uri Create(System.Uri packageUri, System.Uri partUri, string fragment) { return default(System.Uri); }
        public static System.Uri CreatePartUri(System.Uri partUri) { return default(System.Uri); }
        public static System.Uri GetNormalizedPartUri(System.Uri partUri) { return default(System.Uri); }
        public static System.Uri GetPackageUri(System.Uri packUri) { return default(System.Uri); }
        public static System.Uri GetPartUri(System.Uri packUri) { return default(System.Uri); }
        public static System.Uri GetRelationshipPartUri(System.Uri partUri) { return default(System.Uri); }
        public static System.Uri GetRelativeUri(System.Uri sourcePartUri, System.Uri targetPartUri) { return default(System.Uri); }
        public static System.Uri GetSourcePartUriFromRelationshipPartUri(System.Uri relationshipPartUri) { return default(System.Uri); }
        public static bool IsRelationshipPartUri(System.Uri partUri) { return default(bool); }
        public static System.Uri ResolvePartUri(System.Uri sourcePartUri, System.Uri targetUri) { return default(System.Uri); }
    }
    public enum TargetMode
    {
        External = 1,
        Internal = 0,
    }
    public sealed partial class ZipPackage : System.IO.Packaging.Package
    {
        internal ZipPackage() : base (default(System.IO.FileAccess)) { }
        protected override System.IO.Packaging.PackagePart CreatePartCore(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { return default(System.IO.Packaging.PackagePart); }
        protected override void DeletePartCore(System.Uri partUri) { }
        protected override void Dispose(bool disposing) { }
        protected override void FlushCore() { }
        protected override System.IO.Packaging.PackagePart GetPartCore(System.Uri partUri) { return default(System.IO.Packaging.PackagePart); }
        protected override System.IO.Packaging.PackagePart[] GetPartsCore() { return default(System.IO.Packaging.PackagePart[]); }
    }
    public sealed partial class ZipPackagePart : System.IO.Packaging.PackagePart
    {
        internal ZipPackagePart() : base (default(System.IO.Packaging.Package), default(System.Uri)) { }
        protected override System.IO.Stream GetStreamCore(System.IO.FileMode streamFileMode, System.IO.FileAccess streamFileAccess) { return default(System.IO.Stream); }
    }
}
