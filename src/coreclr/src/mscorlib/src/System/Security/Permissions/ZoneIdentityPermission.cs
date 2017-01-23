// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// 

namespace System.Security.Permissions
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class ZoneIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------

        // Zone            Enum       Flag
        // -----           -----      -----
        // NoZone          -1         0x00
        // MyComputer       0         0x01  (1 << 0)
        // Intranet         1         0x02  (1 << 1)
        // Trusted          2         0x04  (1 << 2)
        // Internet         3         0x08  (1 << 3)
        // Untrusted        4         0x10  (1 << 4)

        private const uint AllZones = 0x1f;
        [OptionalField(VersionAdded = 2)]
        private uint m_zones;

        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------

        public ZoneIdentityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_zones = AllZones;
            }
            else if (state == PermissionState.None)
            {
                m_zones = 0;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public ZoneIdentityPermission( SecurityZone zone )
        {
            this.SecurityZone = zone;
        }

        internal ZoneIdentityPermission( uint zones )
        {
            m_zones = (zones & AllZones);
        }

        // Internal function to append all the Zone in this permission to the input ArrayList
        internal void AppendZones(ArrayList zoneList)
        {
            int nEnum = 0;
            uint nFlag;
            for(nFlag = 1; nFlag < AllZones; nFlag <<= 1)
            {
                if((m_zones & nFlag) != 0)
                {
                    zoneList.Add((SecurityZone)nEnum);
                }
                nEnum++;
            }
        }

        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------

        public SecurityZone SecurityZone
        {
            set
            {
                VerifyZone( value );
                if(value == SecurityZone.NoZone)
                    m_zones = 0;
                else
                    m_zones = (uint)1 << (int)value;
            }

            get
            {
                SecurityZone z = SecurityZone.NoZone;
                int nEnum = 0;
                uint nFlag;
                for(nFlag = 1; nFlag < AllZones; nFlag <<= 1)
                {
                    if((m_zones & nFlag) != 0)
                    {
                        if(z == SecurityZone.NoZone)
                            z = (SecurityZone)nEnum;
                        else
                            return SecurityZone.NoZone;
                    }
                    nEnum++;
                }
                return z;
            }
        }

        //------------------------------------------------------
        //
        // PRIVATE AND PROTECTED HELPERS FOR ACCESSORS AND CONSTRUCTORS
        //
        //------------------------------------------------------

        private static void VerifyZone( SecurityZone zone )
        {
            if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
            {
                throw new ArgumentException( Environment.GetResourceString("Argument_IllegalZone") );
            }
            Contract.EndContractBlock();
        }


        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------


        public override IPermission Copy()
        {
            return new ZoneIdentityPermission(this.m_zones);
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return this.m_zones == 0;

            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return (this.m_zones & that.m_zones) == this.m_zones;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;

            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            uint newZones = this.m_zones & that.m_zones;
            if(newZones == 0)
                return null;
            return new ZoneIdentityPermission(newZones);
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return this.m_zones != 0 ? this.Copy() : null;

            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return new ZoneIdentityPermission(this.m_zones | that.m_zones);
        }

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return ZoneIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.ZoneIdentityPermissionIndex;
        }

    }
}
