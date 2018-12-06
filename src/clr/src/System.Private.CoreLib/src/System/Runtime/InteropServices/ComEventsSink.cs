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

using System;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    // see code:ComEventsHelper#ComEventsArchitecture
    internal class ComEventsSink : ICustomQueryInterface
    {
        #region private fields

        private Guid _iidSourceItf;
        private ComTypes.IConnectionPoint _connectionPoint;
        private int _cookie;
        private ComEventsMethod _methods;
        private ComEventsSink _next;

        #endregion


        #region ctor

        internal ComEventsSink(object rcw, Guid iid)
        {
            _iidSourceItf = iid;
            this.Advise(rcw);
        }

        #endregion


        #region static members

        internal static ComEventsSink Find(ComEventsSink sinks, ref Guid iid)
        {
            ComEventsSink sink = sinks;
            while (sink != null && sink._iidSourceItf != iid)
            {
                sink = sink._next;
            }

            return sink;
        }

        internal static ComEventsSink Add(ComEventsSink sinks, ComEventsSink sink)
        {
            sink._next = sinks;
            return sink;
        }

        internal static ComEventsSink RemoveAll(ComEventsSink sinks)
        {
            while (sinks != null)
            {
                sinks.Unadvise();
                sinks = sinks._next;
            }

            return null;
        }

        internal static ComEventsSink Remove(ComEventsSink sinks, ComEventsSink sink)
        {
            Debug.Assert(sinks != null, "removing event sink from empty sinks collection");
            Debug.Assert(sink != null, "specify event sink is null");

            if (sink == sinks)
            {
                sinks = sinks._next;
            }
            else
            {
                ComEventsSink current = sinks;
                while (current != null && current._next != sink)
                    current = current._next;

                if (current != null)
                {
                    current._next = sink._next;
                }
            }

            sink.Unadvise();

            return sinks;
        }

        #endregion


        #region public methods

        public ComEventsMethod RemoveMethod(ComEventsMethod method)
        {
            _methods = ComEventsMethod.Remove(_methods, method);
            return _methods;
        }

        public ComEventsMethod FindMethod(int dispid)
        {
            return ComEventsMethod.Find(_methods, dispid);
        }

        public ComEventsMethod AddMethod(int dispid)
        {
            ComEventsMethod method = new ComEventsMethod(dispid);
            _methods = ComEventsMethod.Add(_methods, method);
            return method;
        }

        #endregion

        private static Guid IID_IManagedObject = new Guid("{C3FCC19E-A970-11D2-8B5A-00A0C9B7C9C4}");

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (iid == _iidSourceItf || iid == typeof(IDispatch).GUID)
            {
                ppv = Marshal.GetComInterfaceForObject(this, typeof(IDispatch), CustomQueryInterfaceMode.Ignore);
                return CustomQueryInterfaceResult.Handled;
            }
            else if (iid == IID_IManagedObject)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        #region private methods


        private void Advise(object rcw)
        {
            Debug.Assert(_connectionPoint == null, "comevent sink is already advised");

            ComTypes.IConnectionPointContainer cpc = (ComTypes.IConnectionPointContainer)rcw;
            ComTypes.IConnectionPoint cp;
            cpc.FindConnectionPoint(ref _iidSourceItf, out cp);

            object sinkObject = this;

            cp.Advise(sinkObject, out _cookie);

            _connectionPoint = cp;
        }

        private void Unadvise()
        {
            Debug.Assert(_connectionPoint != null, "can not unadvise from empty connection point");

            try
            {
                _connectionPoint.Unadvise(_cookie);
                Marshal.ReleaseComObject(_connectionPoint);
            }
            catch (System.Exception)
            {
                // swallow all exceptions on unadvise
                // the host may not be available at this point
            }
            finally
            {
                _connectionPoint = null;
            }
        }

        #endregion
    };
}
