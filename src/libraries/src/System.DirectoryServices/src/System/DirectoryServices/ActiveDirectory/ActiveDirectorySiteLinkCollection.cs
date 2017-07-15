// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySiteLinkCollection : CollectionBase
    {
        internal DirectoryEntry de = null;
        internal bool initialized = false;
        internal DirectoryContext context = null;

        internal ActiveDirectorySiteLinkCollection() { }

        public ActiveDirectorySiteLink this[int index]
        {
            get => (ActiveDirectorySiteLink)InnerList[index];
            set
            {
                ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;

                if (link == null)
                    throw new ArgumentNullException("value");

                if (!link.existing)
                    throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

                if (!Contains(link))
                    List[index] = link;
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , link), "value");
            }
        }

        public int Add(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException("link");

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

            if (!Contains(link))
                return List.Add(link);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , link), "link");
        }

        public void AddRange(ActiveDirectorySiteLink[] links)
        {
            if (links == null)
                throw new ArgumentNullException("links");

            for (int i = 0; i < links.Length; i = i + 1)
                this.Add(links[i]);
        }

        public void AddRange(ActiveDirectorySiteLinkCollection links)
        {
            if (links == null)
                throw new ArgumentNullException("links");

            int count = links.Count;
            for (int i = 0; i < count; i++)
                this.Add(links[i]);
        }

        public bool Contains(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException("link");

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(ActiveDirectorySiteLink[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException("link");

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException("value");

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

            if (!Contains(link))
                List.Insert(index, link);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection , link), "link");
        }

        public void Remove(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException("link");

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collectio
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection , link), "link");
        }

        protected override void OnClearComplete()
        {
            // if the property exists, clear it out
            if (initialized)
            {
                try
                {
                    if (de.Properties.Contains("siteLinkList"))
                        de.Properties["siteLinkList"].Clear();
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
                ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;
                string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);
                try
                {
                    de.Properties["siteLinkList"].Add(dn);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;
            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);
            try
            {
                de.Properties["siteLinkList"].Remove(dn);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            ActiveDirectorySiteLink newLink = (ActiveDirectorySiteLink)newValue;
            string newdn = (string)PropertyManager.GetPropertyValue(newLink.context, newLink.cachedEntry, PropertyManager.DistinguishedName);
            try
            {
                de.Properties["siteLinkList"][index] = newdn;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnValidate(Object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            if (!(value is ActiveDirectorySiteLink))
                throw new ArgumentException("value");

            if (!((ActiveDirectorySiteLink)value).existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted , ((ActiveDirectorySiteLink)value).Name));
        }
    }
}
