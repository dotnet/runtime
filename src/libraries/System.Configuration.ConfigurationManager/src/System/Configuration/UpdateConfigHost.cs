// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;

namespace System.Configuration
{
    // Configuration host that intercepts calls to filename functions
    // to support SaveAs to an alternate file stream.
    internal sealed class UpdateConfigHost : DelegatingConfigHost
    {
        private HybridDictionary _streams; // oldStreamname -> StreamUpdate

        internal UpdateConfigHost(IInternalConfigHost host)
        {
            // Delegate to the host provided.
            Host = host;
        }

        // Add a stream to the list of streams to intercept.
        //
        // Parameters:
        //  alwaysIntercept -   If true, then don't check whether the old stream and the new stream are the same.
        //                      SaveAs() will set this to true if oldStreamname is actually referring to a stream
        //                      on a remote machine.
        internal void AddStreamname(string oldStreamname, string newStreamname, bool alwaysIntercept)
        {
            // After reviewing all the code paths, oldStreamname shouldn't be Null or Empty.
            // It actually doesn't make much sense if we're asked to intercept an null or empty stream.
            Debug.Assert(!string.IsNullOrEmpty(oldStreamname));

            if (string.IsNullOrEmpty(oldStreamname)) return;

            if (!alwaysIntercept && StringUtil.EqualsIgnoreCase(oldStreamname, newStreamname)) return;

            if (_streams == null) _streams = new HybridDictionary(true);

            _streams[oldStreamname] = new StreamUpdate(newStreamname);
        }

        // Get the new stream name for a stream if a new name exists, otherwise
        // return the original stream name.
        internal string GetNewStreamname(string oldStreamname)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(oldStreamname, false);
            return streamUpdate != null ? streamUpdate.NewStreamname : oldStreamname;
        }

        // Get the StreamUpdate for a stream.
        // If alwaysIntercept is true, then the StreamUpdate is
        // always returned if it exists.
        // If alwaysIntercept is false, then only return the StreamUpdate
        // if the new stream has been successfully written to.
        private StreamUpdate GetStreamUpdate(string oldStreamname, bool alwaysIntercept)
        {
            if (_streams == null)
                return null;

            StreamUpdate streamUpdate = (StreamUpdate)_streams[oldStreamname];
            if ((streamUpdate != null) && !alwaysIntercept && !streamUpdate.WriteCompleted) streamUpdate = null;

            return streamUpdate;
        }

        public override object GetStreamVersion(string streamName)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, false);
            return streamUpdate != null
                ? InternalConfigHost.StaticGetStreamVersion(streamUpdate.NewStreamname)
                : Host.GetStreamVersion(streamName);
        }

        public override Stream OpenStreamForRead(string streamName)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, false);
            return streamUpdate != null
                ? InternalConfigHost.StaticOpenStreamForRead(streamUpdate.NewStreamname)
                : Host.OpenStreamForRead(streamName);
        }

        public override Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
        {
            // Always attempt to write to the new stream name if it exists.
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, true);
            if (streamUpdate != null)
            {
                return InternalConfigHost.StaticOpenStreamForWrite(
                    streamUpdate.NewStreamname, templateStreamName, ref writeContext);
            }
            return Host.OpenStreamForWrite(streamName, templateStreamName, ref writeContext);
        }

        public override void WriteCompleted(string streamName, bool success, object writeContext)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, true);
            if (streamUpdate != null)
            {
                InternalConfigHost.StaticWriteCompleted(streamUpdate.NewStreamname, success, writeContext);

                // Mark the write as having successfully completed, so that subsequent calls
                // to Read() will use the new stream name.
                if (success) streamUpdate.WriteCompleted = true;
            }
            else
            {
                Host.WriteCompleted(streamName, success, writeContext);
            }
        }

        public override bool IsConfigRecordRequired(string configPath)
        {
            return true;
        }

        public override void DeleteStream(string streamName)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, false);
            if (streamUpdate != null) InternalConfigHost.StaticDeleteStream(streamUpdate.NewStreamname);
            else Host.DeleteStream(streamName);
        }

        public override bool IsFile(string streamName)
        {
            StreamUpdate streamUpdate = GetStreamUpdate(streamName, false);
            return streamUpdate != null
                ? InternalConfigHost.StaticIsFile(streamUpdate.NewStreamname)
                : Host.IsFile(streamName);
        }
    }
}
