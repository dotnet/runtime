// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;

namespace System.DirectoryServices.ActiveDirectory
{
    public class DirectoryServerCollection : CollectionBase
    {
        internal readonly string siteDN = null;
        internal readonly string transportDN = null;
        internal readonly DirectoryContext context = null;
        internal bool initialized = false;
        internal readonly Hashtable changeList = null;
        private readonly ArrayList _copyList = new ArrayList();
        private readonly DirectoryEntry _crossRefEntry = null;
        private readonly bool _isADAM = false;
        private readonly bool _isForNC = false;

        internal DirectoryServerCollection(DirectoryContext context, string siteDN, string transportName)
        {
            Hashtable tempTable = new Hashtable();

            changeList = Hashtable.Synchronized(tempTable);
            this.context = context;
            this.siteDN = siteDN;
            this.transportDN = transportName;
        }

        internal DirectoryServerCollection(DirectoryContext context, DirectoryEntry crossRefEntry, bool isADAM, ReadOnlyDirectoryServerCollection servers)
        {
            this.context = context;
            _crossRefEntry = crossRefEntry;
            _isADAM = isADAM;

            _isForNC = true;
            foreach (DirectoryServer server in servers)
            {
                InnerList.Add(server);
            }
        }

        public DirectoryServer this[int index]
        {
            get => (DirectoryServer)InnerList[index];
            set
            {
                DirectoryServer server = (DirectoryServer)value;

                if (server == null)
                    throw new ArgumentNullException(nameof(value));

                if (!Contains(server))
                    List[index] = server;
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , server), nameof(value));
            }
        }

        public int Add(DirectoryServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            // make sure that it is within the current site
            if (_isForNC)
            {
                if ((!_isADAM))
                {
                    if (!(server is DomainController))
                        throw new ArgumentException(SR.ServerShouldBeDC, nameof(server));

                    // verify that the version >= 5.2
                    // DC should be Win 2003 or higher
                    if (((DomainController)server).NumericOSVersion < 5.2)
                    {
                        throw new ArgumentException(SR.ServerShouldBeW2K3, nameof(server));
                    }
                }

                if (!Contains(server))
                {
                    return List.Add(server);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , server), nameof(server));
                }
            }
            else
            {
                string siteName = (server is DomainController) ? ((DomainController)server).SiteObjectName : ((AdamInstance)server).SiteObjectName;
                Debug.Assert(siteName != null);
                if (Utils.Compare(siteDN, siteName) != 0)
                {
                    throw new ArgumentException(SR.NotWithinSite);
                }

                if (!Contains(server))
                    return List.Add(server);
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , server), nameof(server));
            }
        }

        public void AddRange(DirectoryServer[] servers)
        {
            if (servers == null)
                throw new ArgumentNullException(nameof(servers));

            foreach (DirectoryServer s in servers)
            {
                if (s == null)
                {
                    throw new ArgumentException(nameof(servers));
                }
            }

            for (int i = 0; ((i) < (servers.Length)); i = ((i) + (1)))
                this.Add(servers[i]);
        }

        public bool Contains(DirectoryServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer tmp = (DirectoryServer)InnerList[i];

                if (Utils.Compare(tmp.Name, server.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(DirectoryServer[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(DirectoryServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer tmp = (DirectoryServer)InnerList[i];

                if (Utils.Compare(tmp.Name, server.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, DirectoryServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (_isForNC)
            {
                if ((!_isADAM))
                {
                    if (!(server is DomainController))
                        throw new ArgumentException(SR.ServerShouldBeDC, nameof(server));

                    // verify that the version >= 5.2
                    // DC should be Win 2003 or higher
                    if (((DomainController)server).NumericOSVersion < 5.2)
                    {
                        throw new ArgumentException(SR.ServerShouldBeW2K3, nameof(server));
                    }
                }

                if (!Contains(server))
                {
                    List.Insert(index, server);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , server), nameof(server));
                }
            }
            else
            {
                // make sure that it is within the current site
                string siteName = (server is DomainController) ? ((DomainController)server).SiteObjectName : ((AdamInstance)server).SiteObjectName;
                Debug.Assert(siteName != null);
                if (Utils.Compare(siteDN, siteName) != 0)
                {
                    throw new ArgumentException(SR.NotWithinSite, nameof(server));
                }

                if (!Contains(server))
                    List.Insert(index, server);
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , server));
            }
        }

        public void Remove(DirectoryServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer tmp = (DirectoryServer)InnerList[i];

                if (Utils.Compare(tmp.Name, server.Name) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collection
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection , server), nameof(server));
        }

        protected override void OnClear()
        {
            if (initialized && !_isForNC)
            {
                _copyList.Clear();
                foreach (object o in List)
                {
                    _copyList.Add(o);
                }
            }
        }

        protected override void OnClearComplete()
        {
            // if the property exists, clear it out
            if (_isForNC)
            {
                if (_crossRefEntry != null)
                {
                    try
                    {
                        if (_crossRefEntry.Properties.Contains(PropertyManager.MsDSNCReplicaLocations))
                        {
                            _crossRefEntry.Properties[PropertyManager.MsDSNCReplicaLocations].Clear();
                        }
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
            }
            else if (initialized)
            {
                for (int i = 0; i < _copyList.Count; i++)
                {
                    OnRemoveComplete(i, _copyList[i]);
                }
            }
        }

        protected override void OnInsertComplete(int index, object value)
        {
            if (_isForNC)
            {
                if (_crossRefEntry != null)
                {
                    try
                    {
                        DirectoryServer server = (DirectoryServer)value;
                        string ntdsaName = (server is DomainController) ? ((DomainController)server).NtdsaObjectName : ((AdamInstance)server).NtdsaObjectName;
                        _crossRefEntry.Properties[PropertyManager.MsDSNCReplicaLocations].Add(ntdsaName);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
            }
            else if (initialized)
            {
                DirectoryServer server = (DirectoryServer)value;
                string name = server.Name;
                string serverName = (server is DomainController) ? ((DomainController)server).ServerObjectName : ((AdamInstance)server).ServerObjectName;

                try
                {
                    if (changeList.Contains(name))
                    {
                        ((DirectoryEntry)changeList[name]).Properties["bridgeheadTransportList"].Value = this.transportDN;
                    }
                    else
                    {
                        DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverName);

                        de.Properties["bridgeheadTransportList"].Value = this.transportDN;
                        changeList.Add(name, de);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            if (_isForNC)
            {
                try
                {
                    if (_crossRefEntry != null)
                    {
                        string ntdsaName = (value is DomainController) ? ((DomainController)value).NtdsaObjectName : ((AdamInstance)value).NtdsaObjectName;
                        _crossRefEntry.Properties[PropertyManager.MsDSNCReplicaLocations].Remove(ntdsaName);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            else
            {
                DirectoryServer server = (DirectoryServer)value;
                string name = server.Name;
                string serverName = (server is DomainController) ? ((DomainController)server).ServerObjectName : ((AdamInstance)server).ServerObjectName;

                try
                {
                    if (changeList.Contains(name))
                    {
                        ((DirectoryEntry)changeList[name]).Properties["bridgeheadTransportList"].Clear();
                    }
                    else
                    {
                        DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverName);

                        de.Properties["bridgeheadTransportList"].Clear();
                        changeList.Add(name, de);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            OnRemoveComplete(index, oldValue);
            OnInsertComplete(index, newValue);
        }

        protected override void OnValidate(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (_isForNC)
            {
                if (_isADAM)
                {
                    // for adam this should be an ADAMInstance
                    if (!(value is AdamInstance))
                        throw new ArgumentException(SR.ServerShouldBeAI, nameof(value));
                }
                else
                {
                    // for AD this should be a DomainController
                    if (!(value is DomainController))
                        throw new ArgumentException(SR.ServerShouldBeDC, nameof(value));
                }
            }
            else
            {
                if (!(value is DirectoryServer))
                    throw new ArgumentException(nameof(value));
            }
        }

        internal string[] GetMultiValuedProperty()
        {
            ArrayList values = new ArrayList();
            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer ds = (DirectoryServer)InnerList[i];

                string ntdsaName = (ds is DomainController) ? ((DomainController)ds).NtdsaObjectName : ((AdamInstance)ds).NtdsaObjectName;
                values.Add(ntdsaName);
            }
            return (string[])values.ToArray(typeof(string));
        }
    }
}
