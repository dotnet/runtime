//------------------------------------------------------------------------------
// <copyright file="DirectoryEntryManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Net;
	using System.Collections;
	using System.Globalization;
	using System.ComponentModel;
	using System.DirectoryServices;
	using System.Runtime.InteropServices;
       using System.Security.Permissions;

	/// <summary>
	/// Internal class that is used as a key in the hashtable
	/// of directory entries
	/// </summary>
	internal class DistinguishedName {
		private Component[] components = null;

		public DistinguishedName(string dn) {
			this.components = Utils.GetDNComponents(dn);
		}

		public Component[] Components {
			get {
				return components;
			}
		}
		
		public bool Equals(DistinguishedName dn) {
			bool result = true;
			if ((dn == null) || (components.GetLength(0) != dn.Components.GetLength(0))) {
				result = false;
			}
			else {
				for(int i = 0; i < components.GetLength(0); i++) {
					if ((Utils.Compare(components[i].Name, dn.Components[i].Name) != 0)
						|| (Utils.Compare(components[i].Value, dn.Components[i].Value) != 0)) {
						result = false;
						break;
					}
				}
			}
			return result;
		}

		public override bool Equals(object obj) {
			if ((obj == null) || (!(obj is DistinguishedName))) {
				return false;
			}
			else {
				return Equals((DistinguishedName)obj);
			}
		}

		public override int GetHashCode() {
			int hashCode = 0;
			for (int i = 0; i < components.GetLength(0); i++) {
				hashCode = hashCode + components[i].Name.ToUpperInvariant().GetHashCode() + components[i].Value.ToUpperInvariant().GetHashCode();
			}
			return hashCode;
		}

		public override string ToString()
		{
			string dn = components[0].Name + "=" + components[0].Value;
			for(int i = 1; i < components.GetLength(0); i++) {
				dn = dn + "," + components[i].Name + "=" + components[i].Value;
			}
			return dn;
		}

	}

	/// <summary>
	/// This class manages a list of directory entries
	/// for an object that needs to bind to several
	/// objects in AD. This maintains a cache of directory entries
	/// and creates a new directory entry (for a given dn) only if
	/// it doesn't already exist
	/// </summary>
	internal class DirectoryEntryManager
	{
		private Hashtable directoryEntries = new Hashtable();
		private string bindingPrefix = null;
		private DirectoryContext context = null;
		private NativeComInterfaces.IAdsPathname pathCracker = null;

		internal DirectoryEntryManager(DirectoryContext context)
		{
			this.context = context;
			pathCracker = (NativeComInterfaces.IAdsPathname) new NativeComInterfaces.Pathname();
			pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_ON;
		}

		internal ICollection GetCachedDirectoryEntries() {
			return directoryEntries.Values;
		}

		internal DirectoryEntry GetCachedDirectoryEntry(WellKnownDN dn) {
			return GetCachedDirectoryEntry(ExpandWellKnownDN(dn));
		}

		internal DirectoryEntry GetCachedDirectoryEntry(string distinguishedName) {
			// check if it's not RootDSE
			Object dn = distinguishedName;

			if ((String.Compare(distinguishedName, "rootdse", StringComparison.OrdinalIgnoreCase) != 0) 
			&& (String.Compare(distinguishedName, "schema", StringComparison.OrdinalIgnoreCase) != 0)) {
				dn = new DistinguishedName(distinguishedName);
			}

			if (!directoryEntries.ContainsKey(dn)) {
				// directory entry does not exist
				// create a new one and cache it
				DirectoryEntry de = GetNewDirectoryEntry(distinguishedName);
				// add it to the cache 
				directoryEntries.Add(dn, de);
			}
			return (DirectoryEntry)directoryEntries[dn];
		}

		internal void RemoveIfExists(string distinguishedName) {
			// check if it's not RootDSE
			Object dn = distinguishedName;
			
			//
			// NOTE: Currently only comparing against "rootdse", but in the future if we are going to 
			//           remove any other entries that are not in dn format (such as schema), we need to add the 
			//           special casing here.
			//
			
			if (String.Compare(distinguishedName, "rootdse", StringComparison.OrdinalIgnoreCase) != 0)  {
				dn = new DistinguishedName(distinguishedName);
			}

			if (directoryEntries.ContainsKey(dn)) {
				DirectoryEntry tmp = (DirectoryEntry)directoryEntries[dn];
				if (tmp != null) {
					directoryEntries.Remove(dn);
					tmp.Dispose();
				}
			}
		}

		private DirectoryEntry GetNewDirectoryEntry(string dn) {
			if (bindingPrefix == null) {
				bindingPrefix = "LDAP://" + context.GetServerName() + "/";
			}

			pathCracker.Set(dn, NativeComInterfaces.ADS_SETTYPE_DN);
			string escapedDN = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_X500_DN);
			
			return Bind(bindingPrefix + escapedDN, context.UserName, context.Password, context.useServerBind());
		}

		internal string ExpandWellKnownDN(WellKnownDN dn) {
			string distinguishedName = null;

			switch (dn) {
				case WellKnownDN.RootDSE: {
					distinguishedName = "RootDSE";
					break;
				}
				case WellKnownDN.RootDomainNamingContext : {
					DirectoryEntry rootDSE = GetCachedDirectoryEntry("RootDSE");

					distinguishedName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.RootDomainNamingContext);
					break;
				}
				case WellKnownDN.DefaultNamingContext: {
					DirectoryEntry	rootDSE = GetCachedDirectoryEntry("RootDSE");
					distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DefaultNamingContext);
					break;
				}
				case WellKnownDN.SchemaNamingContext: {
					DirectoryEntry	rootDSE = GetCachedDirectoryEntry("RootDSE");
					distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.SchemaNamingContext);
					break;
				}
				case WellKnownDN.ConfigurationNamingContext: {
					DirectoryEntry	rootDSE = GetCachedDirectoryEntry("RootDSE");
					distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ConfigurationNamingContext);
					break;
				}
				case WellKnownDN.PartitionsContainer: {
					distinguishedName = "CN=Partitions," + ExpandWellKnownDN(WellKnownDN.ConfigurationNamingContext);
					break;
				}
				case WellKnownDN.SitesContainer: {
					distinguishedName = "CN=Sites," + ExpandWellKnownDN(WellKnownDN.ConfigurationNamingContext);
					break;
				}
				case WellKnownDN.SystemContainer: {
					distinguishedName = "CN=System," + ExpandWellKnownDN(WellKnownDN.DefaultNamingContext);
					break;
				}
				case WellKnownDN.RidManager: {
					distinguishedName = "CN=RID Manager$," + ExpandWellKnownDN(WellKnownDN.SystemContainer);
					break;
				}
				case WellKnownDN.Infrastructure: {
					distinguishedName = "CN=Infrastructure," + ExpandWellKnownDN(WellKnownDN.DefaultNamingContext);
					break;
				}
				default:
					// should not happen
					throw new InvalidEnumArgumentException("dn", (int)dn, typeof(WellKnownDN));
			}
			return distinguishedName;
		}

		internal static DirectoryEntry GetDirectoryEntry(DirectoryContext context, WellKnownDN dn) {
			return GetDirectoryEntry(context, ExpandWellKnownDN(context, dn));
		}

		internal static DirectoryEntry GetDirectoryEntry(DirectoryContext context, string dn) {
			string tempBindingPrefix = "LDAP://" + context.GetServerName() + "/";

			NativeComInterfaces.IAdsPathname pathCracker = (NativeComInterfaces.IAdsPathname) new NativeComInterfaces.Pathname();
			pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_ON;
			pathCracker.Set(dn, NativeComInterfaces.ADS_SETTYPE_DN);
			string escapedDN = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_X500_DN);

			return Bind(tempBindingPrefix + escapedDN, context.UserName, context.Password, context.useServerBind());
		}

                internal static DirectoryEntry GetDirectoryEntryInternal(DirectoryContext context, string path) {
                        return Bind(path, context.UserName, context.Password, context.useServerBind());
                }

		internal static DirectoryEntry Bind(string ldapPath, string username, string password, bool useServerBind) {
			DirectoryEntry de = null;
			AuthenticationTypes authType = Utils.DefaultAuthType;

			//
			// use ServerBind flag is the target is a server and the ServerBind option is supported
			//

			if (DirectoryContext.ServerBindSupported && useServerBind) {
				authType |= AuthenticationTypes.ServerBind;
			}
			
			de = new DirectoryEntry(ldapPath, username, password, authType);
			return de;
		}

		internal static string ExpandWellKnownDN(DirectoryContext context, WellKnownDN dn) {
			string distinguishedName = null;

			switch (dn) {
				case WellKnownDN.RootDSE: {
					distinguishedName = "RootDSE";
					break;
				}
				case WellKnownDN.RootDomainNamingContext : {
					DirectoryEntry rootDSE = GetDirectoryEntry(context, "RootDSE");

					try {
						distinguishedName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.RootDomainNamingContext);
					}
					finally {
						rootDSE.Dispose();
					}
					break;
				}
				case WellKnownDN.DefaultNamingContext: {
					
					DirectoryEntry rootDSE = GetDirectoryEntry(context, "RootDSE");
					try {
						distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DefaultNamingContext);
					}
					finally {
						rootDSE.Dispose();
					}
					break;
				}
				case WellKnownDN.SchemaNamingContext: {

					DirectoryEntry rootDSE = GetDirectoryEntry(context, "RootDSE");
					try {
						distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.SchemaNamingContext);
					}
					finally {
						rootDSE.Dispose();
					}
					break;
				}
				case WellKnownDN.ConfigurationNamingContext: {

					DirectoryEntry rootDSE = GetDirectoryEntry(context, "RootDSE");
					try {
						distinguishedName = (string) PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ConfigurationNamingContext);
					}
					finally {
						rootDSE.Dispose();
					}
					break;
				}
				case WellKnownDN.PartitionsContainer: {
					distinguishedName = "CN=Partitions," + ExpandWellKnownDN(context, WellKnownDN.ConfigurationNamingContext);
					break;
				}
				case WellKnownDN.SitesContainer: {
					distinguishedName = "CN=Sites," + ExpandWellKnownDN(context, WellKnownDN.ConfigurationNamingContext);
					break;
				}
				case WellKnownDN.SystemContainer: {
					distinguishedName = "CN=System," + ExpandWellKnownDN(context, WellKnownDN.DefaultNamingContext);
					break;
				}
				case WellKnownDN.RidManager: {
					distinguishedName = "CN=RID Manager$," + ExpandWellKnownDN(context, WellKnownDN.SystemContainer);
					break;
				}
				case WellKnownDN.Infrastructure: {
					distinguishedName = "CN=Infrastructure," + ExpandWellKnownDN(context, WellKnownDN.DefaultNamingContext);
					break;
				}
				default:
					// should not happen
					throw new InvalidEnumArgumentException("dn", (int)dn, typeof(WellKnownDN));
			}
			return distinguishedName;
		}

	}
}
