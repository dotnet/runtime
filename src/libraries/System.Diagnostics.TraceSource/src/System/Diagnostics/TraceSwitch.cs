// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Provides a multi-level switch to enable or disable tracing
    ///       and debug output for a compiled application or framework.</para>
    /// </devdoc>
    [SwitchLevel(typeof(TraceLevel))]
    public class TraceSwitch : Switch
    {
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TraceSwitch'/> class.</para>
        /// </devdoc>
        public TraceSwitch(string displayName, string? description)
            : base(displayName, description)
        { }

        public TraceSwitch(string displayName, string? description, string defaultSwitchValue)
            : base(displayName, description, defaultSwitchValue)
        { }

        /// <devdoc>
        ///    <para>Gets or sets the trace
        ///       level that specifies what messages to output for tracing and debugging.</para>
        /// </devdoc>
        public TraceLevel Level
        {
            get
            {
                return (TraceLevel)SwitchSetting;
            }

            set
            {
                if (value < TraceLevel.Off || value > TraceLevel.Verbose)
                    throw new ArgumentException(SR.TraceSwitchInvalidLevel);
                SetSwitchValues((int)value, value.ToString());
            }
        }

        /// <devdoc>
        ///    <para>Gets a value
        ///       indicating whether the <see cref='System.Diagnostics.TraceSwitch.Level'/> is set to
        ///    <see langword='Error'/>, <see langword='Warning'/>, <see langword='Info'/>, or
        ///    <see langword='Verbose'/>.</para>
        /// </devdoc>
        public bool TraceError => Level >= TraceLevel.Error;

        /// <devdoc>
        ///    <para>Gets a value
        ///       indicating whether the <see cref='System.Diagnostics.TraceSwitch.Level'/> is set to
        ///    <see langword='Warning'/>, <see langword='Info'/>, or <see langword='Verbose'/>.</para>
        /// </devdoc>
        public bool TraceWarning => Level >= TraceLevel.Warning;

        /// <devdoc>
        ///    <para>Gets a value
        ///       indicating whether the <see cref='System.Diagnostics.TraceSwitch.Level'/> is set to
        ///    <see langword='Info'/> or <see langword='Verbose'/>.</para>
        /// </devdoc>
        public bool TraceInfo => Level >= TraceLevel.Info;

        /// <devdoc>
        ///    <para>Gets a value
        ///       indicating whether the <see cref='System.Diagnostics.TraceSwitch.Level'/> is set to
        ///    <see langword='Verbose'/>.</para>
        /// </devdoc>
        public bool TraceVerbose => Level == TraceLevel.Verbose;

        /// <devdoc>
        ///    <para>
        ///       Update the level for this switch.
        ///    </para>
        /// </devdoc>
        protected override void OnSwitchSettingChanged()
        {
            int level = SwitchSetting;
            if (level < (int)TraceLevel.Off)
            {
                Trace.WriteLine(SR.Format(SR.TraceSwitchLevelTooLow, DisplayName));
                SwitchSetting = (int)TraceLevel.Off;
            }
            else if (level > (int)TraceLevel.Verbose)
            {
                Trace.WriteLine(SR.Format(SR.TraceSwitchLevelTooHigh, DisplayName));
                SwitchSetting = (int)TraceLevel.Verbose;
            }
        }

        protected override void OnValueChanged()
        {
            // Parse the new value and set the switch setting accordingly. Parse should throw an exception if the value is invalid.
            TraceLevel level = Enum.Parse<TraceLevel>(Value, true);

            if (level < TraceLevel.Off)
            {
                level = TraceLevel.Off;
            }
            else if (level > TraceLevel.Verbose)
            {
                level = TraceLevel.Verbose;
            }


            // Update the switch value string accordingly.
            SetSwitchValues((int)level, level.ToString());
        }
    }
}
