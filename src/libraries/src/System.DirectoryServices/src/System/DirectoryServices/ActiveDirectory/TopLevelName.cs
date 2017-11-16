// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum TopLevelNameStatus
    {
        Enabled = 0,
        NewlyCreated = 1,
        AdminDisabled = 2,
        ConflictDisabled = 4
    }

    public class TopLevelName
    {
        private TopLevelNameStatus _status;
        internal readonly LARGE_INTEGER time;

        internal TopLevelName(int flag, LSA_UNICODE_STRING val, LARGE_INTEGER time)
        {
            _status = (TopLevelNameStatus)flag;
            Name = Marshal.PtrToStringUni(val.Buffer, val.Length / 2);
            this.time = time;
        }

        public string Name { get; }

        public TopLevelNameStatus Status
        {
            get => _status;
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
