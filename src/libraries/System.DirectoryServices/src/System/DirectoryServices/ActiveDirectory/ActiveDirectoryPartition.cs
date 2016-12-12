//------------------------------------------------------------------------------
// <copyright file="ActiveDirectoryPartition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Collections;
	using System.Globalization;
	using System.Runtime.InteropServices;
       using System.Security.Permissions;
       
	public abstract class ActiveDirectoryPartition : IDisposable {

		private bool disposed = false;
		internal string partitionName = null;
		internal DirectoryContext context = null;
		internal DirectoryEntryManager directoryEntryMgr = null;

		#region constructors
		protected ActiveDirectoryPartition() {
		}

		internal ActiveDirectoryPartition(DirectoryContext context, string name) {
			this.context = context;
			this.partitionName = name;
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
		public override string ToString() {
			return Name;
		}

              [
                  DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)                  
              ]
		public abstract DirectoryEntry GetDirectoryEntry();

		#endregion public methods

		#region public properties
		// Public Properties
		public string Name {
			get {
				CheckIfDisposed();
				return partitionName;
			}
		}
		#endregion public properties

		#region private methods

		internal void CheckIfDisposed() {
			if (this.disposed) {
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		#endregion private methods
	}
}
