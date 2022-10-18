// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Operations.Count,nq}")]
    public class DwarfExpression : DwarfObject<DwarfObject>
    {
        private readonly List<DwarfOperation> _operations;

        public DwarfExpression()
        {
            _operations = new List<DwarfOperation>();
        }

        public IReadOnlyList<DwarfOperation> Operations => _operations;

        internal List<DwarfOperation> InternalOperations => _operations;

        public ulong OperationLengthInBytes { get; internal set; }

        public void AddOperation(DwarfOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _operations.Add(this, operation);
        }

        public void RemoveOperation(DwarfOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            _operations.Remove(this, operation);
        }

        public DwarfOperation RemoveOperationAt(int index)
        {
            return _operations.RemoveAt(this, index);
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            foreach (var op in _operations)
            {
                op.Verify(diagnostics);
            }
        }

        internal void ReadInternal(DwarfReader reader, bool inLocationSection = false)
        {
            Offset = reader.Offset;
            var size = inLocationSection ? reader.ReadU16() : reader.ReadULEB128();
            OperationLengthInBytes = size;
            var endPosition = reader.Offset + size;

            while (reader.Offset < endPosition)
            {
                var op = new DwarfOperation() {Offset = reader.Offset};
                op.ReadInternal(reader);
                AddOperation(op);
            }

            Size = reader.Offset - Offset;
        }

        internal void WriteInternal(DwarfWriter writer, bool inLocationSection = false)
        {
            Debug.Assert(Offset == writer.Offset);
            Debug.Assert(!inLocationSection || OperationLengthInBytes <= ushort.MaxValue);

            var startExpressionOffset = writer.Offset;
            if (inLocationSection)
            {
                writer.WriteU16((ushort)OperationLengthInBytes);
            }
            else
            {
                writer.WriteULEB128(OperationLengthInBytes);
            }

            foreach (var op in Operations)
            {
                op.WriteInternal(writer);
            }

            Debug.Assert(writer.Offset - startExpressionOffset == Size);
        }

        internal void UpdateLayoutInternal(DwarfLayoutContext layoutContext, bool inLocationSection = false)
        {
            var endOffset = Offset;
            foreach (var op in _operations)
            {
                op.Offset = endOffset;
                op.UpdateLayoutInternal(layoutContext);
                endOffset += op.Size;
            }

            OperationLengthInBytes = endOffset - Offset;

            // We need to shift the expression which is prefixed by its size encoded in LEB128,
            // or fixed-size U2 in .debug_loc section
            var deltaLength = inLocationSection ? sizeof(ushort) : DwarfHelper.SizeOfULEB128(Size);
            foreach (var op in InternalOperations)
            {
                op.Offset += deltaLength;
            }

            Size = OperationLengthInBytes + deltaLength;
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            UpdateLayoutInternal(layoutContext, inLocationSection: false);
        }

        protected override void Read(DwarfReader reader)
        {
            ReadInternal(reader, inLocationSection: false);
        }

        protected override void Write(DwarfWriter writer)
        {
            WriteInternal(writer, inLocationSection: false);
        }
    }
}