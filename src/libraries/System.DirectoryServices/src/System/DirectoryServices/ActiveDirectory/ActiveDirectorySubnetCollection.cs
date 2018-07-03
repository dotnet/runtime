// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySubnetCollection : CollectionBase
    {
        internal readonly Hashtable changeList = null;
        internal bool initialized = false;
        private readonly string _siteDN = null;
        private readonly DirectoryContext _context = null;
        private readonly ArrayList _copyList = new ArrayList();

        internal ActiveDirectorySubnetCollection(DirectoryContext context, string siteDN)
        {
            _context = context;
            _siteDN = siteDN;

            Hashtable tempTable = new Hashtable();
            changeList = Hashtable.Synchronized(tempTable);
        }

        public ActiveDirectorySubnet this[int index]
        {
            get => (ActiveDirectorySubnet)InnerList[index];
            set
            {
                ActiveDirectorySubnet subnet = (ActiveDirectorySubnet)value;

                if (subnet == null)
                    throw new ArgumentNullException(nameof(value));

                if (!subnet.existing)
                    throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

                if (!Contains(subnet))
                    List[index] = subnet;
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , subnet), nameof(value));
            }
        }

        public int Add(ActiveDirectorySubnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));

            if (!subnet.existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

            if (!Contains(subnet))
                return List.Add(subnet);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , subnet), nameof(subnet));
        }

        public void AddRange(ActiveDirectorySubnet[] subnets)
        {
            if (subnets == null)
                throw new ArgumentNullException(nameof(subnets));

            foreach (ActiveDirectorySubnet s in subnets)
            {
                if (s == null)
                {
                    throw new ArgumentException(nameof(subnets));
                }
            }

            for (int i = 0; ((i) < (subnets.Length)); i = ((i) + (1)))
                this.Add(subnets[i]);
        }

        public void AddRange(ActiveDirectorySubnetCollection subnets)
        {
            if (subnets == null)
                throw new ArgumentNullException(nameof(subnets));

            int count = subnets.Count;
            for (int i = 0; i < count; i++)
                this.Add(subnets[i]);
        }

        public bool Contains(ActiveDirectorySubnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));

            if (!subnet.existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

            string dn = (string)PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(ActiveDirectorySubnet[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(ActiveDirectorySubnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));

            if (!subnet.existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

            string dn = (string)PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, ActiveDirectorySubnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));

            if (!subnet.existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

            if (!Contains(subnet))
                List.Insert(index, subnet);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , subnet), nameof(subnet));
        }

        public void Remove(ActiveDirectorySubnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));

            if (!subnet.existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , subnet.Name));

            string dn = (string)PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collectio
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection , subnet), nameof(subnet));
        }

        protected override void OnClear()
        {
            if (initialized)
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
            if (initialized)
            {
                for (int i = 0; i < _copyList.Count; i++)
                {
                    OnRemoveComplete(i, _copyList[i]);
                }
            }
        }

        protected override void OnInsertComplete(int index, object value)
        {
            if (initialized)
            {
                ActiveDirectorySubnet subnet = (ActiveDirectorySubnet)value;
                string dn = (string)PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

                try
                {
                    if (changeList.Contains(dn))
                    {
                        ((DirectoryEntry)changeList[dn]).Properties["siteObject"].Value = _siteDN;
                    }
                    else
                    {
                        DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(_context, MakePath(dn));
                        de.Properties["siteObject"].Value = _siteDN;
                        changeList.Add(dn, de);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            ActiveDirectorySubnet subnet = (ActiveDirectorySubnet)value;
            string dn = (string)PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

            try
            {
                if (changeList.Contains(dn))
                {
                    ((DirectoryEntry)changeList[dn]).Properties["siteObject"].Clear();
                }
                else
                {
                    DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(_context, MakePath(dn));
                    de.Properties["siteObject"].Clear();
                    changeList.Add(dn, de);
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
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

            if (!(value is ActiveDirectorySubnet))
                throw new ArgumentException(nameof(value));

            if (!((ActiveDirectorySubnet)value).existing)
                throw new InvalidOperationException(SR.Format(SR.SubnetNotCommitted , ((ActiveDirectorySubnet)value).Name));
        }

        private string MakePath(string subnetDN)
        {
            string rdn = Utils.GetRdnFromDN(subnetDN);
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < rdn.Length; i++)
            {
                if (rdn[i] == '/')
                {
                    str.Append('\\');
                }

                str.Append(rdn[i]);
            }

            return str.ToString() + "," + subnetDN.Substring(rdn.Length + 1);
        }
    }
}
