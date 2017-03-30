// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;
    using System.ComponentModel;

    public enum TopLevelNameStatus
    {
        Enabled = 0,
        NewlyCreated = 1,
        AdminDisabled = 2,
        ConflictDisabled = 4
    }

    public class TopLevelName
    {
        private string _name = null;
        private TopLevelNameStatus _status;
        internal LARGE_INTEGER time;

        internal TopLevelName(int flag, LSA_UNICODE_STRING val, LARGE_INTEGER time)
        {
            _status = (TopLevelNameStatus)flag;
            _name = Marshal.PtrToStringUni(val.Buffer, val.Length / 2);
            this.time = time;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public TopLevelNameStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value != TopLevelNameStatus.Enabled &&
                    value != TopLevelNameStatus.NewlyCreated &&
                    value != TopLevelNameStatus.AdminDisabled &&
                    value != TopLevelNameStatus.ConflictDisabled)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(TopLevelNameStatus));

                _status = value;
            }
        }
    }
}
