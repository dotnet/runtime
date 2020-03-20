using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.OleDb
{
    internal interface ItagDBPROPINFO
    {
        int dwPropertyID { get; }
        int dwFlags { get; }
        int vtType { get; }
        object vValue { get; }
        string pwszDescription { get; }
    }

    internal interface ItagDBPROP
    {
        OleDbPropertyStatus dwStatus { get; }
        object vValue { get; }
        int dwPropertyID { get; }
    }

    internal sealed partial class tagDBPROPINFO_x86 : ItagDBPROPINFO
    {
        int ItagDBPROPINFO.dwPropertyID => this.dwPropertyID;

        int ItagDBPROPINFO.dwFlags => this.dwFlags;

        int ItagDBPROPINFO.vtType => this.vtType;

        object ItagDBPROPINFO.vValue => this.vValue;

        string ItagDBPROPINFO.pwszDescription => this.pwszDescription;
    }

    internal sealed partial class tagDBPROPINFO : ItagDBPROPINFO
    {
        int ItagDBPROPINFO.dwPropertyID => this.dwPropertyID;

        int ItagDBPROPINFO.dwFlags => this.dwFlags;

        int ItagDBPROPINFO.vtType => this.vtType;

        object ItagDBPROPINFO.vValue => this.vValue;

        string ItagDBPROPINFO.pwszDescription => this.pwszDescription;

    }

    internal sealed partial class tagDBPROP_x86 : ItagDBPROP
    {
        OleDbPropertyStatus ItagDBPROP.dwStatus => this.dwStatus;

        object ItagDBPROP.vValue => this.vValue;

        int ItagDBPROP.dwPropertyID => this.dwPropertyID;
    }

    internal sealed partial class tagDBPROP : ItagDBPROP
    {
        OleDbPropertyStatus ItagDBPROP.dwStatus => this.dwStatus;

        object ItagDBPROP.vValue => this.vValue;

        int ItagDBPROP.dwPropertyID => this.dwPropertyID;
    }

    internal static class ArchitectureSpecificHelpers
    {
        internal static ItagDBPROPINFO CreateTagDbPropInfo()
        {
            if (ODB.IsRunningOnX86)
            {
                return new tagDBPROPINFO_x86();
            }
            else
            {
                return new tagDBPROPINFO();
            }
        }

        internal static ItagDBPROP CreateTagDbProp(int propertyID, bool required, object value)
        {
            if (ODB.IsRunningOnX86)
            {
                return new tagDBPROP_x86(propertyID, required, value);
            }
            else
            {
                return new tagDBPROP(propertyID, required, value);
            }
        }

        internal static ItagDBPROP CreateTagDbProp()
        {
            if (ODB.IsRunningOnX86)
            {
                return new tagDBPROP_x86();
            }
            else
            {
                return new tagDBPROP();
            }
        }
    }
}
