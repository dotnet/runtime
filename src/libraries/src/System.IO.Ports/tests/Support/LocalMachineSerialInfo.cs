// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Legacy.Support
{
    [Serializable()]
    public class LocalMachineSerialInfo
    {
        private readonly string _firstAvailablePortName;
        private readonly string _secondAvailablePortName;
        private readonly bool _nullModemPresent;
        private readonly string _loopbackPortName;

        public LocalMachineSerialInfo(string firstAvailablePortName, string secondAvailablePortName, string loopBackPortName, bool nullModemPresent)
        {
            _firstAvailablePortName = firstAvailablePortName;
            _secondAvailablePortName = secondAvailablePortName;
            _loopbackPortName = loopBackPortName;
            _nullModemPresent = nullModemPresent;
        }

        public string FirstAvailablePortName
        {
            get
            {
                return _firstAvailablePortName;
            }
        }

        public string SecondAvailablePortName
        {
            get
            {
                return _secondAvailablePortName;
            }
        }

        public bool NullModemPresent
        {
            get
            {
                return _nullModemPresent;
            }
        }

        public string LoopbackPortName
        {
            get
            {
                return _loopbackPortName;
            }
        }
    }
}
