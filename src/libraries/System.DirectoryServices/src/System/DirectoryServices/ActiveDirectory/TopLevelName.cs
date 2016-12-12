//------------------------------------------------------------------------------
// <copyright file="TopLevelName.cs" company="Microsoft">
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
    using System.ComponentModel;

    public enum TopLevelNameStatus {
        Enabled = 0,
        NewlyCreated = 1,
        AdminDisabled = 2,
        ConflictDisabled = 4
    }

    public class TopLevelName {
        private string name = null;
        private TopLevelNameStatus status;
        internal LARGE_INTEGER time;
        
        internal TopLevelName(int flag, LSA_UNICODE_STRING val, LARGE_INTEGER time) 
        {
            status = (TopLevelNameStatus) flag;            
            name = Marshal.PtrToStringUni(val.Buffer, val.Length/2);
            this.time = time;
        }

        public string Name {
            get {
                return name;
            }
        }

        public TopLevelNameStatus Status {
            get {
                return status;
            }
            set {
                if (value != TopLevelNameStatus.Enabled &&
                    value != TopLevelNameStatus.NewlyCreated &&
                    value != TopLevelNameStatus.AdminDisabled &&
                    value != TopLevelNameStatus.ConflictDisabled) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(TopLevelNameStatus));

                status = value;
            }
        }        
    }
}
