// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Configuration
{
    /// <summary>
    /// Event args for the SettingChanging event.
    /// </summary>
    public class SettingChangingEventArgs : CancelEventArgs
    {
        private string _settingClass;
        private string _settingName;
        private string _settingKey;
        private object _newValue;

        public SettingChangingEventArgs(string settingName, string settingClass, string settingKey, object newValue, bool cancel) : base(cancel)
        {
            _settingName = settingName;
            _settingClass = settingClass;
            _settingKey = settingKey;
            _newValue = newValue;
        }

        public object NewValue
        {
            get
            {
                return _newValue;
            }
        }

        public string SettingClass
        {
            get
            {
                return _settingClass;
            }
        }

        public string SettingName
        {
            get
            {
                return _settingName;
            }
        }

        public string SettingKey
        {
            get
            {
                return _settingKey;
            }
        }
    }
}
