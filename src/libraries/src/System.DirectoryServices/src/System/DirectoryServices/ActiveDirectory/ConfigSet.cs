//------------------------------------------------------------------------------
// <copyright file="ConfigurationSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Text;
	using System.Collections;
	using System.ComponentModel;
	using System.Globalization;
	using System.Diagnostics;
	using System.Runtime.InteropServices;
       using System.Security.Permissions;

       [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
	public class ConfigurationSet {
		
		// Private Variables
		private DirectoryContext context = null;
		private DirectoryEntryManager directoryEntryMgr = null;
		private bool disposed = false;		
		
		// variables corresponding to public properties
	    private string configSetName = null;
		private ReadOnlySiteCollection cachedSites = null;
		private AdamInstanceCollection cachedADAMInstances = null;
		private ApplicationPartitionCollection cachedApplicationPartitions = null;
		private ActiveDirectorySchema cachedSchema = null;
		private AdamInstance cachedSchemaRoleOwner = null;
		private AdamInstance cachedNamingRoleOwner = null;
		private ReplicationSecurityLevel cachedSecurityLevel = (ReplicationSecurityLevel)(-1);

		// 4 minutes timeout for locating an ADAM instance in the configset
		private static TimeSpan locationTimeout = new TimeSpan(0, 4, 0);

		#region constructors
		internal ConfigurationSet(DirectoryContext context, string configSetName, DirectoryEntryManager directoryEntryMgr) {
			this.context = context;
			this.configSetName = configSetName;
			this.directoryEntryMgr = directoryEntryMgr;
		}

		internal ConfigurationSet(DirectoryContext context, string configSetName) 
			:this(context, configSetName, new DirectoryEntryManager(context)) {
		}
		#endregion constructors

		#region IDisposable

		public void Dispose() {
			Dispose(true);
		}

		// private Dispose method
		protected virtual void Dispose(bool disposing) {
			if (!this.disposed) {
				// check if this is an explicit Dispose
				// only then clean up the directory entries
				if (disposing) {
					// dispose all directory entries
					foreach (DirectoryEntry entry in directoryEntryMgr.GetCachedDirectoryEntries()) {
						entry.Dispose();
					}
				}
				this.disposed = true;
			}
		}
		#endregion IDisposable

		#region public methods
		
		public static ConfigurationSet GetConfigurationSet(DirectoryContext context) {
			// check that the argument is not null
			if (context == null)
				throw new ArgumentNullException("context");

			// target should ConfigurationSet or DirectoryServer
			if ((context.ContextType != DirectoryContextType.ConfigurationSet) &&
				(context.ContextType != DirectoryContextType.DirectoryServer)) {
				throw new ArgumentException(Res.GetString(Res.TargetShouldBeServerORConfigSet), "context");
			}

			// target should be an adam config set or server
			if (((!context.isServer()) && (!context.isADAMConfigSet())))	{
				// the target should be a server or an ADAM Config Set
				if (context.ContextType == DirectoryContextType.ConfigurationSet) {
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ConfigSetNotFound), typeof(ConfigurationSet), context.Name);
				}
				else {
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.AINotFound, context.Name), typeof(ConfigurationSet), null);
				}
			}

			//  work with copy of the context
			context = new DirectoryContext(context);

			//
			// bind to rootdse of an adam instance (if target is already a server, verify that it is an adam instance)
			//
			DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
			DirectoryEntry rootDSE = null;
			string configSetName = null;

			try {
				rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
				if ((context.isServer()) && (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryApplicationMode))) {
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.AINotFound, context.Name), typeof(ConfigurationSet), null);
				}

				configSetName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ConfigurationNamingContext);
			}
			catch (COMException e) {
				int errorCode = e.ErrorCode;
			
				if (errorCode == unchecked((int)0x8007203a)) {
					if (context.ContextType == DirectoryContextType.ConfigurationSet) {
						throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ConfigSetNotFound), typeof(ConfigurationSet), context.Name);
					}
					else {
						throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.AINotFound, context.Name), typeof(ConfigurationSet), null);
					}
				}
				else {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
			catch (ActiveDirectoryObjectNotFoundException) {
				if (context.ContextType == DirectoryContextType.ConfigurationSet) {
					// this is the case when we could not find an ADAM instance in that config set
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ConfigSetNotFound), typeof(ConfigurationSet), context.Name);
				}
				else
					throw;
			}

			// return config set object
			return new ConfigurationSet(context, configSetName, directoryEntryMgr);

		}

		public AdamInstance FindAdamInstance()	{
                     CheckIfDisposed();
			return FindOneAdamInstance(Name, context, null, null);
		}

		
		public AdamInstance FindAdamInstance(string partitionName)	{

                     CheckIfDisposed();

                     if (partitionName == null) {
                            throw new ArgumentNullException("partitionName");
                     }
                     
			return FindOneAdamInstance(Name, context, partitionName, null);
		}
	
		public AdamInstance FindAdamInstance(string partitionName, string siteName)	{
			
                     CheckIfDisposed();

                     //
                     // null partitionName would signify that we don't care about the partition 
                     //
            
			if (siteName == null) {
                            throw new ArgumentNullException("siteName");
                     }
                     
			return FindOneAdamInstance(Name, context, partitionName, siteName);
		}


		public AdamInstanceCollection FindAllAdamInstances() {
                     CheckIfDisposed();
                     
			return FindAdamInstances(context, null, null);
		}

		
		public AdamInstanceCollection FindAllAdamInstances(string partitionName){
		
                     CheckIfDisposed();
			
			if (partitionName == null) {
                            throw new ArgumentNullException("partitionName");
                     }
            
			return FindAdamInstances(context, partitionName, null);
		}
		
		public AdamInstanceCollection FindAllAdamInstances(string partitionName, string siteName)	 {
			
                     CheckIfDisposed();
					 
                     //
                     // null partitionName would signify that we don't care about the partition 
                     //
            
                     if (siteName == null) {
                            throw new ArgumentNullException("siteName");
                     }
                                     
			return FindAdamInstances(context, partitionName, siteName);
		}

		
		public DirectoryEntry GetDirectoryEntry() {

			CheckIfDisposed();
			return DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.ConfigurationNamingContext);
		}

		
		public ReplicationSecurityLevel GetSecurityLevel() {
			CheckIfDisposed();
			if (cachedSecurityLevel == (ReplicationSecurityLevel)(-1)) {
				DirectoryEntry configEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.ConfigurationNamingContext);
				cachedSecurityLevel = (ReplicationSecurityLevel)((int)PropertyManager.GetPropertyValue(context, configEntry, PropertyManager.MsDSReplAuthenticationMode));
			}
			return cachedSecurityLevel;
		}

		
		public void SetSecurityLevel(ReplicationSecurityLevel securityLevel) {
			CheckIfDisposed();
			if (securityLevel < ReplicationSecurityLevel.NegotiatePassThrough || securityLevel > ReplicationSecurityLevel.MutualAuthentication) {
				throw new InvalidEnumArgumentException("securityLevel", (int)securityLevel, typeof(ReplicationSecurityLevel));
			}

			try {
				DirectoryEntry configEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.ConfigurationNamingContext);
				configEntry.Properties[PropertyManager.MsDSReplAuthenticationMode].Value = (int)securityLevel;
				configEntry.CommitChanges();
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}

			// invalidate the cached entry
			cachedSecurityLevel = (ReplicationSecurityLevel)(-1);
		}

		public override string ToString() {
			return Name;
		}
		#endregion public methods

		#region public properties
		
		public string Name	{
			get	{
				CheckIfDisposed();
				return configSetName;
			}
		}

		
		public ReadOnlySiteCollection Sites	{
			get	{
				CheckIfDisposed();
				if (cachedSites == null) {
					cachedSites = new ReadOnlySiteCollection(GetSites());
				}
				return cachedSites;
			}
		}

		
		public AdamInstanceCollection AdamInstances {
			get {
				CheckIfDisposed();
				if (cachedADAMInstances == null)	{
					cachedADAMInstances = FindAllAdamInstances();
				}
				return cachedADAMInstances;
			}
		}

		
		public ApplicationPartitionCollection ApplicationPartitions	{
			get	{
				CheckIfDisposed();
				if (cachedApplicationPartitions == null) {
					cachedApplicationPartitions = new ApplicationPartitionCollection(GetApplicationPartitions());
				}
				return cachedApplicationPartitions;
			}
		}

		
		public ActiveDirectorySchema Schema	{
			get	{
				CheckIfDisposed();
				if (cachedSchema == null) {
					try {
						cachedSchema = new ActiveDirectorySchema(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext));
					}
					catch (COMException e) {
						throw ExceptionHelper.GetExceptionFromCOMException(context, e);
					}
				}
				return cachedSchema;
			}
		}

		
		public AdamInstance SchemaRoleOwner	{
			get	{
				CheckIfDisposed();
				if (cachedSchemaRoleOwner == null)	{
					cachedSchemaRoleOwner = GetRoleOwner(AdamRole.SchemaRole);
				}
				return cachedSchemaRoleOwner;
			}
		}

        
		public AdamInstance NamingRoleOwner	{
			get	{
				CheckIfDisposed();
				if (cachedNamingRoleOwner == null)	{
					cachedNamingRoleOwner = GetRoleOwner(AdamRole.NamingRole);
				}
				return cachedNamingRoleOwner;
			}
		}

		#endregion public properties

		#region private methods

              [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted=true)]
		private static DirectoryEntry GetSearchRootEntry(Forest forest) {
			DirectoryEntry rootEntry;
			DirectoryContext forestContext = forest.GetDirectoryContext();
			bool isServer = false;
			bool isGC = false;
			AuthenticationTypes authType = Utils.DefaultAuthType;

			if (forestContext.ContextType == DirectoryContextType.DirectoryServer) {
				//
				// the forest object was created by specifying a server name 
				// so we will stick to that server for the search. We need to determine 
				// whether or not the server is a DC or GC
				//
				isServer = true;
				DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(forestContext, WellKnownDN.RootDSE);
				string isGCReady = (string) PropertyManager.GetPropertyValue(forestContext, rootDSE, PropertyManager.IsGlobalCatalogReady);
				isGC = (Utils.Compare(isGCReady, "TRUE") == 0);
			}

			if (isServer) {
				if (DirectoryContext.ServerBindSupported) {
					authType |= AuthenticationTypes.ServerBind;
				}
				if (isGC) {
					rootEntry = new DirectoryEntry("GC://" + forestContext.GetServerName(), forestContext.UserName, forestContext.Password, authType);
				}
				else {
					rootEntry = new DirectoryEntry("LDAP://" + forestContext.GetServerName(), forestContext.UserName, forestContext.Password, authType);
				}
			}
			else {
				// need to find any GC in the forest
				rootEntry = new DirectoryEntry("GC://" + forest.Name, forestContext.UserName, forestContext.Password, authType);
			}

			return rootEntry;
		}
		
		internal static AdamInstance FindAnyAdamInstance(DirectoryContext context) {
			
			if (context.ContextType != DirectoryContextType.ConfigurationSet)	{
				// assuming it's an ADAM Instance 
				// check that it is an ADAM server only (not AD)
				DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
				DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

				if (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryApplicationMode))	{
					directoryEntryMgr.RemoveIfExists(directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.RootDSE));
					throw new ArgumentException(Res.GetString(Res.TargetShouldBeServerORConfigSet), "context");
				}
				
				string dnsHostName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DnsHostName);
				
				return new AdamInstance(context, dnsHostName, directoryEntryMgr);
			}

			// Now this is the case where context is a Config Set
			// Here we need to search for the service connection points in the forest
			// (if the forest object was created by specifying the server, we stick to that, else search in a GC)
			DirectoryEntry rootEntry = GetSearchRootEntry(Forest.GetCurrentForest());
			ArrayList adamInstanceNames = new ArrayList();

			try	{
				string entryName = (string)rootEntry.Properties["distinguishedName"].Value;

				// Search for computer "serviceConnectionObjects" where the keywords attribute 
				// contains the specified keyword
				// set up the searcher object

				// build the filter
				StringBuilder str = new StringBuilder(15);
				str.Append("(&(");
				str.Append(PropertyManager.ObjectCategory);
				str.Append("=serviceConnectionPoint)");
				str.Append("(");
				str.Append(PropertyManager.Keywords);
				str.Append("=1.2.840.113556.1.4.1851)(");
				str.Append(PropertyManager.Keywords);
				str.Append("=");
				str.Append(Utils.GetEscapedFilterValue(context.Name)); // target = config set name 
				str.Append("))");

				string filter = str.ToString();
				string[] propertiesToLoad = new string[1];

				propertiesToLoad[0] = PropertyManager.ServiceBindingInformation;

				ADSearcher searcher = new ADSearcher(rootEntry, filter, propertiesToLoad, SearchScope.Subtree, false /*not paged search*/, false /*no cached results*/);
				SearchResultCollection resCol = searcher.FindAll();

				try {
					foreach (SearchResult res in resCol) {

						// the binding info contains two values
						// "ldap://hostname:ldapport"
						// and "ldaps://hostname:sslport"
						// we need the "hostname:ldapport" value
						string prefix = "ldap://";

						foreach (string bindingInfo in res.Properties[PropertyManager.ServiceBindingInformation])	{
							if ((bindingInfo.Length > prefix.Length) && (String.Compare(bindingInfo.Substring(0, prefix.Length), prefix, StringComparison.OrdinalIgnoreCase) == 0))	{
								adamInstanceNames.Add(bindingInfo.Substring(prefix.Length));
							}
						}
					}
				}
				finally {
					resCol.Dispose();
				}

			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally	{
				rootEntry.Dispose();
			}

			//
			// we have all the adam instance names in teh form of server:port from the scp
			// now we need to find one that is alive
			//
			return FindAliveAdamInstance(null, context, adamInstanceNames);

		}

		internal static AdamInstance FindOneAdamInstance(DirectoryContext context, string partitionName, string siteName) {
			return FindOneAdamInstance(null, context, partitionName, siteName);
		}

		internal static AdamInstance FindOneAdamInstance(string configSetName, DirectoryContext context, string partitionName, string siteName) {

			// can expect valid context (non-null)
			if (partitionName != null && partitionName.Length == 0)  {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "partitionName");
			}

			if (siteName != null && siteName.Length == 0)  {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "siteName");
			}

			ArrayList ntdsaNames = Utils.GetReplicaList(context, partitionName, siteName, false /* isDefaultNC */, true /* isADAM */, false /* mustBeGC */);

			if (ntdsaNames.Count < 1) {
				throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ADAMInstanceNotFound), typeof(AdamInstance), null);
			}

			return FindAliveAdamInstance(configSetName, context, ntdsaNames);
		}

		internal static AdamInstanceCollection FindAdamInstances(DirectoryContext context, string partitionName, string siteName) {
		    
			// can expect valid context (non-null)
			if (partitionName != null && partitionName.Length == 0)  {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "partitionName");
			}

			if (siteName != null && siteName.Length == 0) {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "siteName");
			}

			ArrayList adamInstanceList = new ArrayList();

			foreach(string adamInstanceName in Utils.GetReplicaList(context, partitionName, siteName, false /* isDefaultNC */, true /* isADAM */, false /* mustBeGC */)) {
				DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstanceName, DirectoryContextType.DirectoryServer, context);
				adamInstanceList.Add(new AdamInstance(adamInstContext, adamInstanceName));
			}

			return new AdamInstanceCollection(adamInstanceList);
			
			
		}

		//
		// The input to this function is a list of adam instance names in the form server:port
		// This function tries to bind to each of the instances in this list sequentially until one of the following occurs:
		// 1.  An ADAM instance responds to an ldap_bind - we return an ADAMInstance object for that adam instance
		// 2.  We exceed the timeout duration - we return an ActiveDirectoryObjectNotFoundException
		//
		internal static AdamInstance FindAliveAdamInstance(string configSetName, DirectoryContext context, ArrayList adamInstanceNames)
		{
			bool foundAliveADAMInstance = false;
			AdamInstance adamInstance = null;

			// record the start time so that we can determine if the timeout duration has been exceeded or not
			DateTime startTime = DateTime.UtcNow;

			// loop through each adam instance and try to bind to the rootdse
			foreach (string adamInstanceName in adamInstanceNames) {

				DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstanceName, DirectoryContextType.DirectoryServer, context);
				DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(adamInstContext);
				DirectoryEntry tempRootEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

				try {
					tempRootEntry.Bind(true);
					adamInstance = new AdamInstance(adamInstContext, adamInstanceName, directoryEntryMgr, true /* nameIncludesPort */);
					foundAliveADAMInstance = true;
				}
				catch (COMException e)
				{
					// if this is server down /server busy / server unavailable / timeout  exception we should just eat this up and try the next one
					if ((e.ErrorCode == unchecked((int) 0x8007203a)) ||
						(e.ErrorCode == unchecked((int) 0x8007200e)) ||
						(e.ErrorCode == unchecked((int) 0x8007200f)) ||
						(e.ErrorCode == unchecked((int) 0x800705b4)))  {
						
						// if we are passed the timeout period, we should throw, else do nothing
						if (DateTime.UtcNow.Subtract(startTime) > locationTimeout)
							throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, (configSetName != null) ? configSetName : context.Name), typeof(AdamInstance), null);
					}
					else
						throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}

				if (foundAliveADAMInstance) {
					return adamInstance;
				}
			}

			// if we reach here, we haven't found an adam instance
			throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, (configSetName != null) ? configSetName : context.Name), typeof(AdamInstance), null);
				
		}
			

		/// <returns>Returns a DomainController object for the DC that holds the the specified FSMO role</returns>
		private AdamInstance GetRoleOwner(AdamRole role)	{
			DirectoryEntry entry = null;

			string adamInstName = null;
			try {
				switch (role)	{
					case AdamRole.SchemaRole : {
							entry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.SchemaNamingContext);
							break;
						}

					case AdamRole.NamingRole : {
							entry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.PartitionsContainer);
							break;
						}

					default :
						// should not happen since we are calling this only internally
						Debug.Assert(false, "ConfigurationSet.GetRoleOwner: Invalid role type.");
						break;
				}
				entry.RefreshCache();
				adamInstName = Utils.GetAdamDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, entry, PropertyManager.FsmoRoleOwner));
				
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally {
				if (entry != null) {
					entry.Dispose();
				}
			}

			// create a new context object for the adam instance passing on  the 
			// credentials from the context
			DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstName, DirectoryContextType.DirectoryServer, context);

			return new AdamInstance(adamInstContext, adamInstName);
		}

		private ArrayList GetSites() {
			ArrayList sites = new ArrayList();
			DirectoryEntry sitesEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.SitesContainer);

			// search for all the "site" objects 
			// (one-level search is good enough)
			// setup the directory searcher object
			string filter = "(" + PropertyManager.ObjectCategory + "=site)";
			string[] propertiesToLoad = new string[1];

			propertiesToLoad[0] = PropertyManager.Cn;

			ADSearcher searcher = new ADSearcher(sitesEntry, filter, propertiesToLoad, SearchScope.OneLevel);
			SearchResultCollection resCol = null;

			try	{
				resCol = searcher.FindAll();
				
				foreach (SearchResult res in resCol) {
					// an existing site
					sites.Add(new ActiveDirectorySite(context, (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.Cn), true));
				}
			}
			catch (COMException e) {	
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally	{
				if (resCol != null) {
					// call dispose on search result collection
					resCol.Dispose();
				}
			}
			return sites;
		}

		private ArrayList GetApplicationPartitions()	{
			ArrayList appNCs = new ArrayList();
			DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
			DirectoryEntry partitionsEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.PartitionsContainer);

			// search for all the "crossRef" objects that have the 
			// ADS_SYSTEMFLAG_CR_NTDS_NC set and the SYSTEMFLAG_CR_NTDS_DOMAIN flag not set
			// (one-level search is good enough)
			// setup the directory searcher object

			// build the filter
			StringBuilder str = new StringBuilder(100);
			str.Append("(&(");
			str.Append(PropertyManager.ObjectCategory);
			str.Append("=crossRef)(");
			str.Append(PropertyManager.SystemFlags);
			str.Append(":1.2.840.113556.1.4.804:=");
			str.Append((int)SystemFlag.SystemFlagNtdsNC);
			str.Append(")(!(");
			str.Append(PropertyManager.SystemFlags);
			str.Append(":1.2.840.113556.1.4.803:=");
			str.Append((int)SystemFlag.SystemFlagNtdsDomain);
			str.Append(")))");

			string filter = str.ToString();
			string[] propertiesToLoad = new string[2];
			propertiesToLoad[0] = PropertyManager.NCName;
			propertiesToLoad[1] = PropertyManager.MsDSNCReplicaLocations;
		
			ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel);
			SearchResultCollection resCol = null;

			try	{

				resCol = searcher.FindAll();
				
				string schemaNamingContext = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.SchemaNamingContext);
				string configurationNamingContext = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ConfigurationNamingContext);

				foreach (SearchResult res in resCol)	{
					// add the name of the appNC only if it is not 
					// the Schema or Configuration partition
					string nCName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.NCName);

					if ((!(nCName.Equals(schemaNamingContext))) && (!(nCName.Equals(configurationNamingContext))))	{	
						ResultPropertyValueCollection replicaLocations = res.Properties[PropertyManager.MsDSNCReplicaLocations];
						if (replicaLocations.Count > 0) {
							string replicaName = Utils.GetAdamDnsHostNameFromNTDSA(context, (string)replicaLocations[Utils.GetRandomIndex(replicaLocations.Count)]);
							DirectoryContext appNCContext = Utils.GetNewDirectoryContext(replicaName, DirectoryContextType.DirectoryServer, context);
							appNCs.Add(new ApplicationPartition(appNCContext, nCName, null, ApplicationPartitionType.ADAMApplicationPartition, new DirectoryEntryManager(appNCContext)));
						}
					}
				}
			}
			catch (COMException e) {	
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally {
				if (resCol != null) {
					// call dispose on search result collection
					resCol.Dispose();
				}
			}
			return appNCs;
		}

		private void CheckIfDisposed() {
			if (this.disposed) {
				throw new ObjectDisposedException(GetType().Name);
			}
		}
		#endregion private methods
	}
}
