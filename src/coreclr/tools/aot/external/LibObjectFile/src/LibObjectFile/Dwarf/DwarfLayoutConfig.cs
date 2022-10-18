// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfLayoutConfig
    {
        private DwarfAttributeForm _defaultAttributeFormForReference;

        public DwarfLayoutConfig()
        {
            DefaultAttributeFormForReference = DwarfAttributeForm.Ref4;
        }

        public DwarfAttributeFormEx DefaultAttributeFormForReference
        {
            get => _defaultAttributeFormForReference;
            set
            {
                switch (value.Value)
                {
                    case DwarfAttributeForm.Ref1:
                    case DwarfAttributeForm.Ref2:
                    case DwarfAttributeForm.Ref4:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }

                _defaultAttributeFormForReference = value;
            }
        }
    }
}