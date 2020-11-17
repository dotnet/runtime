// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Used by ActivityListener to indicate what amount of data should be collected for this Activity
    /// Requesting more data causes greater performance overhead to collect it.
    /// </summary>
    public enum ActivitySamplingResult
    {
        /// <summary>
        /// The Activity object doesn't need to be created
        /// </summary>
        None,

        /// <summary>
        /// The Activity object needs to be created. It will have Name, Source, Id and Baggage.
        /// Other properties are unnecessary and will be ignored by this listener.
        /// </summary>
        PropagationData,

        /// <summary>
        /// The activity object should be populated with all the propagation info and also all other
        /// properties such as Links, Tags, and Events. Activity.IsAllDataRequested will return true.
        /// </summary>
        AllData,

        /// <summary>
        /// The activity object should be populated the same as the AllData case and additionally
        /// Activity.IsRecorded is set true. For activities using W3C trace ids this sets a flag bit in the
        /// ID that will be propagated downstream requesting that trace is recorded everywhere.
        /// </summary>
        AllDataAndRecorded
    }
}
