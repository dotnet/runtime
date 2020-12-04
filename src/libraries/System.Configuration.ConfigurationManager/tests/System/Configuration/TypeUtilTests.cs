// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Internal;
using System.IO;
using System.Security;
using Xunit;

namespace System.ConfigurationTests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Unit test for core implementation")]
    public class TypeUtilTests
    {
        [Theory,
            // CoreLib
            InlineData("System.String", typeof(string)),
            InlineData("System.Int32", typeof(int)),
            // System.Configuration
            InlineData("System.Configuration.PropertyInformation", typeof(PropertyInformation)),
            // System.Runtime, previously System
            InlineData("System.Uri", typeof(Uri)),
            // System.Collections, previously System
            InlineData("System.Collections.Generic.LinkedList`1", typeof(LinkedList<>)),
            InlineData("System.Collections.Generic.Queue`1", typeof(Queue<>)),
            InlineData("System.Collections.Generic.Stack`1", typeof(Stack<>)),
            // System.Collections.Concurrent, previously System
            InlineData("System.Collections.Concurrent.ConcurrentBag`1", typeof(ConcurrentBag<>)),
            InlineData("System.Collections.Concurrent.BlockingCollection`1", typeof(BlockingCollection<>)),
            // System.Collections.Specialized, previously System
            InlineData("System.Collections.Specialized.HybridDictionary", typeof(HybridDictionary)),
            InlineData("System.Collections.Specialized.ListDictionary", typeof(ListDictionary)),
            InlineData("System.Collections.Specialized.StringDictionary", typeof(StringDictionary)),
            InlineData("System.Collections.Specialized.OrderedDictionary", typeof(OrderedDictionary)),
            InlineData("System.Collections.Specialized.StringCollection", typeof(StringCollection)),
            InlineData("System.Collections.Specialized.NameValueCollection", typeof(NameValueCollection)),
            ]
        public void GetType_NoAssemblyQualifcation(string typeString, Type expectedType)
        {
            Assert.Equal(expectedType, TypeUtil.GetType(typeString, throwOnError: false));
        }


        [Theory,
            // ConfigurationManager types roll forward
            // ConfigurationManager isn't part of the shared framework and potentially app-local.
            // https://github.com/dotnet/runtime/issues/12376#issuecomment-479670104 explains why testing the type roll forward behavior doesn't work in such cases.
            InlineData(
                "System.Configuration.UserSettingsGroup, System.Configuration.ConfigurationManager, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                typeof(UserSettingsGroup))
            ]
        public void GetType_ConfigurationManagerTypes(string typeString, Type expectedType)
        {
            Assert.Equal(expectedType, TypeUtil.GetType(typeString, throwOnError: false));
        }

        [Fact]
        public void GetType_ThrowOnError()
        {
            Assert.Throws<TypeLoadException>(() => TypeUtil.GetType("Mxyzptlk", throwOnError: true));
        }

        [Fact]
        public void GetType_NoThrowOnError()
        {
            Assert.Null(TypeUtil.GetType("Mxyzptlk", throwOnError: false));
        }

        [Fact]
        public void GetTypeConfigHost()
        {
            TestHost host = new TestHost((s, b) => { return typeof(string); });
            Assert.Equal(typeof(string), TypeUtil.GetType(host, "Mxyzptlk", throwOnError: false));
        }

        [Fact]
        public void GetTypeConfigHost_ThrowOnError()
        {
            TestHost host = new TestHost((s, b) => { if (b) throw new ArgumentException(); return null; });
            AssertExtensions.Throws<ArgumentException>(null, () => TypeUtil.GetType(host, "Mxyzptlk", throwOnError: true));
        }

        [Fact]
        public void GetTypeConfigHost_NoThrowOnError()
        {
            TestHost host = new TestHost((s, b) => { if (b) throw new ArgumentException(); return null; });
            Assert.Null(TypeUtil.GetType(host, "Mxyzptlk", throwOnError: false));
        }

        private class TestHost : IInternalConfigHost
        {
            Func<string, bool, Type> _configTypeFunc;

            public TestHost(Func<string, bool, Type> configTypeFunc)
            {
                _configTypeFunc = configTypeFunc;
            }

            Type IInternalConfigHost.GetConfigType(string typeName, bool throwOnError)
            {
                return _configTypeFunc(typeName, throwOnError);
            }

            bool IInternalConfigHost.IsRemote
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool IInternalConfigHost.SupportsChangeNotifications
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool IInternalConfigHost.SupportsLocation
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool IInternalConfigHost.SupportsPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool IInternalConfigHost.SupportsRefresh
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            object IInternalConfigHost.CreateConfigurationContext(string configPath, string locationSubPath)
            {
                throw new NotImplementedException();
            }

            object IInternalConfigHost.CreateDeprecatedConfigContext(string configPath)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.DecryptSection(string encryptedXml, ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfigSection)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.DeleteStream(string streamName)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.EncryptSection(string clearTextXml, ProtectedConfigurationProvider protectionProvider, ProtectedConfigurationSection protectedConfigSection)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.GetConfigPathFromLocationSubPath(string configPath, string locationSubPath)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.GetConfigTypeName(Type t)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.GetStreamName(string configPath)
            {
                throw new NotImplementedException();
            }

            string IInternalConfigHost.GetStreamNameForConfigSource(string streamName, string configSource)
            {
                throw new NotImplementedException();
            }

            object IInternalConfigHost.GetStreamVersion(string streamName)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath, IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsAboveApplication(string configPath)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsConfigRecordRequired(string configPath)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition, ConfigurationAllowExeDefinition allowExeDefinition)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsFile(string streamName)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsInitDelayed(IInternalConfigRecord configRecord)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsLocationApplicable(string configPath)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsSecondaryRoot(string configPath)
            {
                throw new NotImplementedException();
            }

            Stream IInternalConfigHost.OpenStreamForRead(string streamName)
            {
                throw new NotImplementedException();
            }

            Stream IInternalConfigHost.OpenStreamForRead(string streamName, bool assertPermissions)
            {
                throw new NotImplementedException();
            }

            Stream IInternalConfigHost.OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
            {
                throw new NotImplementedException();
            }

            Stream IInternalConfigHost.OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext, bool assertPermissions)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.PrefetchAll(string configPath, string streamName)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.PrefetchSection(string sectionGroupName, string sectionName)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.RequireCompleteInit(IInternalConfigRecord configRecord)
            {
                throw new NotImplementedException();
            }

            object IInternalConfigHost.StartMonitoringStreamForChanges(string streamName, StreamChangeCallback callback)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.StopMonitoringStreamForChanges(string streamName, StreamChangeCallback callback)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.VerifyDefinitionAllowed(string configPath, ConfigurationAllowDefinition allowDefinition, ConfigurationAllowExeDefinition allowExeDefinition, IConfigErrorInfo errorInfo)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.WriteCompleted(string streamName, bool success, object writeContext)
            {
                throw new NotImplementedException();
            }

            void IInternalConfigHost.WriteCompleted(string streamName, bool success, object writeContext, bool assertPermissions)
            {
                throw new NotImplementedException();
            }

            IDisposable IInternalConfigHost.Impersonate()
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsFullTrustSectionWithoutAptcaAllowed(IInternalConfigRecord configRecord)
            {
                throw new NotImplementedException();
            }

            bool IInternalConfigHost.IsTrustedConfigPath(string configPath)
            {
                throw new NotImplementedException();
            }

#pragma warning disable SYSLIB0003 // Obsolete: CAS
            void IInternalConfigHost.GetRestrictedPermissions(IInternalConfigRecord configRecord, out PermissionSet permissionSet, out bool isHostReady)
            {
                throw new NotImplementedException();
            }
#pragma warning restore SYSLIB0003 // Obsolete: CAS
        }
    }
}
