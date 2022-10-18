// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public sealed class DwarfAbbreviation : DwarfObject<DwarfAbbreviationTable>
    {
        private readonly List<DwarfAbbreviationItem> _items;
        private readonly Dictionary<ulong, DwarfAbbreviationItem> _mapItems; // Only used if code are non contiguous
        private readonly Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem> _mapKeyToItem;
        private ulong _nextCode;

        public DwarfAbbreviation()
        {
            _items = new List<DwarfAbbreviationItem>();
            _mapItems = new Dictionary<ulong, DwarfAbbreviationItem>();
            _mapKeyToItem = new Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem>();
            _nextCode = 1;
        }

        public void Reset()
        {
            // Reset parent dependency
            foreach (var dwarfAbbreviationItem in _items)
            {
                dwarfAbbreviationItem.Parent = null;
            }

            if (_mapItems.Count > 0)
            {
                foreach (var keyPair in _mapItems)
                {
                    keyPair.Value.Parent = null;
                }
            }

            _items.Clear();
            _mapItems.Clear();
            _mapKeyToItem.Clear();
            _nextCode = 1;
        }

        public IEnumerable<DwarfAbbreviationItem> Items => _mapItems.Count > 0 ? GetMapItems() : _items;

        private IEnumerable<DwarfAbbreviationItem> GetMapItems()
        {
            foreach (var item in _mapItems.Values)
            {
                yield return item;
            }
        }
        
        public DwarfAbbreviationItem GetOrCreate(DwarfAbbreviationItemKey itemKey)
        {
            if (!_mapKeyToItem.TryGetValue(itemKey, out var item))
            {
                item = new DwarfAbbreviationItem(_nextCode, itemKey.Tag, itemKey.HasChildren, itemKey.Descriptors)
                {
                    Parent = this
                };

                if (_mapItems.Count > 0)
                {

                    _mapItems[_nextCode] = item;
                }
                else
                {
                    _items.Add(item);
                }

                _mapKeyToItem[itemKey] = item;

                _nextCode++;
            }

            return item;
        }

        public bool TryFindByCode(ulong code, out DwarfAbbreviationItem item)
        {
            item = null;
            if (code == 0)
            {
                return false;
            }

            code--;

            if (_mapItems.Count > 0)
            {
                return _mapItems.TryGetValue(code, out item);
            }

            if (code < int.MaxValue && (int)code < _items.Count)
            {
                item = _items[(int) code];
                return true;
            }

            item = null;
            return false;
        }

        private string DebuggerDisplay => $"Count = {(_mapItems.Count > 0 ? _mapItems.Count : _items.Count)}";

        private bool TryReadNext(DwarfReader reader)
        {
            var startOffset = (ulong)reader.Offset;
            var code = reader.ReadULEB128();
            if (code == 0)
            {
                return false;
            }

            var item = new DwarfAbbreviationItem
            {
                Offset = startOffset, 
                Code = code
            };

            var index = code - 1;
            bool canAddToList = _mapItems.Count == 0 && index < int.MaxValue && _items.Count == (int)index;

            item.ReadInternal(reader);

            if (canAddToList)
            {
                _items.Add(item);
                _nextCode++;
            }
            else
            {
                if (_mapItems.Count == 0)
                {
                    for (var i = 0; i < _items.Count; i++)
                    {
                        var previousItem = _items[i];
                        _mapItems.Add((ulong)i + 1, previousItem);
                    }
                    _items.Clear();
                }

                // TODO: check collisions
                if (_mapItems.ContainsKey(code))
                {
                    reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid code {code} found while another code already exists in this abbreviation.");
                    return false;
                }
                _mapItems.Add(code, item);

                _nextCode = Math.Max(code, _nextCode) + 1;
            }

            var key = new DwarfAbbreviationItemKey(item.Tag, item.HasChildren, item.Descriptors);
            _mapKeyToItem.Add(key, item);

            return true;
        }

        protected override void Read(DwarfReader reader)
        {
            Offset = reader.Offset;
            while (TryReadNext(reader))
            {
            }

            Size = (ulong)reader.Offset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;
            Debug.Assert(startOffset == Offset);
            if (_mapItems.Count > 0)
            {
                foreach (var itemPair in _mapItems)
                {
                    var item = itemPair.Value;
                    item.WriteInternal(writer);
                }

            }
            else
            {
                if (_items.Count > 0)
                {
                    foreach (var item in _items)
                    {
                        item.WriteInternal(writer);
                    }
                }
            }

            // End of abbreviation item
            writer.WriteULEB128(0);

            Debug.Assert(writer.Offset - startOffset == Size);
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;

            if (_mapItems.Count > 0)
            {
                foreach (var itemPair in _mapItems)
                {
                    var item = itemPair.Value;
                    item.Offset = endOffset;
                    item.UpdateLayoutInternal(layoutContext);
                    endOffset += item.Size;
                }

            }
            else
            {
                if (_items.Count > 0)
                {
                    foreach (var item in _items)
                    {
                        item.Offset = endOffset;
                        item.UpdateLayoutInternal(layoutContext);
                        endOffset += item.Size;
                    }
                }
            }

            endOffset += DwarfHelper.SizeOfULEB128(0);

            Size = endOffset - Offset;
        }
    }
}