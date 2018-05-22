// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.ActiveDirectory.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public class DomainControllerTests
    {
        [Fact]
        public void GetDomainController_NullContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.GetDomainController(null));
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.ConfigurationSet)]
        [InlineData(DirectoryContextType.Domain)]
        [InlineData(DirectoryContextType.Forest)]
        public void GetDomainController_InvalidContextType_ThrowsArgumentException(DirectoryContextType contextType)
        {
            var context = new DirectoryContext(contextType, "name");
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.GetDomainController(context));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData("\0")]
        [InlineData("[")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Access to path is denied when in App container")]
        public void GetDomainController_InvalidName(string name)
        {
            var context = new DirectoryContext(DirectoryContextType.DirectoryServer, name);
            Exception exception = Record.Exception(() => DomainController.GetDomainController(context));
            Assert.NotNull(exception);
            Assert.True(exception is ActiveDirectoryObjectNotFoundException ||
                        exception is ActiveDirectoryOperationException,
                        $"We got unrecognized exception {exception}");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Access to path is denied when in App container")]
        public void GetDomainController_InvalidIPV6()
        {
            var context = new DirectoryContext(DirectoryContextType.DirectoryServer, "[::1]:port");
            Exception exception = Record.Exception(() => DomainController.GetDomainController(context));
            Assert.NotNull(exception);
            Assert.True(exception is ActiveDirectoryObjectNotFoundException ||
                        exception is ActiveDirectoryOperationException,
                        $"We got unrecognized exception {exception}");
        }

        [Fact]
        public void FindOne_NullContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindOne(null));
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindOne(null, "siteName"));
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindOne(null, LocatorOptions.AvoidSelf));
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindOne(null, "siteName", LocatorOptions.AvoidSelf));
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.ConfigurationSet)]
        [InlineData(DirectoryContextType.DirectoryServer)]
        [InlineData(DirectoryContextType.Forest)]
        public void FindOne_InvalidContextType_ThrowsArgumentException(DirectoryContextType contextType)
        {
            var context = new DirectoryContext(contextType, "name");
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindOne(context));
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindOne(context, "siteName"));
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindOne(context, LocatorOptions.AvoidSelf));
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindOne(context, "siteName", LocatorOptions.AvoidSelf));
        }

        [Fact]
        public void FindOne_NullSiteName_ThrowsArgumentNullException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentNullException>("siteName", () => DomainController.FindOne(context, null));
            AssertExtensions.Throws<ArgumentNullException>("siteName", () => DomainController.FindOne(context, null, LocatorOptions.AvoidSelf));
        }

        [Fact]
        public void FindOne_EmptySiteName_ThrowsArgumentException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentException>("siteName", () => DomainController.FindOne(context, string.Empty));
            AssertExtensions.Throws<ArgumentException>("siteName", () => DomainController.FindOne(context, string.Empty, LocatorOptions.AvoidSelf));
        }

        [Theory]
        [InlineData((LocatorOptions)(-1))]
        [InlineData((LocatorOptions)int.MaxValue)]
        public void FindOne_InvalidFlag_ThrowsArgumentException(LocatorOptions flag)
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentException>("flag", () => DomainController.FindOne(context, flag));
            AssertExtensions.Throws<ArgumentException>("flag", () => DomainController.FindOne(context, "siteName", flag));
        }

        [Theory]
        [InlineData("\0", typeof(ActiveDirectoryObjectNotFoundException))]
        [InlineData("server:port", typeof(ActiveDirectoryOperationException))]
        public void FindOne_InvalidName_ThrowsException(string name, Type exceptionType)
        {
            var context = new DirectoryContext(DirectoryContextType.Domain, name);
            Assert.Throws(exceptionType, () => DomainController.FindOne(context, "siteName"));
            Assert.Throws(exceptionType, () => DomainController.FindOne(context, "siteName", LocatorOptions.AvoidSelf));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void FindAll_NoSuchName_ReturnsEmpty()
        {
            // Domain joined machines can have entries in the DomainController.
            if (PlatformDetection.IsDomainJoinedMachine)
            {
                var context = new DirectoryContext(DirectoryContextType.Domain, "\0");
                Assert.NotNull(DomainController.FindAll(context));
                Assert.NotNull(DomainController.FindAll(context, "siteName"));
            }
        }

        [Fact]
        [OuterLoop]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Getting information about domain is denied inside App")]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21553", TargetFrameworkMonikers.UapAot)]
        public void FindAll_NullName_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            if (!PlatformDetection.IsDomainJoinedMachine)
            {
                Assert.Throws<ActiveDirectoryOperationException>(() => DomainController.FindAll(context));
            }
        }

        [Fact]
        public void FindAll_NullContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindAll(null));
            AssertExtensions.Throws<ArgumentNullException>("context", () => DomainController.FindAll(null, "siteName"));
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.ConfigurationSet)]
        [InlineData(DirectoryContextType.DirectoryServer)]
        [InlineData(DirectoryContextType.Forest)]
        public void FindAll_InvalidContextType_ThrowsArgumentException(DirectoryContextType contextType)
        {
            var context = new DirectoryContext(contextType, "name");
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindAll(context));
            AssertExtensions.Throws<ArgumentException>("context", () => DomainController.FindAll(context, "siteName"));
        }

        [Fact]
        public void FindAll_NullSiteName_ThrowsArgumentNullException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentNullException>("siteName", () => DomainController.FindAll(context, null));
        }

        [Fact]
        public void FindAll_EmptySiteName_ThrowsArgumentException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentException>("siteName", () => DomainController.FindAll(context, string.Empty));
        }

        [Fact]
        public void FindAll_InvalidName_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain, "server:port");
            Assert.Throws<ActiveDirectoryOperationException>(() => DomainController.FindAll(context, "siteName"));
        }

        [Fact]
        public void CheckReplicationConsistency_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.CheckReplicationConsistency());
        }

        [Fact]
        public void CheckReplicationConsistency_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.CheckReplicationConsistency());
        }

        [Fact]
        public void CurrentTime_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.CurrentTime);
        }

        [Fact]
        public void Domain_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.Domain);
        }

        [Fact]
        public void Forest_GetWithNoContext_ThrowsActiveDirectoryObjectNotFoundException()
        {
            var controller = new SubController();
            Assert.Throws<ActiveDirectoryObjectNotFoundException>(() => controller.Forest);
        }

        [Fact]
        public void EnableGlobalCatalog_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.EnableGlobalCatalog());
        }

        [Fact]
        public void EnableGlobalCatalog_NoContext_ThrowsActiveDirectoryObjectNotFoundException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.EnableGlobalCatalog());
        }

        [Fact]
        public void GetAllReplicationNeighbors_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetAllReplicationNeighbors());
        }

        [Fact]
        public void GetAllReplicationNeighbors_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetAllReplicationNeighbors());
        }

        [Fact]
        public void GetDirectoryEntry_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetDirectoryEntry());
        }

        [Fact]
        public void GetDirectoryEntry_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetDirectoryEntry());
        }

        [Fact]
        public void GetDirectorySearcher_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetDirectorySearcher());
        }

        [Fact]
        public void GetReplicationConnectionFailures_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetReplicationConnectionFailures());
        }

        [Fact]
        public void GetReplicationConnectionFailures_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetReplicationConnectionFailures());
        }

        [Fact]
        public void GetReplicationCursors_NullPartition_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("partition", () => controller.GetReplicationCursors(null));
        }

        [Fact]
        public void GetReplicationCursors_EmptyPartition_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("partition", () => controller.GetReplicationCursors(string.Empty));
        }

        [Fact]
        public void GetReplicationCursors_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetReplicationCursors("partition"));
        }

        [Fact]
        public void GetReplicationCursors_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetReplicationCursors("partition"));
        }

        [Fact]
        public void GetReplicationMetadata_NullObjectPath_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("objectPath", () => controller.GetReplicationMetadata(null));
        }

        [Fact]
        public void GetReplicationMetadata_EmptyObjectPath_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("objectPath", () => controller.GetReplicationMetadata(string.Empty));
        }

        [Fact]
        public void GetReplicationMetadata_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetReplicationMetadata("objectPath"));
        }

        [Fact]
        public void GetReplicationMetadata_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetReplicationMetadata("objectPath"));
        }

        [Fact]
        public void GetReplicationNeighbors_NullPartition_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("partition", () => controller.GetReplicationNeighbors(null));
        }

        [Fact]
        public void GetReplicationNeighbors_EmptyPartition_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("partition", () => controller.GetReplicationNeighbors(string.Empty));
        }

        [Fact]
        public void GetReplicationNeighbors_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetReplicationNeighbors("partition"));
        }

        [Fact]
        public void GetReplicationNeighbors_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetReplicationNeighbors("partition"));
        }

        [Fact]
        public void GetReplicationOperationInformation_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.GetReplicationOperationInformation());
        }

        [Fact]
        public void GetReplicationOperationInformation_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.GetReplicationOperationInformation());
        }

        [Fact]
        public void HighestCommittedUsn_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.HighestCommittedUsn);
        }

        [Fact]
        public void InboundConnections_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.InboundConnections);
        }

        [Fact]
        public void InboundConnections_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.InboundConnections);
        }

        [Fact]
        public void IPAddress_GetWithNoContext_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("hostNameOrAddress", () => controller.IPAddress);
        }

        [Fact]
        public void IsGlobalCatalog_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.IsGlobalCatalog());
        }

        [Fact]
        public void IsGlobalCatalog_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.IsGlobalCatalog());
        }

        [Fact]
        public void MoveToAnotherSite_NullSiteName_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("siteName", () => controller.MoveToAnotherSite(null));
        }

        [Fact]
        public void MoveToAnotherSite_EmptySiteName_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("siteName", () => controller.MoveToAnotherSite(string.Empty));
        }

        [Fact]
        public void MoveToAnotherSite_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.MoveToAnotherSite("siteName"));
        }

        [Fact]
        public void MoveToAnotherSite_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.MoveToAnotherSite("siteName"));
        }

        [Fact]
        public void Name_GetWithNoContext_ReturnsNull()
        {
            var controller = new SubController();
            Assert.Null(controller.Name);
            Assert.Null(controller.ToString());
        }

        [Fact]
        public void OSVersion_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.OSVersion);
        }

        [Fact]
        public void OSVersion_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.OSVersion);
        }

        [Fact]
        public void OutboundConnections_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.OutboundConnections);
        }

        [Fact]
        public void OutboundConnections_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.OutboundConnections);
        }

        [Fact]
        public void Partitions_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.Partitions);
        }

        [Fact]
        public void Roles_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.Roles);
        }

        [Fact]
        public void Roles_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.Roles);
        }

        [Fact]
        public void SeizeRoleOwnership_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.SeizeRoleOwnership(ActiveDirectoryRole.InfrastructureRole));
            Assert.Throws<NullReferenceException>(() => controller.SeizeRoleOwnership(ActiveDirectoryRole.NamingRole));
            Assert.Throws<NullReferenceException>(() => controller.SeizeRoleOwnership(ActiveDirectoryRole.PdcRole));
            Assert.Throws<NullReferenceException>(() => controller.SeizeRoleOwnership(ActiveDirectoryRole.RidRole));
            Assert.Throws<NullReferenceException>(() => controller.SeizeRoleOwnership(ActiveDirectoryRole.SchemaRole));
        }

        [Theory]
        [InlineData(ActiveDirectoryRole.SchemaRole - 1)]
        [InlineData(ActiveDirectoryRole.InfrastructureRole + 1)]
        public void SeizeRoleOwnership_InvalidRole_ThrowsInvalidEnumArgumentException(ActiveDirectoryRole role)
        {
            var controller = new SubController();
            AssertExtensions.Throws<InvalidEnumArgumentException>("role", () => controller.SeizeRoleOwnership(role));
        }

        [Fact]
        public void SiteName_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.SiteName);
        }

        [Fact]
        public void SiteName_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.SiteName);
        }

        [Fact]
        public void SyncFromAllServersCallback_Set_GetReturnsExpected()
        {
            SyncUpdateCallback callback = SyncUpdateCallback;
            var controller = new SubController { SyncFromAllServersCallback = callback };
            Assert.Equal(callback, controller.SyncFromAllServersCallback);
        }

        private bool SyncUpdateCallback(SyncFromAllServersEvent eventType, string targetServer, string sourceServer, SyncFromAllServersOperationException exception)
        {
            return true;
        }

        [Fact]
        public void SyncFromAllServersCallback_GetSetWhenDisposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.SyncFromAllServersCallback);
            Assert.Throws<ObjectDisposedException>(() => controller.SyncFromAllServersCallback = null);
        }

        [Fact]
        public void SyncFromAllServersCallback_GetWithNoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Null(controller.SyncFromAllServersCallback);
        }

        [Fact]
        public void SyncReplicaFromServer_NullPartition_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("partition", () => controller.SyncReplicaFromServer(null, "sourceServer"));
        }

        [Fact]
        public void SyncReplicaFromServer_EmptyPartition_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("partition", () => controller.SyncReplicaFromServer(string.Empty, "sourceServer"));
        }

        [Fact]
        public void SyncReplicaFromServer_NullSourceServer_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("sourceServer", () => controller.SyncReplicaFromServer("partition", null));
        }

        [Fact]
        public void SyncReplicaFromServer_EmptySourceServer_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("sourceServer", () => controller.SyncReplicaFromServer("partition", string.Empty));
        }

        [Fact]
        public void SyncReplicaFromServer_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.SyncReplicaFromServer("partition", "sourceServer"));
        }

        [Fact]
        public void SyncReplicaFromServer_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.SyncReplicaFromServer("partition", "sourceServer"));
        }

        [Fact]
        public void SyncReplicaFromAllServers_NullPartition_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("partition", () => controller.SyncReplicaFromAllServers(null, SyncFromAllServersOptions.AbortIfServerUnavailable));
        }

        [Fact]
        public void SyncReplicaFromAllServers_EmptyPartition_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("partition", () => controller.SyncReplicaFromAllServers(string.Empty, SyncFromAllServersOptions.AbortIfServerUnavailable));
        }

        [Fact]
        public void SyncReplicaFromAllServers_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.SyncReplicaFromAllServers("partition", SyncFromAllServersOptions.AbortIfServerUnavailable));
        }

        [Fact]
        public void SyncReplicaFromAllServers_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.SyncReplicaFromAllServers("partition", SyncFromAllServersOptions.AbortIfServerUnavailable));
        }

        [Fact]
        public void TransferRoleOwnership_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.TransferRoleOwnership(ActiveDirectoryRole.NamingRole));
        }

        [Theory]
        [InlineData(ActiveDirectoryRole.SchemaRole - 1)]
        [InlineData(ActiveDirectoryRole.InfrastructureRole + 1)]
        public void TransferRoleOwnership_InvalidRole_ThrowsInvalidEnumArgumentException(ActiveDirectoryRole role)
        {
            var controller = new SubController();
            AssertExtensions.Throws<InvalidEnumArgumentException>("role", () => controller.TransferRoleOwnership(role));
        }

        [Fact]
        public void TriggerSyncReplicaFromNeighbors_NullPartition_ThrowsArgumentNullException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentNullException>("partition", () => controller.TriggerSyncReplicaFromNeighbors(null));
        }

        [Fact]
        public void TriggerSyncReplicaFromNeighbors_EmptyPartition_ThrowsArgumentException()
        {
            var controller = new SubController();
            AssertExtensions.Throws<ArgumentException>("partition", () => controller.TriggerSyncReplicaFromNeighbors(string.Empty));
        }

        [Fact]
        public void TriggerSyncReplicaFromNeighbors_Disposed_ThrowsObjectDisposedException()
        {
            var controller = new SubController();
            controller.Dispose();

            Assert.Throws<ObjectDisposedException>(() => controller.TriggerSyncReplicaFromNeighbors("partition"));
        }

        [Fact]
        public void TriggerSyncReplicaFromNeighbors_NoContext_ThrowsNullReferenceException()
        {
            var controller = new SubController();
            Assert.Throws<NullReferenceException>(() => controller.TriggerSyncReplicaFromNeighbors("partition"));
        }

        private class SubController : DomainController
        {
        }
    }
}
