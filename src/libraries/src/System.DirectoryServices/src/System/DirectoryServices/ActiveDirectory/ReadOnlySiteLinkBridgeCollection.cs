// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlySiteLinkBridgeCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlySiteLinkBridgeCollection() { }

        public ActiveDirectorySiteLinkBridge this[int index]
        {
            get => (ActiveDirectorySiteLinkBridge)InnerList[index];
        }

        public bool Contains(ActiveDirectorySiteLinkBridge bridge)
        {
            if (bridge == null)
                throw new ArgumentNullException("bridge");

            string dn = (string)PropertyManager.GetPropertyValue(bridge.context, bridge.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLinkBridge tmp = (ActiveDirectorySiteLinkBridge)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ActiveDirectorySiteLinkBridge bridge)
        {
            if (bridge == null)
                throw new ArgumentNullException("bridge");

            string dn = (string)PropertyManager.GetPropertyValue(bridge.context, bridge.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLinkBridge tmp = (ActiveDirectorySiteLinkBridge)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ActiveDirectorySiteLinkBridge[] bridges, int index)
        {
            InnerList.CopyTo(bridges, index);
        }

        internal int Add(ActiveDirectorySiteLinkBridge bridge) => InnerList.Add(bridge);

        internal void Clear() => InnerList.Clear();
    }
}
