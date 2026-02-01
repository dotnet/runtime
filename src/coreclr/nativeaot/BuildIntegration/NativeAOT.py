"""
LLDB pretty printer for NativeAOT

Add to ~/.lldbinit:

command script import {path-to-ilcompiler-package}/build/NativeAOT.lldb.py
"""

import lldb


def __lldb_init_module(debugger, dict):
    debugger.HandleCommand(
        'type summary add -x "^String$" -e -F NativeAOT.String_SummaryProvider'
    )
    debugger.HandleCommand(
        'type summary add -x "^__Array<.+>$" -e -F NativeAOT.Array_SummaryProvider'
    )
    debugger.HandleCommand(
        'type synthetic add -x "^__Array<.+>$" -l NativeAOT.Array_SyntheticProvider'
    )
    debugger.HandleCommand(
        'type synthetic add -x "^Object$" -l NativeAOT.Object_SyntheticProvider'
    )


def String_SummaryProvider(valobj, internal_dict):
    valobj = valobj.GetNonSyntheticValue()
    strval = '"'
    try:
        length = valobj.GetChildMemberWithName("_stringLength").GetValueAsUnsigned()
        chars = (
            valobj.GetChildMemberWithName("_firstChar")
            .address_of.GetPointeeData(0, length)
            .uint16
        )
        for i in range(0, length):
            strval += chr(chars[i])
    except:
        pass
    strval += '"'
    return strval


def Array_SummaryProvider(valobj, internal_dict):
    return "length=" + str(
        valobj.GetNonSyntheticValue()
        .GetChildMemberWithName("_numComponents")
        .GetValueAsUnsigned()
    )


class Array_SyntheticProvider:
    def __init__(self, valobj, internal_dict):
        self.valobj = valobj

    def num_children(self):
        return self.valobj.GetChildMemberWithName("_numComponents").GetValueAsUnsigned()

    def get_child_index(self, name):
        try:
            return int(name.lstrip("[").rstrip("]"))
        except:
            return None

    def get_child_at_index(self, index):
        t = self.valobj.GetChildMemberWithName("m_Data").GetType().GetArrayElementType()
        return self.valobj.GetChildMemberWithName(
            "_numComponents"
        ).address_of.CreateChildAtOffset(
            f"[{index}]",
            self.valobj.target.GetAddressByteSize() + index * t.GetByteSize(),
            t,
        )


class Object_SyntheticProvider:
    def __init__(self, valobj, internal_dict):
        self.valobj = valobj

    def num_children(self):
        return 0
