// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySiteLinkCollection : CollectionBase
    {
        internal DirectoryEntry? de;
        internal bool initialized;
        internal DirectoryContext? context;

        internal ActiveDirectorySiteLinkCollection() { }

        public ActiveDirectorySiteLink this[int index]
        {
            get => (ActiveDirectorySiteLink)InnerList[index]!;
            set
            {
                ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;

                if (link == null)
                    throw new ArgumentNullException(nameof(value));

                if (!link.existing)
                    throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

                if (!Contains(link))
                    List[index] = link;
                else
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, link), nameof(value));
            }
        }

        public int Add(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

            if (!Contains(link))
                return List.Add(link);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, link), nameof(link));
        }

        public void AddRange(ActiveDirectorySiteLink[] links)
        {
            if (links == null)
                throw new ArgumentNullException(nameof(links));

            for (int i = 0; i < links.Length; i = i + 1)
                this.Add(links[i]);
        }

        public void AddRange(ActiveDirectorySiteLinkCollection links)
        {
            if (links == null)
                throw new ArgumentNullException(nameof(links));

            int count = links.Count;
            for (int i = 0; i < count; i++)
                this.Add(links[i]);
        }

        public bool Contains(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName)!;

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i]!;
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName)!;

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
                throw new ArgumentNullException(nameof(link));

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName)!;

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i]!;
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName)!;

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
                throw new ArgumentNullException(nameof(link));

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

            if (!Contains(link))
                List.Insert(index, link);
            else
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, link), nameof(link));
        }

        public void Remove(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            if (!link.existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, link.Name));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName)!;

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i]!;
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName)!;

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }

            // something that does not exist in the collectio
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection, link), nameof(link));
        }

        protected override void OnClearComplete()
        {
            // if the property exists, clear it out
            if (initialized)
            {
                try
                {
                    if (de!.Properties.Contains("siteLinkList"))
                        de.Properties["siteLinkList"].Clear();
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overriden member
        protected override void OnInsertComplete(int index, object value)
#pragma warning restore CS8765
        {
            if (initialized)
            {
                ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;
                string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName)!;
                try
                {
                    de!.Properties["siteLinkList"].Add(dn);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overriden member
        protected override void OnRemoveComplete(int index, object value)
#pragma warning restore CS8765
        {
            ActiveDirectorySiteLink link = (ActiveDirectorySiteLink)value;
            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName)!;
            try
            {
                de!.Properties["siteLinkList"].Remove(dn);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overriden member
        protected override void OnSetComplete(int index, object oldValue, object newValue)
#pragma warning restore CS8765
        {
            ActiveDirectorySiteLink newLink = (ActiveDirectorySiteLink)newValue!;
            string newdn = (string)PropertyManager.GetPropertyValue(newLink.context, newLink.cachedEntry, PropertyManager.DistinguishedName)!;
            try
            {
                de!.Properties["siteLinkList"][index] = newdn;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        protected override void OnValidate(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (!(value is ActiveDirectorySiteLink))
                throw new ArgumentException(null, nameof(value));

            if (!((ActiveDirectorySiteLink)value).existing)
                throw new InvalidOperationException(SR.Format(SR.SiteLinkNotCommitted, ((ActiveDirectorySiteLink)value).Name));
        }
    }
}
