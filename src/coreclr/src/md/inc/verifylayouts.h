// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// VerifyLayouts.h
//

//
// Make sure that layouts of MD data strucutres doesn't change accidentally
//
//*****************************************************************************

// The code in MD\DataSource\TargetTypes.* takes a direct dependency on
// the layouts of types in MD. This is used by the debugger to read metadata
// from a seperate process by deserializing the memory for these datastructures.
//
// You are probably reading this comment because you changed a layout and
// one of the static_asserts failed during build. This is what you should
// do to fix it:
//
// a) Go to clr\src\Debug\EE\Debugger.cpp and increment the global version counter
//    m_mdDataStructureVersion set in Debugger::Debugger()
//    Please comment the change there with a new entry in the version table
//
// b) If a define is conditionally changing the layout:
//    i) add, if needed, an entry to the list of define bits in
//       clr\src\Debug\EE\debugger.h Debugger::_Target_Defines
//    ii) add code like this that sets the bit in the Debugger::_defines static
//        variable
//        #ifdef MY_DEFINE
//        | DEFINE_MY_DEFINE
//        #endif
//
// c) Update the code in MD\DataSource\TargetTypes.h/cpp to deserialize your
//    new layout correctly. The code needs to work for any version of the layouts
//    with or without defines set. Your reader code can access the current version
//    and defines by calling:
//    reader.GetMDStructuresVersion()
//    reader.IsDefined(Define_XYZ)
//
// d) If your changes affect what a debugger should be reading in order to fetch
//    metadata then you probably need to change other parts of the debugger
//    implementation as well. In general the debugger cares about the schema,
//    TableDefs, storage signature, table records, and storage pools.
//
// e) AFTER you have fixed up the debugger stuff above, now its time to update
//    layout definitions so the static asserts will quiet down. Check out
//    the comments in VerifyLayouts.inc for how to do that.
//
//  Thanks for helping us keep the debugger working :)
//




//-------------------------------------------------------------------------------
// Type layout verification
//
//
// These macros includes VerifyLayouts.inc a few times with different definitions to build up
// the source. The final result should look something like this:
// (don't assume specific type names/fields/offsets/sizes are accurate in this example)
//
//
// class VerifyLayoutsMD
// {
//
//      static const int expected_offset_of_first_field_in_CMiniMdRW = 208;
//      static const int actual_offset_of_first_field_in_CMiniMdRW =
//      208;
//      static const int offset_of_field_after_CMiniMdRW_m_Schema =
//      312;
//      static const int offset_of_field_after_CMiniMdRW_m_Tables =
//      316;
//      ... many more lines like this covering all fields in all marked up types ...
//
//
//      static const int alignment_of_first_field_in_CMiniMdRW =
//      4;
//      static const int alignment_of_field_after_CMiniMdRW_m_Schema =
//      8;
//      static const int alignment_of_field_after_CMiniMdRW_m_Tables =
//      8;
//      ... many more lines like this cover all fields in all marked up types ...
//
//
//      static_assert_no_msg(expected_offset_of_first_field_in_CMiniMdRW == actual_offset_of_first_field_in_CMiniMdRW);
//      static_assert_no_msg(offset_of_field_after_CMiniMdRW_m_Schema ==
//          ALIGN_UP(offsetof(CMiniMdRW, m_Schema) + 104, alignment_of_field_after_CMiniMdRW_m_Schema));
//      static_assert_no_msg(offset_of_field_after_CMiniMdRW_m_Tables ==
//          ALIGN_UP(offsetof(CMiniMdRW, m_Tables) + 4, alignment_of_field_after_CMiniMdRW_m_Tables));
//      ... many more lines like this cover all fields in all marked up types ...
//
//  };
//
//
//
//

#ifdef FEATURE_METADATA_VERIFY_LAYOUTS

#include <stddef.h> // offsetof
#include "static_assert.h"
#include "metamodel.h"
#include "mdinternalrw.h"

// other types provide friend access to this type so that the
// offsetof macro can access their private fields
class VerifyLayoutsMD
{
    // we have a bunch of arrays with this fixed size, make sure it doesn't change
    static_assert_no_msg(TBL_COUNT == 45);

#define USING_ALIAS(typeName, ...)  using typeName = __VA_ARGS__;

#define FIELD(typeName, fieldName, fieldSize) ALIGN_FIELD(typeName, fieldName, fieldSize, fieldSize)

#define BEGIN_TYPE(typeName, initialFieldOffset) \
    static const int expected_offset_of_first_field_in_##typeName = initialFieldOffset; \
    static const int actual_offset_of_first_field_in_##typeName =

#define ALIGN_FIELD(typeName, fieldName, fieldSize, fieldAlign) \
    offsetof(typeName, fieldName); \
    static const int offset_of_field_after_##typeName##_##fieldName =

#define BITFIELD(typeName, fieldName, fieldOffset, fieldSize) \
    fieldOffset; \
    static const int offset_of_field_after_##typeName##_##fieldName =

#define END_TYPE(typeName, typeAlignentSize) \
    sizeof(typeName);

#include "VerifyLayouts.inc"

// Only declare using once
#undef USING_ALIAS
#define USING_ALIAS(a, ...)

#undef BEGIN_TYPE
#undef ALIGN_FIELD
#undef END_TYPE
#undef BITFIELD


#define BEGIN_TYPE(typeName, initialFieldOffset) \
    static const int alignment_of_first_field_in_##typeName =
#define ALIGN_FIELD(typeName, fieldName, fieldSize, fieldAlign) \
    fieldAlign; \
    static const int alignment_of_field_after_##typeName##_##fieldName =
#define BITFIELD(typeName, fieldName, fieldOffset, fieldSize) \
    fieldSize; \
    static const int alignment_of_field_after_##typeName##_##fieldName =
#define END_TYPE(typeName, typeAlignmentSize) \
    typeAlignmentSize;

#include "VerifyLayouts.inc"

#undef BEGIN_TYPE
#undef ALIGN_FIELD
#undef END_TYPE
#undef BITFIELD


#define BEGIN_TYPE(typeName, initialFieldOffset) \
    static_assert_no_msg(expected_offset_of_first_field_in_##typeName == actual_offset_of_first_field_in_##typeName);


#define ALIGN_UP(value, alignment) (((value) + (alignment) - 1)&~((alignment) - 1))
#define ALIGN_FIELD(typeName, fieldName, fieldSize, fieldAlign) \
    static_assert_no_msg(offset_of_field_after_##typeName##_##fieldName == \
    ALIGN_UP(offsetof(typeName, fieldName) + fieldSize, alignment_of_field_after_##typeName##_##fieldName));
#define BITFIELD(typeName, fieldName, fieldOffset, fieldSize) \
    static_assert_no_msg(offset_of_field_after_##typeName##_##fieldName == \
    ALIGN_UP(fieldOffset + fieldSize, alignment_of_field_after_##typeName##_##fieldName));

#define END_TYPE(typeName, typeAlignmentSize)
#include "VerifyLayouts.inc"

};




#endif //FEATURE_METADATA_VERIFY_LAYOUTS
