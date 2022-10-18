// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfReader : DwarfReaderWriter
    {
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerCompilationUnit;
        private readonly Dictionary<ulong, DwarfDIE> _registeredDIEPerSection;
        private readonly List<DwarfDIEReference> _unresolvedDIECompilationUnitReference;
        private readonly List<DwarfDIEReference> _attributesWithUnresolvedDIESectionReference;
        private readonly Stack<DwarfDIE> _stack;
        private readonly Stack<DwarfDIE> _stackWithLineProgramTable;

        internal DwarfReader(DwarfReaderContext context, DwarfFile file, DiagnosticBag diagnostics) : base(file, diagnostics)
        {
            IsReadOnly = context.IsInputReadOnly;
            AddressSize = context.AddressSize;
            IsLittleEndian = context.IsLittleEndian;
            _registeredDIEPerCompilationUnit = new Dictionary<ulong, DwarfDIE>();
            _registeredDIEPerSection = new Dictionary<ulong, DwarfDIE>();
            _unresolvedDIECompilationUnitReference = new List<DwarfDIEReference>();
            _attributesWithUnresolvedDIESectionReference = new List<DwarfDIEReference>();
            OffsetToLineProgramTable = new Dictionary<ulong, DwarfLineProgramTable>();
            OffsetToLocationList = new Dictionary<ulong, DwarfLocationList>();
            _stack = new Stack<DwarfDIE>();
            _stackWithLineProgramTable = new Stack<DwarfDIE>();
        }

        public override bool IsReadOnly { get; }

        public DwarfUnitKind DefaultUnitKind { get; internal set; }

        internal int DIELevel { get; set; }

        internal DwarfDIE CurrentDIE => _stack.Count > 0 ? _stack.Peek() : null;

        internal DwarfLineProgramTable CurrentLineProgramTable => _stackWithLineProgramTable.Count > 0 ? _stackWithLineProgramTable.Peek().CurrentLineProgramTable : null;

        internal DwarfAttributeDescriptor CurrentAttributeDescriptor { get; set; }

        internal Dictionary<ulong, DwarfLineProgramTable> OffsetToLineProgramTable { get; }

        internal Dictionary<ulong, DwarfLocationList> OffsetToLocationList { get; }

        internal void PushDIE(DwarfDIE die)
        {
            _registeredDIEPerCompilationUnit.Add(die.Offset - CurrentUnit.Offset, die);
            _registeredDIEPerSection.Add(die.Offset, die);
            _stack.Push(die);
        }

        internal void PushLineProgramTable(DwarfLineProgramTable lineTable)
        {
            var dieWithLineProgramTable = CurrentDIE;
            if (_stackWithLineProgramTable.Count > 0 && ReferenceEquals(_stackWithLineProgramTable.Peek(), dieWithLineProgramTable))
            {
                return;
            }

            _stackWithLineProgramTable.Push(dieWithLineProgramTable);
            dieWithLineProgramTable.CurrentLineProgramTable = lineTable;
        }

        internal void PopDIE()
        {
            var die = _stack.Pop();
            if (die.CurrentLineProgramTable != null)
            {
                var dieWithProgramLineTable = _stackWithLineProgramTable.Pop();
                Debug.Assert(ReferenceEquals(die, dieWithProgramLineTable));
                dieWithProgramLineTable.CurrentLineProgramTable = null;
            }
        }

        internal void ClearResolveAttributeReferenceWithinCompilationUnit()
        {
            _registeredDIEPerCompilationUnit.Clear();
            _unresolvedDIECompilationUnitReference.Clear();
        }

        internal void ResolveAttributeReferenceWithinCompilationUnit()
        {
            // Resolve attribute reference within the CU
            foreach (var unresolvedAttrRef in _unresolvedDIECompilationUnitReference)
            {
                ResolveAttributeReferenceWithinCompilationUnit(unresolvedAttrRef, true);
            }
        }

        internal void ResolveAttributeReferenceWithinSection()
        {
            // Resolve attribute reference within the section
            foreach (var unresolvedAttrRef in _attributesWithUnresolvedDIESectionReference)
            {
                ResolveAttributeReferenceWithinSection(unresolvedAttrRef, true);
            }
        }
        
        internal void ResolveAttributeReferenceWithinCompilationUnit(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerCompilationUnit.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}, section 0x{(dieRef.Offset):x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _unresolvedDIECompilationUnitReference.Add(dieRef);
                }
            }
        }

        internal  void ResolveAttributeReferenceWithinSection(DwarfDIEReference dieRef, bool errorIfNotFound)
        {
            if (_registeredDIEPerSection.TryGetValue(dieRef.Offset, out var die))
            {
                dieRef.Resolved = die;
                dieRef.Resolver(ref dieRef);
            }
            else
            {
                if (errorIfNotFound)
                {
                    if (dieRef.Offset != 0)
                    {
                        Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidReference, $"Unable to resolve DIE reference (0x{dieRef.Offset:x}) for {dieRef.DwarfObject} at offset 0x{dieRef.Offset:x}");
                    }
                }
                else
                {
                    _attributesWithUnresolvedDIESectionReference.Add(dieRef);
                }
            }
        }

        internal struct DwarfDIEReference
        {
            public DwarfDIEReference(ulong offset, object dwarfObject, DwarfDIEReferenceResolver resolver) : this()
            {
                Offset = offset;
                DwarfObject = dwarfObject;
                Resolver = resolver;
            }

            public readonly ulong Offset;

            public readonly object DwarfObject;

            public readonly DwarfDIEReferenceResolver Resolver;

            public DwarfDIE Resolved;
        }

        internal delegate void DwarfDIEReferenceResolver(ref DwarfDIEReference reference);
    }
}