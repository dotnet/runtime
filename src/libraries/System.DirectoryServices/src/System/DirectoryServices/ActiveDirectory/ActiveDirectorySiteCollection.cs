// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySiteCollection : CollectionBase
    {
        internal DirectoryEntry de = null;
        internal bool initialized = false;
        internal DirectoryContext context = null;

        internal ActiveDirectorySiteCollection() { }

        internal ActiveDirectorySiteCollection(ArrayList sites)
        {
            for (int i = 0; i < sites.Count; i++)
                Add((ActiveDirectorySite)sites[i]);
        }

        public ActiveDirectorySite this[int index]
        {
            get => (ActiveDirectorySite)InnerList[index];
            set
            {
                ActiveDirectorySite site = (ActiveDirectorySite)value;

                if (site == null)
                    throw new ArgumentNullException("value");

                if (!site.existing)
                    throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

                if (!Contains(site))
                    List[index] = site;
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , site), "value");
            }
        }

        public int Add(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            if (!site.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

            if (!Contains(site))
                return List.Add(site);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , site), "site");
        }

        public void AddRange(ActiveDirectorySite[] sites)
        {
            if (sites == null)
                throw new ArgumentNullException("sites");

            for (int i = 0; ((i) < (sites.Length)); i = ((i) + (1)))
                this.Add(sites[i]);
        }

        public void AddRange(ActiveDirectorySiteCollection sites)
        {
            if (sites == null)
                throw new ArgumentNullException("sites");

            int count = sites.Count;
            for (int i = 0; i < count; i++)
                this.Add(sites[i]);
        }

        public bool Contains(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            if (!site.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(ActiveDirectorySite[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            if (!site.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            if (!site.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

            if (!Contains(site))
                List.Insert(index, site);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , site), "site");
        }

        public void Remove(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            if (!site.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , site.Name));

            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collectio
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection , site), "site");
        }

        protected override void OnClearComplete()
        {
            // if the property exists, clear it out
            if (initialized)
            {
                try
                {
                    if (de.Properties.Contains("siteList"))
                        de.Properties["siteList"].Clear();
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        protected override void OnInsertComplete(int index, object value)
        {
            if (initialized)
            {
                ActiveDirectorySite site = (ActiveDirectorySite)value;
                string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);
                try
                {
                    de.Properties["siteList"].Add(dn);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            ActiveDirectorySite site = (ActiveDirectorySite)value;
            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);
            try
            {
                de.Properties["siteList"].Remove(dn);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            ActiveDirectorySite newsite = (ActiveDirectorySite)newValue;
            string newdn = (string)PropertyManager.GetPropertyValue(newsite.context, newsite.cachedEntry, PropertyManager.DistinguishedName);
            try
            {
                de.Properties["siteList"][index] = newdn;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnValidate(Object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (!(value is ActiveDirectorySite))
                throw new ArgumentException("value");

            if (!((ActiveDirectorySite)value).existing)
                throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted , ((ActiveDirectorySite)value).Name));
        }
    }
}
