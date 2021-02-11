// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;

namespace MonoTests.Common
{
    internal class PokerChangeMonitor : ChangeMonitor
    {
        private List<string> _calls;
        private string _uniqueId;

        public List<string> Calls
        {
            get
            {
                if (_calls == null)
                    _calls = new List<string>();

                return _calls;
            }
        }

        public override string UniqueId
        {
            get { return _uniqueId; }
        }

        public PokerChangeMonitor()
        {
            _uniqueId = "UniqueID";
            InitializationComplete();
        }

        public void SignalChange()
        {
            OnChanged(null);
        }

        protected override void Dispose(bool disposing)
        {
            Calls.Add("Dispose (bool disposing)");
        }
    }
}
