// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum MediaPermissionAudio
    {
        NoAudio,
        SiteOfOriginAudio,
        SafeAudio,
        AllAudio
    }

#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum MediaPermissionVideo
    {
        NoVideo,
        SiteOfOriginVideo,
        SafeVideo,
        AllVideo,
    }

#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public enum MediaPermissionImage
    {
        NoImage,
        SiteOfOriginImage,
        SafeImage,
        AllImage,
    }

#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class MediaPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public MediaPermission() { }
        public MediaPermission(PermissionState state) { }
        public MediaPermission(MediaPermissionAudio permissionAudio) { }
        public MediaPermission(MediaPermissionVideo permissionVideo) { }
        public MediaPermission(MediaPermissionImage permissionImage) { }
        public MediaPermission(MediaPermissionAudio permissionAudio,
                               MediaPermissionVideo permissionVideo,
                               MediaPermissionImage permissionImage)
        { }
        public bool IsUnrestricted() { return true; }
        public override bool IsSubsetOf(IPermission target) { return true; }
        public override IPermission Intersect(IPermission target) { return new MediaPermission(); }
        public override IPermission Union(IPermission target) { return new MediaPermission(); }
        public override IPermission Copy() { return new MediaPermission(); }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override void FromXml(SecurityElement securityElement) { }
        public MediaPermissionAudio Audio { get { return MediaPermissionAudio.AllAudio; } }
        public MediaPermissionVideo Video { get { return MediaPermissionVideo.AllVideo; } }
        public MediaPermissionImage Image { get { return MediaPermissionImage.AllImage; } }
    }

#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class MediaPermissionAttribute : CodeAccessSecurityAttribute
    {
        public MediaPermissionAttribute(SecurityAction action) : base(action) { }
        public override IPermission CreatePermission() { return new MediaPermission(); }
        public MediaPermissionAudio Audio { get { return MediaPermissionAudio.AllAudio; } set { } }
        public MediaPermissionVideo Video { get { return MediaPermissionVideo.AllVideo; } set { } }
        public MediaPermissionImage Image { get { return MediaPermissionImage.AllImage; } set { } }
    }
}
