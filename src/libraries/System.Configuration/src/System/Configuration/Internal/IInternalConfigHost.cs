// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    [ComVisible(false)]
    public interface IInternalConfigHost
    {
        // change notification support - runtime only
        bool SupportsChangeNotifications { get; }

        // RefreshConfig support - runtime only
        bool SupportsRefresh { get; }

        // path support: whether we support Path attribute in location.
        bool SupportsPath { get; }

        // location support
        bool SupportsLocation { get; }

        // Remote support
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

        // stream support
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

        // ConfigurationErrorsException support
        bool IsFile(string streamName);
        object StartMonitoringStreamForChanges(string streamName, StreamChangeCallback callback);
        void StopMonitoringStreamForChanges(string streamName, StreamChangeCallback callback);
        bool IsAboveApplication(string configPath);
        string GetConfigPathFromLocationSubPath(string configPath, string locationSubPath);
        bool IsLocationApplicable(string configPath);

        // definition support
        bool IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition);

        void VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo);

        // prefetch support
        bool PrefetchAll(string configPath, string streamName);
        // E.g. If the config file is downloaded from HTTP, we want to prefetch everything.
        bool PrefetchSection(string sectionGroupName, string sectionName);

        // context support
        object CreateDeprecatedConfigContext(string configPath);
        object CreateConfigurationContext(string configPath, string locationSubPath);

        // Encrypt/decrypt support 
        string DecryptSection(string encryptedXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection);

        string EncryptSection(string clearTextXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection);

        // Type name support
        // E.g. to support type defined in app_code
        Type GetConfigType(string typeName, bool throwOnError);
        string GetConfigTypeName(Type t);
    }
}