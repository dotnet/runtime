// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace System.Diagnostics
{
    public class SourceSwitch : Switch
    {
        public SourceSwitch(string name) : base(name, String.Empty) { }

        public SourceSwitch(string displayName, string defaultSwitchValue)
            : base(displayName, String.Empty, defaultSwitchValue)
        { }

        public SourceLevels Level
        {
            get
            {
                return (SourceLevels)SwitchSetting;
            }
            set
            {
                SwitchSetting = (int)value;
            }
        }

        public bool ShouldTrace(TraceEventType eventType)
        {
            return (SwitchSetting & (int)eventType) != 0;
        }

        protected override void OnValueChanged()
        {
            SwitchSetting = (int)Enum.Parse(typeof(SourceLevels), Value, true);
        }
    }
}
