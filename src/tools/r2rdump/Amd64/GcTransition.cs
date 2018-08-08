// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump.Amd64
{
    public struct InterruptibleRange
    {
        [XmlAttribute("Index")]
        public uint Index { get; set; }
        public uint StartOffset { get; set; }
        public uint StopOffset { get; set; }
        public InterruptibleRange(uint index, uint start, uint stop)
        {
            Index = index;
            StartOffset = start;
            StopOffset = stop;
        }
    }

    public class GcTransition : BaseGcTransition
    {
        public int SlotId { get; set; }
        public bool IsLive { get; set; }
        public int ChunkId { get; set; }
        public string SlotState { get; set; }

        public GcTransition() { }

        public GcTransition(int codeOffset, int slotId, bool isLive, int chunkId, GcSlotTable slotTable)
        {
            CodeOffset = codeOffset;
            SlotId = slotId;
            IsLive = isLive;
            ChunkId = chunkId;
            SlotState = GetSlotState(slotTable);
        }

        public override string ToString()
        {
            return SlotState;
        }

        public string GetSlotState(GcSlotTable slotTable)
        {
            GcSlotTable.GcSlot slot = slotTable.GcSlots[SlotId];
            string slotStr = "";
            if (slot.StackSlot == null)
            {
                slotStr = Enum.GetName(typeof(Registers), slot.RegisterNumber);
            }
            else
            {
                slotStr = $"sp{slot.StackSlot.SpOffset:+#;-#;+0}";
            }
            string isLiveStr = "live";
            if (!IsLive)
                isLiveStr = "dead";
            return $"{slotStr} is {isLiveStr}";
        }
    }
}
