// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Configuration.Internal
{
    internal sealed class InternalConfigHost : IInternalConfigHost
    {
        private const FileAttributes InvalidAttributesForWrite = FileAttributes.ReadOnly | FileAttributes.Hidden;

        void IInternalConfigHost.Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
        { }

        void IInternalConfigHost.InitForConfiguration(ref string locationSubPath, out string configPath,
            out string locationConfigPath,
            IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
        {
            configPath = null;
            locationConfigPath = null;
        }

        bool IInternalConfigHost.IsConfigRecordRequired(string configPath)
        {
            return true;
        }

        bool IInternalConfigHost.IsInitDelayed(IInternalConfigRecord configRecord)
        {
            return false;
        }

        void IInternalConfigHost.RequireCompleteInit(IInternalConfigRecord configRecord) { }

        public bool IsSecondaryRoot(string configPath)
        {
            // In the default there are no secondary root's
            return false;
        }

        string IInternalConfigHost.GetStreamName(string configPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.GetStreamName");
        }

        string IInternalConfigHost.GetStreamNameForConfigSource(string streamName, string configSource)
        {
            return StaticGetStreamNameForConfigSource(streamName, configSource);
        }

        object IInternalConfigHost.GetStreamVersion(string streamName)
        {
            return StaticGetStreamVersion(streamName);
        }

        Stream IInternalConfigHost.OpenStreamForRead(string streamName)
        {
            return StaticOpenStreamForRead(streamName);
        }

        Stream IInternalConfigHost.OpenStreamForRead(string streamName, bool assertPermissions)
        {
            return StaticOpenStreamForRead(streamName);
        }

        Stream IInternalConfigHost.OpenStreamForWrite(string streamName, string templateStreamName,
            ref object writeContext)
        {
            return StaticOpenStreamForWrite(streamName, templateStreamName, ref writeContext);
        }

        Stream IInternalConfigHost.OpenStreamForWrite(string streamName, string templateStreamName,
            ref object writeContext, bool assertPermissions)
        {
            return StaticOpenStreamForWrite(streamName, templateStreamName, ref writeContext);
        }

        void IInternalConfigHost.WriteCompleted(string streamName, bool success, object writeContext)
        {
            StaticWriteCompleted(streamName, success, writeContext);
        }

        void IInternalConfigHost.WriteCompleted(string streamName, bool success, object writeContext,
            bool assertPermissions)
        {
            StaticWriteCompleted(streamName, success, writeContext);
        }

        void IInternalConfigHost.DeleteStream(string streamName)
        {
            StaticDeleteStream(streamName);
        }

        bool IInternalConfigHost.IsFile(string streamName)
        {
            return StaticIsFile(streamName);
        }

        bool IInternalConfigHost.SupportsChangeNotifications => false;

        object IInternalConfigHost.StartMonitoringStreamForChanges(string streamName, StreamChangeCallback callback)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.StartMonitoringStreamForChanges");
        }

        void IInternalConfigHost.StopMonitoringStreamForChanges(string streamName, StreamChangeCallback callback)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.StopMonitoringStreamForChanges");
        }

        bool IInternalConfigHost.SupportsRefresh => false;

        bool IInternalConfigHost.SupportsPath => false;

        bool IInternalConfigHost.IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition)
        {
            return true;
        }

        void IInternalConfigHost.VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo)
        { }

        bool IInternalConfigHost.SupportsLocation => false;

        bool IInternalConfigHost.IsAboveApplication(string configPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.IsAboveApplication");
        }

        string IInternalConfigHost.GetConfigPathFromLocationSubPath(string configPath, string locationSubPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.GetConfigPathFromLocationSubPath");
        }

        bool IInternalConfigHost.IsLocationApplicable(string configPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.IsLocationApplicable");
        }

        bool IInternalConfigHost.PrefetchAll(string configPath, string streamName)
        {
            return false;
        }

        bool IInternalConfigHost.PrefetchSection(string sectionGroupName, string sectionName)
        {
            return false;
        }

        object IInternalConfigHost.CreateDeprecatedConfigContext(string configPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.CreateDeprecatedConfigContext");
        }

        object IInternalConfigHost.CreateConfigurationContext(string configPath, string locationSubPath)
        {
            throw ExceptionUtil.UnexpectedError("IInternalConfigHost.CreateConfigurationContext");
        }

        string IInternalConfigHost.DecryptSection(string encryptedXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection)
        {
            return ProtectedConfigurationSection.DecryptSection(encryptedXml, protectionProvider);
        }

        string IInternalConfigHost.EncryptSection(string clearTextXml, ProtectedConfigurationProvider protectionProvider,
            ProtectedConfigurationSection protectedConfigSection)
        {
            return ProtectedConfigurationSection.EncryptSection(clearTextXml, protectionProvider);
        }

        Type IInternalConfigHost.GetConfigType(string typeName, bool throwOnError)
        {
            return Type.GetType(typeName, throwOnError);
        }

        string IInternalConfigHost.GetConfigTypeName(Type t)
        {
            return t.AssemblyQualifiedName;
        }

        bool IInternalConfigHost.IsRemote => false;

        internal static string StaticGetStreamNameForConfigSource(string streamName, string configSource)
        {
            // RemoteWebConfigurationHost also redirects GetStreamNameForConfigSource to this
            // method, and that means streamName is referring to a path that's on the remote
            // machine.

            // don't allow relative paths for stream name
            if (!Path.IsPathRooted(streamName)) throw ExceptionUtil.ParameterInvalid(nameof(streamName));

            // get the path part of the original stream
            streamName = Path.GetFullPath(streamName);
            string dirStream = UrlPath.GetDirectoryOrRootName(streamName);

            // combine with the new config source
            string result = Path.Combine(dirStream, configSource);
            result = Path.GetFullPath(result);

            // ensure the result is in or under the directory of the original source
            string dirResult = UrlPath.GetDirectoryOrRootName(result);
            if (!UrlPath.IsEqualOrSubdirectory(dirStream, dirResult))
                throw new ArgumentException(string.Format(SR.Config_source_not_under_config_dir, configSource));

            return result;
        }

        internal static FileVersion StaticGetStreamVersion(string streamName)
        {
            FileInfo info = new FileInfo(streamName);
            return info.Exists
                ? new FileVersion(true, info.Length, info.CreationTimeUtc, info.LastWriteTimeUtc)
                : new FileVersion(false, 0, DateTime.MinValue, DateTime.MinValue);
        }

        // default impl treats name as a file name
        // null means stream doesn't exist for this name
        internal static Stream StaticOpenStreamForRead(string streamName)
        {
            if (string.IsNullOrEmpty(streamName))
                throw ExceptionUtil.UnexpectedError("InternalConfigHost::StaticOpenStreamForRead");

            return !File.Exists(streamName)
                ? null
                : new FileStream(streamName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // This method doesn't really open the streamName for write.  Instead, using WriteFileContext
        // it opens a stream on a temporary file created in the same directory as streamName.
        internal static Stream StaticOpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ConfigurationErrorsException(SR.Config_no_stream_to_write);

            // Create directory if it does not exist.
            // Ignore errors, allow any failure to come when trying to open the file.
            string dir = Path.GetDirectoryName(streamName);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Stream stream;
            WriteFileContext writeFileContext = null;

            try
            {
                writeFileContext = new WriteFileContext(streamName, templateStreamName);

                if (File.Exists(streamName))
                {
                    FileInfo fi = new FileInfo(streamName);
                    FileAttributes attrs = fi.Attributes;
                    if ((attrs & InvalidAttributesForWrite) != 0)
                    {
                        throw new IOException(string.Format(SR.Config_invalid_attributes_for_write, streamName));
                    }
                }

                try
                {
                    stream = new FileStream(writeFileContext.TempNewFilename, FileMode.Create, FileAccess.Write, FileShare.Read);
                }
                catch (Exception e)
                {
                    // Wrap all exceptions so that we provide a meaningful filename - otherwise the end user
                    // will just see the temporary file name, which is meaningless.

                    throw new ConfigurationErrorsException(string.Format(SR.Config_write_failed, streamName), e);
                }
            }
            catch
            {
                writeFileContext?.Complete(streamName, false);
                throw;
            }

            writeContext = writeFileContext;
            return stream;

        }

        internal static void StaticWriteCompleted(string streamName, bool success, object writeContext)
        {
            ((WriteFileContext)writeContext).Complete(streamName, success);
        }

        internal static void StaticDeleteStream(string streamName)
        {
            File.Delete(streamName);
        }

        internal static bool StaticIsFile(string streamName)
        {
            return Path.IsPathRooted(streamName);
        }

        public bool IsTrustedConfigPath(string configPath) => true;

        public bool IsFullTrustSectionWithoutAptcaAllowed(IInternalConfigRecord configRecord) => true;

        public IDisposable Impersonate() => new DummyDisposable();
    }
}