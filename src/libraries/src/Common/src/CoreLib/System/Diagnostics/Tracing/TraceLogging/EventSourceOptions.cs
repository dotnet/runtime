// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Used when calling EventSource.Write.
    /// Optional overrides for event settings such as Level, Keywords, or Opcode.
    /// If overrides are not provided for a setting, default values will be used.
    /// </summary>
    public struct EventSourceOptions
    {
        internal EventKeywords keywords;
        internal EventTags tags;
        internal EventActivityOptions activityOptions;
        internal byte level;
        internal byte opcode;
        internal byte valuesSet;

        internal const byte keywordsSet = 0x1;
        internal const byte tagsSet = 0x2;
        internal const byte levelSet = 0x4;
        internal const byte opcodeSet = 0x8;
        internal const byte activityOptionsSet = 0x10;

        /// <summary>
        /// Gets or sets the level to use for the specified event. If this property
        /// is unset, the event's level will be 5 (Verbose).
        /// </summary>
        public EventLevel Level
        {
            get
            {
                return (EventLevel)this.level;
            }

            set
            {
                this.level = checked((byte)value);
                this.valuesSet |= levelSet;
            }
        }

        /// <summary>
        /// Gets or sets the opcode to use for the specified event. If this property
        /// is unset, the event's opcode will 0 (Info).
        /// </summary>
        public EventOpcode Opcode
        {
            get
            {
                return (EventOpcode)this.opcode;
            }

            set
            {
                this.opcode = checked((byte)value);
                this.valuesSet |= opcodeSet;
            }
        }

        internal bool IsOpcodeSet
        {
            get
            {
                return (this.valuesSet & opcodeSet) != 0;
            }
        }

        /// <summary>
        /// Gets or sets the keywords to use for the specified event. If this
        /// property is unset, the event's keywords will be 0.
        /// </summary>
        public EventKeywords Keywords
        {
            get
            {
                return this.keywords;
            }

            set
            {
                this.keywords = value;
                this.valuesSet |= keywordsSet;
            }
        }

        /// <summary>
        /// Gets or sets the tags to use for the specified event. If this property is
        /// unset, the event's tags will be 0.
        /// </summary>
        public EventTags Tags
        {
            get
            {
                return this.tags;
            }

            set
            {
                this.tags = value;
                this.valuesSet |= tagsSet;
            }
        }

        /// <summary>
        /// Gets or sets the activity options for this specified events. If this property is
        /// unset, the event's activity options will be 0.
        /// </summary>
        public EventActivityOptions ActivityOptions
        {
            get
            {
                return this.activityOptions;
            }
            set
            {
                this.activityOptions = value;
                this.valuesSet |= activityOptionsSet;
            }
        }
    }
}
