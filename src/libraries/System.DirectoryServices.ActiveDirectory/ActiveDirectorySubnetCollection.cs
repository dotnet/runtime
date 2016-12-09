//------------------------------------------------------------------------------
// <copyright file="ActiveDirectorySubnetCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;
    using System.Text;

    public class ActiveDirectorySubnetCollection :CollectionBase {
        internal Hashtable changeList = null;
        internal bool initialized = false;
        private string siteDN = null;
        private DirectoryContext context = null;
        private ArrayList copyList = new ArrayList();

        internal ActiveDirectorySubnetCollection(DirectoryContext context, string siteDN) 
        {
            this.context = context;
            this.siteDN = siteDN;

            Hashtable tempTable = new Hashtable();
            changeList= Hashtable.Synchronized(tempTable);
        }
        
        public ActiveDirectorySubnet this[int index] {
            get {
                return (ActiveDirectorySubnet) InnerList[index];                                                 
            }
            set {
                ActiveDirectorySubnet subnet = (ActiveDirectorySubnet) value;

                if(subnet == null)
                    throw new ArgumentNullException("value");

                if(!subnet.existing)
                    throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name));                  

                if(!Contains(subnet))
                    List[index] = subnet; 
                else
                    throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, subnet), "value");  
            }
        }

        public int Add(ActiveDirectorySubnet subnet)
        {
            if(subnet == null)
                throw new ArgumentNullException("subnet");

            if(!subnet.existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name));     
            
            if(!Contains(subnet))
                return List.Add(subnet);
            else
                throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, subnet), "subnet");
        }

        public void AddRange(ActiveDirectorySubnet[] subnets)
        {
            if(subnets == null)
                throw new ArgumentNullException("subnets");

            foreach (ActiveDirectorySubnet s in subnets)
            {
                if (s == null)
                {
                    throw new ArgumentException("subnets");
                }
            }

            for (int i = 0; ((i) < (subnets.Length)); i = ((i) + (1))) 
                this.Add(subnets[i]);
        }        

        public void AddRange(ActiveDirectorySubnetCollection subnets)
        {
            if(subnets == null)
                throw new ArgumentNullException("subnets");

            int count =subnets.Count;
            for(int i = 0; i < count; i++)
                this.Add(subnets[i]);
        }

        public bool Contains(ActiveDirectorySubnet subnet) {
            if(subnet == null)
                throw new ArgumentNullException("subnet");

            if(!subnet.existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name)); 
            
            string dn = (string) PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);
            
            for(int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet) InnerList[i];
                string tmpDn = (string) PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);
                
                if(Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(ActiveDirectorySubnet[] array, int index) {            
            List.CopyTo(array, index);
        }

        public int IndexOf(ActiveDirectorySubnet subnet) {            
            if(subnet == null)
                throw new ArgumentNullException("subnet");

            if(!subnet.existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name)); 

            string dn = (string) PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);
            
            for(int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet) InnerList[i];
                string tmpDn = (string) PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);
                
                if(Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, ActiveDirectorySubnet subnet) {
            if(subnet == null)
                throw new ArgumentNullException("subnet");

            if(!subnet.existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name)); 

            if(!Contains(subnet))
                List.Insert(index, subnet);      
            else
                throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, subnet), "subnet");            
        }

        public void Remove(ActiveDirectorySubnet subnet) {
            if(subnet == null)
                throw new ArgumentNullException("subnet");

            if(!subnet.existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, subnet.Name)); 

            string dn = (string) PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);
            
            for(int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySubnet tmp = (ActiveDirectorySubnet) InnerList[i];
                string tmpDn = (string) PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);
                
                if(Utils.Compare(tmpDn, dn) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collectio
            throw new ArgumentException(Res.GetString(Res.NotFoundInCollection, subnet), "subnet");
        }        

        protected override void OnClear()
        {            
            if(initialized)
            {
                copyList.Clear();
                foreach(object o in List)
                {
                    copyList.Add(o);                    
                }
            }
        }
        
        protected override void OnClearComplete() {
            // if the property exists, clear it out
            if(initialized)
            {
                for(int i = 0; i < copyList.Count; i++)
                {
                    OnRemoveComplete(i, copyList[i]);                    
                }                
            }
        }

        protected override void OnInsertComplete(int index, object value) {        
            if(initialized)
            {
                ActiveDirectorySubnet subnet = (ActiveDirectorySubnet) value;
                string dn = (string) PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

                try
                {
                    if(changeList.Contains(dn))
                    {
                        ((DirectoryEntry) changeList[dn]).Properties["siteObject"].Value = this.siteDN;
                    }
                    else
                    {
                        DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, MakePath(dn));                    
                        de.Properties["siteObject"].Value = this.siteDN;
                        changeList.Add(dn, de);
                    }
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                
            }
        }

        protected override void OnRemoveComplete(int index, object value) {
            ActiveDirectorySubnet subnet = (ActiveDirectorySubnet) value;
            string dn = (string) PropertyManager.GetPropertyValue(subnet.context, subnet.cachedEntry, PropertyManager.DistinguishedName);

            try
            {
                if(changeList.Contains(dn))
                {
                    ((DirectoryEntry) changeList[dn]).Properties["siteObject"].Clear();
                }
                else
                {
                    DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, MakePath(dn));
                    de.Properties["siteObject"].Clear();
                    changeList.Add(dn, de);
                }               
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue) {
            OnRemoveComplete(index, oldValue);
            OnInsertComplete(index, newValue);              
        }    


        protected override void OnValidate(Object value) {
            if (value == null) throw new ArgumentNullException("value");

            if(!(value is ActiveDirectorySubnet))
                throw new ArgumentException("value");         

            if(!((ActiveDirectorySubnet) value).existing)
                throw new InvalidOperationException(Res.GetString(Res.SubnetNotCommitted, ((ActiveDirectorySubnet) value).Name));
        }

        private string MakePath(string subnetDN)
        {
            string rdn = Utils.GetRdnFromDN(subnetDN);
            StringBuilder str = new StringBuilder();
            for(int i = 0; i < rdn.Length; i++)
            {
                if(rdn[i] == '/')
                {
                    str.Append('\\');
                }

                str.Append(rdn[i]);
            }

            return str.ToString() + "," + subnetDN.Substring(rdn.Length + 1);
        }
    }
}
