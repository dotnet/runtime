// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    ///  Encapsulates the information for fast access to Event Values
    ///  of an EventLogRecord.  An instance of this class is constructed
    ///  and then passed to EventLogRecord.GetEventPropertyValues.
    /// </summary>
    public class EventLogPropertySelector : IDisposable
    {
        public EventLogPropertySelector(IEnumerable<string> propertyQueries)
        {
            if (propertyQueries == null)
                throw new ArgumentNullException(nameof(propertyQueries));

            string[] paths;

            ICollection<string> coll = propertyQueries as ICollection<string>;
            if (coll != null)
            {
                paths = new string[coll.Count];
                coll.CopyTo(paths, 0);
            }
            else
            {
                List<string> queries;
                queries = new List<string>(propertyQueries);
                paths = queries.ToArray();
            }

            Handle = NativeWrapper.EvtCreateRenderContext(paths.Length, paths, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextValues);
        }

        internal EventLogHandle Handle { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Handle != null && !Handle.IsInvalid)
                Handle.Dispose();
        }
    }
}
