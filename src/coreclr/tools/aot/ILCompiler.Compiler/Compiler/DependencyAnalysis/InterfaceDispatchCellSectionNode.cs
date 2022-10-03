// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a section of the executable where interface dispatch cells and their slot information
    /// is stored.
    /// </summary>
    public class InterfaceDispatchCellSectionNode : ArrayOfEmbeddedDataNode<InterfaceDispatchCellNode>
    {
        public InterfaceDispatchCellSectionNode(NodeFactory factory)
            : base("__InterfaceDispatchCellSection_Start", "__InterfaceDispatchCellSection_End", new DispatchCellComparer(factory))
        {
        }

        protected override void GetElementDataForNodes(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return;

            // The interface dispatch cell has an alignment requirement of 2 * [Pointer size] as part of the 
            // synchronization mechanism of the two values in the runtime.
            builder.RequireInitialAlignment(factory.Target.PointerSize * 2);

            // This number chosen to be high enough that the cost of recording slot numbers is cheap.
            const int InterfaceDispatchCellRunLength = 32;

            const int NoSlot = -1;

            //
            // We emit the individual dispatch cells in groups. The purpose of the grouping is to save
            // us the number of slots we need to emit. The grouping looks like this:
            //
            // DispatchCell1
            // DispatchCell2
            // ...
            // DispatchCellN
            // Null
            // Slot of the above dispatch cells
            //
            int runLength = 0;
            int currentSlot = NoSlot;
            foreach (InterfaceDispatchCellNode node in NodesList)
            {
                MethodDesc targetMethod = node.TargetMethod;
                int targetSlot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                if (currentSlot == NoSlot)
                {
                    // This is the first dispatch cell we're emitting
                    currentSlot = targetSlot;
                }
                else if (currentSlot != targetSlot || runLength == InterfaceDispatchCellRunLength)
                {
                    // Make sure we are sorted
                    Debug.Assert(targetSlot >= currentSlot);

                    // End the run of dispatch cells
                    builder.EmitZeroPointer();
                    builder.EmitNaturalInt(currentSlot);

                    currentSlot = targetSlot;
                    runLength = 0;
                }

                node.InitializeOffsetFromBeginningOfArray(builder.CountBytes);
                node.EncodeData(ref builder, factory, relocsOnly);
                builder.AddSymbol(node);

                runLength++;
            }

            if (runLength > 0)
            {
                // End the run of dispatch cells
                builder.EmitZeroPointer();
                builder.EmitNaturalInt(currentSlot);
            }
        }

        public override int ClassCode => -1389343;

        /// <summary>
        /// Comparer that groups interface dispatch cells by their slot number.
        /// </summary>
        private class DispatchCellComparer : IComparer<InterfaceDispatchCellNode>
        {
            private readonly NodeFactory _factory;
            private readonly TypeSystemComparer _comparer = TypeSystemComparer.Instance;

            public DispatchCellComparer(NodeFactory factory)
            {
                _factory = factory;
            }

            public int Compare(InterfaceDispatchCellNode x, InterfaceDispatchCellNode y)
            {
                MethodDesc methodX = x.TargetMethod;
                MethodDesc methodY = y.TargetMethod;

                // The primary purpose of this comparer is to sort everything by slot
                int slotX = VirtualMethodSlotHelper.GetVirtualMethodSlot(_factory, methodX, methodX.OwningType);
                int slotY = VirtualMethodSlotHelper.GetVirtualMethodSlot(_factory, methodY, methodY.OwningType);

                int result = slotX - slotY;
                if (result != 0)
                    return result;

                // If slots are the same, compare the method and callsite identifier to get
                // a deterministic order within the group.
                result = _comparer.Compare(methodX, methodY);
                if (result != 0)
                    return result;

                result = StringComparer.Ordinal.Compare(x.CallSiteIdentifier, y.CallSiteIdentifier);
                if (result != 0)
                    return result;

                Debug.Assert(x == y);
                return 0;
            }
        }
    }
}
