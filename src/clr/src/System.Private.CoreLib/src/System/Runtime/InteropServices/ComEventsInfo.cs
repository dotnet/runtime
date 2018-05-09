// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
**
** Purpose: part of ComEventHelpers APIs which allow binding 
** managed delegates to COM's connection point based events.
**
**/

namespace System.Runtime.InteropServices
{
    using System;
    using ComTypes = System.Runtime.InteropServices.ComTypes;

    // see code:ComEventsHelper#ComEventsArchitecture
    internal class ComEventsInfo
    {
        #region fields

        private ComEventsSink _sinks;
        private object _rcw;

        #endregion


        #region ctor/dtor

        private ComEventsInfo(object rcw)
        {
            _rcw = rcw;
        }

        ~ComEventsInfo()
        {
            // see code:ComEventsHelper#ComEventsFinalization
            _sinks = ComEventsSink.RemoveAll(_sinks);
        }

        #endregion


        #region static methods

        internal static ComEventsInfo Find(object rcw)
        {
            return (ComEventsInfo)Marshal.GetComObjectData(rcw, typeof(ComEventsInfo));
        }

        // it is caller's responsibility to call this method under lock(rcw)
        internal static ComEventsInfo FromObject(object rcw)
        {
            ComEventsInfo eventsInfo = Find(rcw);
            if (eventsInfo == null)
            {
                eventsInfo = new ComEventsInfo(rcw);
                Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), eventsInfo);
            }
            return eventsInfo;
        }

        #endregion


        #region internal methods

        internal ComEventsSink FindSink(ref Guid iid)
        {
            return ComEventsSink.Find(_sinks, ref iid);
        }

        // it is caller's responsibility to call this method under lock(rcw)
        internal ComEventsSink AddSink(ref Guid iid)
        {
            ComEventsSink sink = new ComEventsSink(_rcw, iid);
            _sinks = ComEventsSink.Add(_sinks, sink);

            return _sinks;
        }

        // it is caller's responsibility to call this method under lock(rcw)
        internal ComEventsSink RemoveSink(ComEventsSink sink)
        {
            _sinks = ComEventsSink.Remove(_sinks, sink);
            return _sinks;
        }

        #endregion
    }
}
