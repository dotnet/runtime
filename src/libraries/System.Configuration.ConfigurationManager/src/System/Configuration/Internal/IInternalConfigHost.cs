// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Configuration.Internal
{
    public interface IInternalConfigHost
    {
        // change notification support - runtime only
        bool SupportsChangeNotifications { get; }

        // RefreshConfig support - runtime only
        bool SupportsRefresh { get; }

        // Whether we support Path attribute in location.
        bool SupportsPath { get; }

        bool SupportsLocation { get; }

        // Used by MgmtConfigurationRecord during SaveAs
        bool IsRemote { get; }

        void Init(IInternalConfigRoot configRoot, params object[] hostInitParams);

        void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath,
            IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams);

        // To support creation of new config record - whether that path requires a configRecord.
        bool IsConfigRecordRequired(string configPath);

        bool IsInitDelayed(IInternalConfigRecord configRecord);

        void RequireCompleteInit(IInternalConfigRecord configRecord);

        bool IsSecondaryRoot(string configPath);

        string GetStreamName(string configPath);

        string GetStreamNameForConfigSource(string streamName, string configSource);

        object GetStreamVersion(string streamName);

        // default impl treats name as a file name
        // null means stream doesn't exist for this name
        Stream OpenStreamForRead(string streamName);

        Stream OpenStreamForRead(string streamName, bool assertPermissions);

        Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext);

        Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext,
            bool assertPermissions);

        void WriteCompleted(string streamName, bool success, object writeContext);

        void WriteCompleted(string streamName, bool success, object writeContext, bool assertPermissions);

        void DeleteStream(string streamName);

        bool IsFile(string streamName);

        object StartMonitoringStreamForChanges(string streamName, StreamChangeCallback callback);

        void StopMonitoringStreamForChanges(string streamName, StreamChangeCallback callback);

        bool IsAboveApplication(string configPath);

        string GetConfigPathFromLocationSubPath(string configPath, string locationSubPath);

        bool IsLocationApplicable(string configPath);

        bool IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition);

        void VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo);

        bool PrefetchAll(string configPath, string streamName);

        // E.g. If the config file is downloaded from HTTP, we want to prefetch everything.
        bool PrefetchSection(string sectionGroupName, string sectionName);

        object CreateDeprecatedConfigContext(string configPath);

        object CreateConfigurationContext(string configPath, string locationSubPath);

        string DecryptSection(string encryptedXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection);

        string EncryptSection(string clearTextXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection);

        Type GetConfigType(string typeName, bool throwOnError);

        string GetConfigTypeName(Type t);

        bool IsTrustedConfigPath(string configPath);

        bool IsFullTrustSectionWithoutAptcaAllowed(IInternalConfigRecord configRecord);

        IDisposable Impersonate();

#pragma warning disable SYSLIB0003 // Obsolete: CAS
        void GetRestrictedPermissions(IInternalConfigRecord configRecord, out PermissionSet permissionSet, out bool isHostReady);
#pragma warning restore SYSLIB0003 // Obsolete: CAS
    }
}
