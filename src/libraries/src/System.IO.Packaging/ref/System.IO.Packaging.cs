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
        public System.Uri SourceUri { get { throw null; } }
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
        public System.IO.FileAccess FileOpenAccess { get { throw null; } }
        public System.IO.Packaging.PackageProperties PackageProperties { get { throw null; } }
        public void Close() { }
        public System.IO.Packaging.PackagePart CreatePart(System.Uri partUri, string contentType) { throw null; }
        public System.IO.Packaging.PackagePart CreatePart(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { throw null; }
        protected abstract System.IO.Packaging.PackagePart CreatePartCore(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption);
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType) { throw null; }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType, string id) { throw null; }
        public void DeletePart(System.Uri partUri) { }
        protected abstract void DeletePartCore(System.Uri partUri);
        public void DeleteRelationship(string id) { }
        protected virtual void Dispose(bool disposing) { }
        public void Flush() { }
        protected abstract void FlushCore();
        public System.IO.Packaging.PackagePart GetPart(System.Uri partUri) { throw null; }
        protected abstract System.IO.Packaging.PackagePart GetPartCore(System.Uri partUri);
        public System.IO.Packaging.PackagePartCollection GetParts() { throw null; }
        protected abstract System.IO.Packaging.PackagePart[] GetPartsCore();
        public System.IO.Packaging.PackageRelationship GetRelationship(string id) { throw null; }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationships() { throw null; }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationshipsByType(string relationshipType) { throw null; }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream) { throw null; }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream, System.IO.FileMode packageMode) { throw null; }
        public static System.IO.Packaging.Package Open(System.IO.Stream stream, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess) { throw null; }
        public static System.IO.Packaging.Package Open(string path) { throw null; }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode) { throw null; }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess) { throw null; }
        public static System.IO.Packaging.Package Open(string path, System.IO.FileMode packageMode, System.IO.FileAccess packageAccess, System.IO.FileShare packageShare) { throw null; }
        public virtual bool PartExists(System.Uri partUri) { throw null; }
        public bool RelationshipExists(string id) { throw null; }
        void System.IDisposable.Dispose() { }
    }
    public abstract partial class PackagePart
    {
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri) { }
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri, string contentType) { }
        protected PackagePart(System.IO.Packaging.Package package, System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { }
        public System.IO.Packaging.CompressionOption CompressionOption { get { throw null; } }
        public string ContentType { get { throw null; } }
        public System.IO.Packaging.Package Package { get { throw null; } }
        public System.Uri Uri { get { throw null; } }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType) { throw null; }
        public System.IO.Packaging.PackageRelationship CreateRelationship(System.Uri targetUri, System.IO.Packaging.TargetMode targetMode, string relationshipType, string id) { throw null; }
        public void DeleteRelationship(string id) { }
        protected virtual string GetContentTypeCore() { throw null; }
        public System.IO.Packaging.PackageRelationship GetRelationship(string id) { throw null; }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationships() { throw null; }
        public System.IO.Packaging.PackageRelationshipCollection GetRelationshipsByType(string relationshipType) { throw null; }
        public System.IO.Stream GetStream() { throw null; }
        public System.IO.Stream GetStream(System.IO.FileMode mode) { throw null; }
        public System.IO.Stream GetStream(System.IO.FileMode mode, System.IO.FileAccess access) { throw null; }
        protected abstract System.IO.Stream GetStreamCore(System.IO.FileMode mode, System.IO.FileAccess access);
        public bool RelationshipExists(string id) { throw null; }
    }
    public partial class PackagePartCollection : System.Collections.Generic.IEnumerable<System.IO.Packaging.PackagePart>, System.Collections.IEnumerable
    {
        internal PackagePartCollection() { }
        public System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart> GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.IO.Packaging.PackagePart> System.Collections.Generic.IEnumerable<System.IO.Packaging.PackagePart>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
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
        public string Id { get { throw null; } }
        public System.IO.Packaging.Package Package { get { throw null; } }
        public string RelationshipType { get { throw null; } }
        public System.Uri SourceUri { get { throw null; } }
        public System.IO.Packaging.TargetMode TargetMode { get { throw null; } }
        public System.Uri TargetUri { get { throw null; } }
    }
    public partial class PackageRelationshipCollection : System.Collections.Generic.IEnumerable<System.IO.Packaging.PackageRelationship>, System.Collections.IEnumerable
    {
        internal PackageRelationshipCollection() { }
        public System.Collections.Generic.IEnumerator<System.IO.Packaging.PackageRelationship> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class PackageRelationshipSelector
    {
        public PackageRelationshipSelector(System.Uri sourceUri, System.IO.Packaging.PackageRelationshipSelectorType selectorType, string selectionCriteria) { }
        public string SelectionCriteria { get { throw null; } }
        public System.IO.Packaging.PackageRelationshipSelectorType SelectorType { get { throw null; } }
        public System.Uri SourceUri { get { throw null; } }
        public System.Collections.Generic.List<System.IO.Packaging.PackageRelationship> Select(System.IO.Packaging.Package package) { throw null; }
    }
    public enum PackageRelationshipSelectorType
    {
        Id = 0,
        Type = 1,
    }
    public static partial class PackUriHelper
    {
        public static readonly string UriSchemePack;
        public static int ComparePartUri(System.Uri firstPartUri, System.Uri secondPartUri) { throw null; }
        public static System.Uri CreatePartUri(System.Uri partUri) { throw null; }
        public static System.Uri GetNormalizedPartUri(System.Uri partUri) { throw null; }
        public static System.Uri GetRelationshipPartUri(System.Uri partUri) { throw null; }
        public static System.Uri GetRelativeUri(System.Uri sourcePartUri, System.Uri targetPartUri) { throw null; }
        public static System.Uri GetSourcePartUriFromRelationshipPartUri(System.Uri relationshipPartUri) { throw null; }
        public static bool IsRelationshipPartUri(System.Uri partUri) { throw null; }
        public static System.Uri ResolvePartUri(System.Uri sourcePartUri, System.Uri targetUri) { throw null; }
    }
    public enum TargetMode
    {
        External = 1,
        Internal = 0,
    }
    public sealed partial class ZipPackage : System.IO.Packaging.Package
    {
        internal ZipPackage() : base (default(System.IO.FileAccess)) { }
        protected override System.IO.Packaging.PackagePart CreatePartCore(System.Uri partUri, string contentType, System.IO.Packaging.CompressionOption compressionOption) { throw null; }
        protected override void DeletePartCore(System.Uri partUri) { }
        protected override void Dispose(bool disposing) { }
        protected override void FlushCore() { }
        protected override System.IO.Packaging.PackagePart GetPartCore(System.Uri partUri) { throw null; }
        protected override System.IO.Packaging.PackagePart[] GetPartsCore() { throw null; }
    }
    public sealed partial class ZipPackagePart : System.IO.Packaging.PackagePart
    {
        internal ZipPackagePart() : base (default(System.IO.Packaging.Package), default(System.Uri)) { }
        protected override System.IO.Stream GetStreamCore(System.IO.FileMode streamFileMode, System.IO.FileAccess streamFileAccess) { throw null; }
    }
}
