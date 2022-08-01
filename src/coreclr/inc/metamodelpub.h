// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModelPub.h -- header file for Common Language Runtime metadata.
//

//
//*****************************************************************************

#ifndef _METAMODELPUB_H_
#define _METAMODELPUB_H_

#if _MSC_VER >= 1100
# pragma once
#endif

#include <cor.h>
#include "contract.h"

template<class T> inline T Align4(T p)
{
    LIMITED_METHOD_CONTRACT;

    INT_PTR i = (INT_PTR)p;
    i = (i+(3)) & ~3;
    return (T)i;
}

typedef uint32_t RID;

// check if a rid is valid or not
#define     InvalidRid(rid) ((rid) == 0)

#ifndef METADATA_FIELDS_PROTECTION
#define METADATA_FIELDS_PROTECTION public
#endif

//*****************************************************************************
// Record definitions.  Records have some combination of fixed size fields and
//  variable sized fields (actually, constant across a database, but variable
//  between databases).
//
// In this section we define record definitions which include the fixed size
//  fields and an enumeration of the variable sized fields.
//
// Naming is as follows:
//  Given some table "Xyz":
//  class XyzRec { public:
//    SOMETYPE  m_SomeField;
//        // rest of the fixed fields.
//    enum { COL_Xyz_SomeOtherField,
//        // rest of the fields, enumerated.
//        COL_Xyz_COUNT };
//   };
//
// The important features are the class name (XyzRec), the enumerations
//  (COL_Xyz_FieldName), and the enumeration count (COL_Xyz_COUNT).
//
// THESE NAMING CONVENTIONS ARE CARVED IN STONE!  DON'T TRY TO BE CREATIVE!
//
//*****************************************************************************
// Have the compiler generate two byte alignment.  Be careful to manually lay
//  out the fields for proper alignment.  The alignment for variable-sized
//  fields will be computed at save time.
#include <pshpack2.h>

// Non-sparse tables.
class ModuleRec
{
METADATA_FIELDS_PROTECTION:
    USHORT  m_Generation;               // ENC generation.
public:
    enum {
        COL_Generation,

        COL_Name,
        COL_Mvid,
        COL_EncId,
        COL_EncBaseId,
        COL_COUNT,
        COL_KEY
    };
    USHORT GetGeneration()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Generation);
    }
    void SetGeneration(USHORT Generation)
    {
        LIMITED_METHOD_CONTRACT;

        m_Generation = VAL16(Generation);
    }
};

class TypeRefRec
{
public:
    enum {
        COL_ResolutionScope,            // mdModuleRef or mdAssemblyRef.
        COL_Name,
        COL_Namespace,
        COL_COUNT,
        COL_KEY
    };
};

class TypeDefRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Flags;                // Flags for this TypeDef
public:
    enum {
        COL_Flags,

        COL_Name,                       // offset into string pool.
        COL_Namespace,
        COL_Extends,                    // coded token to typedef/typeref.
        COL_FieldList,                  // rid of first field.
        COL_MethodList,                 // rid of first method.
        COL_COUNT,
        COL_KEY
    };
    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }
    void AddFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags |= VAL32(Flags);
    }
    void RemoveFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags &= ~VAL32(Flags);
    }

};

class FieldPtrRec
{
public:
    enum {
        COL_Field,
        COL_COUNT,
        COL_KEY
    };
};

class FieldRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Flags;                // Flags for the field.
public:
    enum {
        COL_Flags,

        COL_Name,
        COL_Signature,
        COL_COUNT,
        COL_KEY
    };
    USHORT GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = (USHORT)VAL16(Flags);
    }
    void AddFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags |= (USHORT)VAL16(Flags);
    }
    void RemoveFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags &= (USHORT)~VAL16(Flags);
    }


};

class MethodPtrRec
{
public:
    enum {
        COL_Method,
        COL_COUNT,
        COL_KEY
    };
};

class MethodRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_RVA;        // RVA of the Method.
    USHORT      m_ImplFlags;  // Descr flags of the Method.
    USHORT      m_Flags;      // Flags for the Method.
public:
    enum {
        COL_RVA,
        COL_ImplFlags,
        COL_Flags,

        COL_Name,
        COL_Signature,
        COL_ParamList,                  // Rid of first param.
        COL_COUNT,
        COL_KEY
    };

    void Copy(MethodRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_RVA = pFrom->m_RVA;
        m_ImplFlags = pFrom->m_ImplFlags;
        m_Flags = pFrom->m_Flags;
    }

    ULONG GetRVA()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_RVA);
    }
    void SetRVA(ULONG RVA)
    {
        LIMITED_METHOD_CONTRACT;

        m_RVA = VAL32(RVA);
    }

    USHORT GetImplFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_ImplFlags);
    }
    void SetImplFlags(USHORT ImplFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_ImplFlags = VAL16(ImplFlags);
    }
    void AddImplFlags(USHORT ImplFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_ImplFlags |= VAL16(ImplFlags);
    }
    void RemoveImplFlags(USHORT ImplFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_ImplFlags &= ~VAL16(ImplFlags);
    }


    USHORT GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = (USHORT)VAL16(Flags);
    }
    void AddFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags |= (USHORT)VAL16(Flags);
    }
    void RemoveFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags &= (USHORT)~VAL16(Flags);
    }
};

class ParamPtrRec
{
public:
    enum {
        COL_Param,
        COL_COUNT,
        COL_KEY
    };
};

class ParamRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Flags;                // Flags for this Param.
    USHORT      m_Sequence;             // Sequence # of param.  0 - return value.
public:
    enum {
        COL_Flags,
        COL_Sequence,

        COL_Name,                       // Name of the param.
        COL_COUNT,
        COL_KEY
    };

    void Copy(ParamRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = pFrom->m_Flags;
        m_Sequence = pFrom->m_Sequence;
    }

    USHORT GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = (USHORT)VAL16(Flags);
    }
    void AddFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags |= (USHORT)VAL16(Flags);
    }
    void RemoveFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags &= (USHORT)~VAL16(Flags);
    }

    USHORT GetSequence()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Sequence);
    }
    void SetSequence(USHORT Sequence)
    {
        LIMITED_METHOD_CONTRACT;

        m_Sequence = VAL16(Sequence);
    }

};

class InterfaceImplRec
{
public:
    enum {
        COL_Class,                      // Rid of class' TypeDef.
        COL_Interface,                  // Coded rid of implemented interface.
        COL_COUNT,
        COL_KEY = COL_Class
    };
};

class MemberRefRec
{
public:
    enum {
        COL_Class,                      // Rid of TypeDef.
        COL_Name,
        COL_Signature,
        COL_COUNT,
        COL_KEY
    };
};

class StandAloneSigRec
{
public:
    enum {
        COL_Signature,
        COL_COUNT,
        COL_KEY
    };
};

// Sparse tables.  These contain modifiers for tables above.
class ConstantRec
{
METADATA_FIELDS_PROTECTION:
    BYTE        m_Type;                 // Type of the constant.
    BYTE        m_PAD1;
public:
    enum {
        COL_Type,

        COL_Parent,                     // Coded rid of object (param, field).
        COL_Value,                      // Index into blob pool.
        COL_COUNT,
        COL_KEY = COL_Parent
    };
    BYTE GetType()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Type;
    }
    void SetType(BYTE Type)
    {
        LIMITED_METHOD_CONTRACT;

        m_Type = Type;
    }
};

class CustomAttributeRec
{
public:
    enum {
        COL_Parent,                     // Coded rid of any object.
        COL_Type,                       // TypeDef or TypeRef.
        COL_Value,                      // Blob.
        COL_COUNT,
        COL_KEY = COL_Parent
    };
};

class FieldMarshalRec
{
public:
    enum {
        COL_Parent,                     // Coded rid of field or param.
        COL_NativeType,
        COL_COUNT,
        COL_KEY = COL_Parent
    };
};

class DeclSecurityRec
{
METADATA_FIELDS_PROTECTION:
    USHORT  m_Action;
public:
    enum {
        COL_Action,

        COL_Parent,
        COL_PermissionSet,
        COL_COUNT,
        COL_KEY = COL_Parent
    };

    void Copy(DeclSecurityRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_Action = pFrom->m_Action;
    }
    USHORT GetAction()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Action);
    }
    void SetAction(USHORT Action)
    {
        LIMITED_METHOD_CONTRACT;

        m_Action = VAL16(Action);
    }
};


class ClassLayoutRec
{
METADATA_FIELDS_PROTECTION:
    USHORT  m_PackingSize;
    ULONG   m_ClassSize;
public:
    enum {
        COL_PackingSize,
        COL_ClassSize,

        COL_Parent,                     // Rid of TypeDef.
        COL_COUNT,
        COL_KEY = COL_Parent
    };

    void Copy(ClassLayoutRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_PackingSize = pFrom->m_PackingSize;
        m_ClassSize = pFrom->m_ClassSize;
    }
    USHORT GetPackingSize()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_PackingSize);
    }
    void SetPackingSize(USHORT PackingSize)
    {
        LIMITED_METHOD_CONTRACT;

        m_PackingSize = VAL16(PackingSize);
    }

    ULONG GetClassSize()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_ClassSize);
    }
    void SetClassSize(ULONG ClassSize)
    {
        LIMITED_METHOD_CONTRACT;

        m_ClassSize = VAL32(ClassSize);
    }
};

class FieldLayoutRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_OffSet;
public:
    enum {
        COL_OffSet,

        COL_Field,
        COL_COUNT,
        COL_KEY = COL_Field
    };

    void Copy(FieldLayoutRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_OffSet = pFrom->m_OffSet;
    }
    ULONG GetOffSet()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OffSet);
    }
    void SetOffSet(ULONG Offset)
    {
        LIMITED_METHOD_CONTRACT;

        m_OffSet = VAL32(Offset);
    }
};

class EventMapRec
{
public:
    enum {
        COL_Parent,
        COL_EventList,                  // rid of first event.
        COL_COUNT,
        COL_KEY
    };
};

class EventPtrRec
{
public:
    enum {
        COL_Event,
        COL_COUNT,
        COL_KEY
    };
};

class EventRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_EventFlags;
public:
    enum {
        COL_EventFlags,

        COL_Name,
        COL_EventType,
        COL_COUNT,
        COL_KEY
    };
    USHORT GetEventFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_EventFlags);
    }
    void SetEventFlags(USHORT EventFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_EventFlags = VAL16(EventFlags);
    }
    void AddEventFlags(USHORT EventFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_EventFlags |= VAL16(EventFlags);
    }
};

class PropertyMapRec
{
public:
    enum {
        COL_Parent,
        COL_PropertyList,               // rid of first property.
        COL_COUNT,
        COL_KEY
    };
};

class PropertyPtrRec
{
public:
    enum {
        COL_Property,
        COL_COUNT,
        COL_KEY
    };
};

class PropertyRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_PropFlags;
public:
    enum {
        COL_PropFlags,

        COL_Name,
        COL_Type,
        COL_COUNT,
        COL_KEY
    };
    USHORT GetPropFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_PropFlags);
    }
    void SetPropFlags(USHORT PropFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_PropFlags = VAL16(PropFlags);
    }
    void AddPropFlags(USHORT PropFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_PropFlags |= VAL16(PropFlags);
    }
};

class MethodSemanticsRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Semantic;
public:
    enum {
        COL_Semantic,

        COL_Method,
        COL_Association,
        COL_COUNT,
        COL_KEY = COL_Association
    };
    USHORT GetSemantic()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Semantic);
    }
    void SetSemantic(USHORT Semantic)
    {
        LIMITED_METHOD_CONTRACT;

        m_Semantic = VAL16(Semantic);
    }
};

class MethodImplRec
{
public:
    enum {
        COL_Class,                  // TypeDef where the MethodBody lives.
        COL_MethodBody,             // MethodDef or MemberRef.
        COL_MethodDeclaration,      // MethodDef or MemberRef.
        COL_COUNT,
        COL_KEY = COL_Class
    };
};

class ModuleRefRec
{
public:
    enum {
        COL_Name,
        COL_COUNT,
        COL_KEY
    };
};

class TypeSpecRec
{
public:
    enum {
        COL_Signature,
        COL_COUNT,
        COL_KEY
    };
};

class ImplMapRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_MappingFlags;
public:
    enum {
        COL_MappingFlags,

        COL_MemberForwarded,        // mdField or mdMethod.
        COL_ImportName,
        COL_ImportScope,            // mdModuleRef.
        COL_COUNT,
        COL_KEY = COL_MemberForwarded
    };
    USHORT GetMappingFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_MappingFlags);
    }
    void SetMappingFlags(USHORT MappingFlags)
    {
        LIMITED_METHOD_CONTRACT;

        m_MappingFlags = VAL16(MappingFlags);
    }

};

class FieldRVARec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_RVA;
public:
    enum{
        COL_RVA,

        COL_Field,
        COL_COUNT,
        COL_KEY = COL_Field
    };

    void Copy(FieldRVARec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_RVA = pFrom->m_RVA;
    }
    ULONG GetRVA()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_RVA);
    }
    void SetRVA(ULONG RVA)
    {
        LIMITED_METHOD_CONTRACT;

        m_RVA = VAL32(RVA);
    }
};

class ENCLogRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Token;            // Token, or like a token, but with (ixTbl|0x80) instead of token type.
    ULONG       m_FuncCode;         // Function code describing the nature of ENC change.
public:
    enum {
        COL_Token,
        COL_FuncCode,
        COL_COUNT,
        COL_KEY
    };
    ULONG GetToken()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Token);
    }
    void SetToken(ULONG Token)
    {
        LIMITED_METHOD_CONTRACT;

        m_Token = VAL32(Token);
    }

    ULONG GetFuncCode()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_FuncCode);
    }
    void SetFuncCode(ULONG FuncCode)
    {
        LIMITED_METHOD_CONTRACT;

        m_FuncCode = VAL32(FuncCode);
    }
};

class ENCMapRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Token;            // Token, or like a token, but with (ixTbl|0x80) instead of token type.
public:
    enum {
        COL_Token,
        COL_COUNT,
        COL_KEY
    };
    ULONG GetToken()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Token);
    }
    void SetToken(ULONG Token)
    {
        LIMITED_METHOD_CONTRACT;

        m_Token = VAL32(Token);
    }
};

// Assembly tables.

class AssemblyRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_HashAlgId;
    USHORT      m_MajorVersion;
    USHORT      m_MinorVersion;
    USHORT      m_BuildNumber;
    USHORT      m_RevisionNumber;
    ULONG       m_Flags;
public:
    enum {
        COL_HashAlgId,
        COL_MajorVersion,
        COL_MinorVersion,
        COL_BuildNumber,
        COL_RevisionNumber,
        COL_Flags,

        COL_PublicKey,          // Public key identifying the publisher
        COL_Name,
        COL_Locale,
        COL_COUNT,
        COL_KEY
    };

    void Copy(AssemblyRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_HashAlgId = pFrom->m_HashAlgId;
        m_MajorVersion = pFrom->m_MajorVersion;
        m_MinorVersion = pFrom->m_MinorVersion;
        m_BuildNumber = pFrom->m_BuildNumber;
        m_RevisionNumber = pFrom->m_RevisionNumber;
        m_Flags = pFrom->m_Flags;
    }

    ULONG GetHashAlgId()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_HashAlgId);
    }
    void SetHashAlgId (ULONG HashAlgId)
    {
        LIMITED_METHOD_CONTRACT;

        m_HashAlgId = VAL32(HashAlgId);
    }

    USHORT GetMajorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_MajorVersion);
    }
    void SetMajorVersion (USHORT MajorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_MajorVersion = VAL16(MajorVersion);
    }

    USHORT GetMinorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_MinorVersion);
    }
    void SetMinorVersion (USHORT MinorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_MinorVersion = VAL16(MinorVersion);
    }

    USHORT GetBuildNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_BuildNumber);
    }
    void SetBuildNumber (USHORT BuildNumber)
    {
        LIMITED_METHOD_CONTRACT;

        m_BuildNumber = VAL16(BuildNumber);
    }

    USHORT GetRevisionNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_RevisionNumber);
    }
    void SetRevisionNumber (USHORT RevisionNumber)
    {
        LIMITED_METHOD_CONTRACT;

        m_RevisionNumber = VAL16(RevisionNumber);
    }

    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags (ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }

};

class AssemblyProcessorRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Processor;
public:
    enum {
        COL_Processor,

        COL_COUNT,
        COL_KEY
    };
    ULONG GetProcessor()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Processor);
    }
    void SetProcessor(ULONG Processor)
    {
        LIMITED_METHOD_CONTRACT;

        m_Processor = VAL32(Processor);
    }
};

class AssemblyOSRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_OSPlatformId;
    ULONG       m_OSMajorVersion;
    ULONG       m_OSMinorVersion;
public:
    enum {
        COL_OSPlatformId,
        COL_OSMajorVersion,
        COL_OSMinorVersion,

        COL_COUNT,
        COL_KEY
    };
    ULONG GetOSPlatformId()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSPlatformId);
    }
    void SetOSPlatformId(ULONG OSPlatformId)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSPlatformId = VAL32(OSPlatformId);
    }

    ULONG GetOSMajorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSMajorVersion);
    }
    void SetOSMajorVersion(ULONG OSMajorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSMajorVersion = VAL32(OSMajorVersion);
    }

    ULONG GetOSMinorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSMinorVersion);
    }
    void SetOSMinorVersion(ULONG OSMinorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSMinorVersion = VAL32(OSMinorVersion);
    }

};

class AssemblyRefRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_MajorVersion;
    USHORT      m_MinorVersion;
    USHORT      m_BuildNumber;
    USHORT      m_RevisionNumber;
    ULONG       m_Flags;
public:
    enum {
        COL_MajorVersion,
        COL_MinorVersion,
        COL_BuildNumber,
        COL_RevisionNumber,
        COL_Flags,

        COL_PublicKeyOrToken,               // The public key or token identifying the publisher of the Assembly.
        COL_Name,
        COL_Locale,
        COL_HashValue,
        COL_COUNT,
        COL_KEY
    };
    void Copy(AssemblyRefRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_MajorVersion = pFrom->m_MajorVersion;
        m_MinorVersion = pFrom->m_MinorVersion;
        m_BuildNumber = pFrom->m_BuildNumber;
        m_RevisionNumber = pFrom->m_RevisionNumber;
        m_Flags = pFrom->m_Flags;
    }
    USHORT  GetMajorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_MajorVersion);
    }
    void SetMajorVersion(USHORT MajorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_MajorVersion = VAL16(MajorVersion);
    }

    USHORT GetMinorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_MinorVersion);
    }
    void SetMinorVersion(USHORT MinorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_MinorVersion = VAL16(MinorVersion);
    }

    USHORT GetBuildNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_BuildNumber);
    }
    void SetBuildNumber(USHORT BuildNumber)
    {
        LIMITED_METHOD_CONTRACT;

        m_BuildNumber = VAL16(BuildNumber);
    }

    USHORT GetRevisionNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_RevisionNumber);
    }
    void SetRevisionNumber(USHORT RevisionNumber)
    {
        LIMITED_METHOD_CONTRACT;

        m_RevisionNumber = RevisionNumber;
    }

    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }

};

class AssemblyRefProcessorRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Processor;
public:
    enum {
        COL_Processor,

        COL_AssemblyRef,                // mdtAssemblyRef
        COL_COUNT,
        COL_KEY
    };
    ULONG GetProcessor()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Processor);
    }
    void SetProcessor(ULONG Processor)
    {
        LIMITED_METHOD_CONTRACT;

        m_Processor = VAL32(Processor);
    }
};

class AssemblyRefOSRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_OSPlatformId;
    ULONG       m_OSMajorVersion;
    ULONG       m_OSMinorVersion;
public:
    enum {
        COL_OSPlatformId,
        COL_OSMajorVersion,
        COL_OSMinorVersion,

        COL_AssemblyRef,                // mdtAssemblyRef.
        COL_COUNT,
        COL_KEY
    };
    ULONG       GetOSPlatformId()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSPlatformId);
    }
    void SetOSPlatformId(ULONG OSPlatformId)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSPlatformId = VAL32(OSPlatformId);
    }

    ULONG GetOSMajorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSMajorVersion);
    }
    void SetOSMajorVersion(ULONG OSMajorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSMajorVersion = VAL32(OSMajorVersion);
    }

    ULONG GetOSMinorVersion()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_OSMinorVersion);
    }
    void SetOSMinorVersion(ULONG OSMinorVersion)
    {
        LIMITED_METHOD_CONTRACT;

        m_OSMinorVersion = VAL32(OSMinorVersion);
    }
};

class FileRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Flags;
public:
    enum {
        COL_Flags,

        COL_Name,
        COL_HashValue,
        COL_COUNT,
        COL_KEY
    };
    void Copy(FileRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = pFrom->m_Flags;
    }
    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }
};

class ExportedTypeRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Flags;
    ULONG       m_TypeDefId;
public:
    enum {
        COL_Flags,
        COL_TypeDefId,

        COL_TypeName,
        COL_TypeNamespace,
        COL_Implementation,         // mdFile or mdAssemblyRef.
        COL_COUNT,
        COL_KEY
    };
    void Copy(ExportedTypeRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = pFrom->m_Flags;
        m_TypeDefId = pFrom->m_TypeDefId;
    }
    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }

    ULONG GetTypeDefId()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_TypeDefId);
    }
    void SetTypeDefId(ULONG TypeDefId)
    {
        LIMITED_METHOD_CONTRACT;

        m_TypeDefId = VAL32(TypeDefId);
    }
};

class ManifestResourceRec
{
METADATA_FIELDS_PROTECTION:
    ULONG       m_Offset;
    ULONG       m_Flags;
public:
    enum {
        COL_Offset,
        COL_Flags,

        COL_Name,
        COL_Implementation,         // mdFile or mdAssemblyRef.
        COL_COUNT,
        COL_KEY
    };
    void Copy(ManifestResourceRec *pFrom)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = pFrom->m_Flags;
        m_Offset = pFrom->m_Offset;
    }

    ULONG GetOffset()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Offset);
    }
    void SetOffset(ULONG Offset)
    {
        LIMITED_METHOD_CONTRACT;

        m_Offset = VAL32(Offset);
    }

    ULONG GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL32(&m_Flags);
    }
    void SetFlags(ULONG Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL32(Flags);
    }

};

// End Assembly Tables.

class NestedClassRec
{
public:
    enum {
        COL_NestedClass,
        COL_EnclosingClass,
        COL_COUNT,
        COL_KEY = COL_NestedClass
    };
};

// Generics


class GenericParamRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Number;               // index; zero = first var
    USHORT      m_Flags;                // index; zero = first var
public:
    enum {

        COL_Number,                     // index; zero = first var
        COL_Flags,                      // flags, for future use
        COL_Owner,                      // typeDef/methodDef
        COL_Name,                       // Purely descriptive, not used for binding purposes
        COL_COUNT,
        COL_KEY = COL_Owner
    };

    USHORT GetNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Number);
    }
    void SetNumber(USHORT Number)
    {
        LIMITED_METHOD_CONTRACT;

        m_Number = VAL16(Number);
    }

    USHORT GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Flags);
    }
    void SetFlags(USHORT Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL16(Flags);
    }
};

// @todo: this definition is for reading the old (and wrong) GenericParamRec from a
// Beta1 assembly.
class GenericParamV1_1Rec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Number;               // index; zero = first var
    USHORT      m_Flags;                // index; zero = first var
public:
    enum {

        COL_Number,                     // index; zero = first var
        COL_Flags,                      // flags, for future use
        COL_Owner,                      // typeDef/methodDef
        COL_Name,                       // Purely descriptive, not used for binding purposes
        COL_Kind,                       // typeDef/Ref/Spec, reserved for future use
        COL_COUNT,
        COL_KEY = COL_Owner
    };

    USHORT GetNumber()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Number);
    }
    void SetNumber(USHORT Number)
    {
        LIMITED_METHOD_CONTRACT;

        m_Number = VAL16(Number);
    }

    USHORT GetFlags()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Flags);
    }
    void SetFlags(USHORT Flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_Flags = VAL16(Flags);
    }
};

class MethodSpecRec
{
public:
    enum {
        COL_Method,                 // methodDef/memberRef
        COL_Instantiation,          // signature
        COL_COUNT,
        COL_KEY
    };
};


class GenericParamConstraintRec
{
public:
    enum {

        COL_Owner,                                      // GenericParam
        COL_Constraint,                                 // typeDef/Ref/Spec
        COL_COUNT,
        COL_KEY = COL_Owner
    };
};

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
/* Portable PDB tables */
// -- Dummy records to fill the gap to 0x30
class DummyRec
{
public:
    enum {
        COL_COUNT,
        COL_KEY
    };
};
class Dummy1Rec : public DummyRec {};
class Dummy2Rec : public DummyRec {};
class Dummy3Rec : public DummyRec {};

class DocumentRec
{
public:
    enum {
        COL_Name,
        COL_HashAlgorithm,
        COL_Hash,
        COL_Language,
        COL_COUNT,
        COL_KEY
    };
};

class MethodDebugInformationRec
{
public:
    enum {
        COL_Document,
        COL_SequencePoints,
        COL_COUNT,
        COL_KEY
    };
};

class LocalScopeRec
{
METADATA_FIELDS_PROTECTION:
    // [IMPORTANT]: Assigning values directly can override other columns, use PutCol instead
    ULONG      m_StartOffset;
    // [IMPORTANT]: Assigning values directly can override other columns, use PutCol instead
    ULONG      m_Length;
public:
    enum {
        COL_Method,
        COL_ImportScope,
        COL_VariableList,
        COL_ConstantList,
        COL_StartOffset,
        COL_Length,
        COL_COUNT,
        COL_KEY
    };
};

class LocalVariableRec
{
METADATA_FIELDS_PROTECTION:
    USHORT      m_Attributes;
    USHORT      m_Index;
public:
    enum {
        COL_Attributes,
        COL_Index,
        COL_Name,
        COL_COUNT,
        COL_KEY
    };

    USHORT GetAttributes()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Attributes);
    }
    void SetAttributes(USHORT attributes)
    {
        LIMITED_METHOD_CONTRACT;

        m_Attributes = VAL16(attributes);
    }

    USHORT GetIndex()
    {
        LIMITED_METHOD_CONTRACT;

        return GET_UNALIGNED_VAL16(&m_Index);
    }
    void SetIndex(USHORT index)
    {
        LIMITED_METHOD_CONTRACT;

        m_Index = VAL16(index);
    }
};

class LocalConstantRec
{
public:
    enum {
        COL_Name,
        COL_Signature,
        COL_COUNT,
        COL_KEY
    };
};

class ImportScopeRec
{
public:
    enum {
        COL_Parent,
        COL_Imports,
        COL_COUNT,
        COL_KEY
    };
};

// TODO:
// class StateMachineMethodRec
// class CustomDebugInformationRec
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#include <poppack.h>

// List of MiniMd tables.

#define MiniMdTables()          \
    MiniMdTable(Module)         \
    MiniMdTable(TypeRef)        \
    MiniMdTable(TypeDef)        \
    MiniMdTable(FieldPtr)       \
    MiniMdTable(Field)          \
    MiniMdTable(MethodPtr)      \
    MiniMdTable(Method)         \
    MiniMdTable(ParamPtr)       \
    MiniMdTable(Param)          \
    MiniMdTable(InterfaceImpl)  \
    MiniMdTable(MemberRef)      \
    MiniMdTable(Constant)       \
    MiniMdTable(CustomAttribute)\
    MiniMdTable(FieldMarshal)   \
    MiniMdTable(DeclSecurity)   \
    MiniMdTable(ClassLayout)    \
    MiniMdTable(FieldLayout)    \
    MiniMdTable(StandAloneSig)  \
    MiniMdTable(EventMap)       \
    MiniMdTable(EventPtr)       \
    MiniMdTable(Event)          \
    MiniMdTable(PropertyMap)    \
    MiniMdTable(PropertyPtr)    \
    MiniMdTable(Property)       \
    MiniMdTable(MethodSemantics)\
    MiniMdTable(MethodImpl)     \
    MiniMdTable(ModuleRef)      \
    MiniMdTable(TypeSpec)       \
    MiniMdTable(ImplMap)        \
    MiniMdTable(FieldRVA)       \
    MiniMdTable(ENCLog)         \
    MiniMdTable(ENCMap)         \
    MiniMdTable(Assembly)       \
    MiniMdTable(AssemblyProcessor)  \
    MiniMdTable(AssemblyOS)     \
    MiniMdTable(AssemblyRef)    \
    MiniMdTable(AssemblyRefProcessor)   \
    MiniMdTable(AssemblyRefOS)  \
    MiniMdTable(File)           \
    MiniMdTable(ExportedType)   \
    MiniMdTable(ManifestResource)   \
    MiniMdTable(NestedClass)    \
    MiniMdTable(GenericParam)     \
    MiniMdTable(MethodSpec)     \
    MiniMdTable(GenericParamConstraint)

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
#define PortablePdbMiniMdTables() \
    /* Dummy tables to fill the gap to 0x30 */ \
    MiniMdTable(Dummy1)                             /* 0x2D */ \
    MiniMdTable(Dummy2)                             /* 0x2E */ \
    MiniMdTable(Dummy3)                             /* 0x2F */ \
    /* Actual portable PDB tables */ \
    MiniMdTable(Document)                           /* 0x30 */ \
    MiniMdTable(MethodDebugInformation)             /* 0x31 */ \
    MiniMdTable(LocalScope)                         /* 0x32 */ \
    MiniMdTable(LocalVariable)                      /* 0x33 */ \
    MiniMdTable(LocalConstant)                      /* 0x34 */ \
    MiniMdTable(ImportScope)                        /* 0x35 */ \
    // TODO:
    // MiniMdTable(StateMachineMethod)                 /* 0x36 */
    // MiniMdTable(CustomDebugInformation)             /* 0x37 */
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#undef MiniMdTable
#define MiniMdTable(x) TBL_##x,
enum {
    MiniMdTables()
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    PortablePdbMiniMdTables()
#endif
    TBL_COUNT,                              // Highest table.
    TBL_COUNT_V1 = TBL_NestedClass + 1,    // Highest table in v1.0 database
#ifndef FEATURE_METADATA_EMIT_PORTABLE_PDB
    TBL_COUNT_V2 = TBL_GenericParamConstraint + 1 // Highest in v2.0 database
#else
    TBL_COUNT_V2 = TBL_ImportScope + 1 // Highest in portable PDB database
#endif
};
#undef MiniMdTable

// List of MiniMd coded token types.
#define MiniMdCodedTokens()                 \
    MiniMdCodedToken(TypeDefOrRef)          \
    MiniMdCodedToken(HasConstant)           \
    MiniMdCodedToken(HasCustomAttribute)    \
    MiniMdCodedToken(HasFieldMarshal)       \
    MiniMdCodedToken(HasDeclSecurity)       \
    MiniMdCodedToken(MemberRefParent)       \
    MiniMdCodedToken(HasSemantic)           \
    MiniMdCodedToken(MethodDefOrRef)        \
    MiniMdCodedToken(MemberForwarded)       \
    MiniMdCodedToken(Implementation)        \
    MiniMdCodedToken(CustomAttributeType)   \
    MiniMdCodedToken(ResolutionScope)       \
    MiniMdCodedToken(TypeOrMethodDef)       \

#undef MiniMdCodedToken
#define MiniMdCodedToken(x) CDTKN_##x,
enum {
    MiniMdCodedTokens()
    CDTKN_COUNT
};
#undef MiniMdCodedToken

//*****************************************************************************
// Meta-meta data.  Constant across all MiniMds.
//*****************************************************************************
#ifndef _META_DATA_META_CONSTANTS_DEFINED
#define _META_DATA_META_CONSTANTS_DEFINED
const unsigned int iRidMax          = 63;
const unsigned int iCodedToken      = 64;   // base of coded tokens.
const unsigned int iCodedTokenMax   = 95;
const unsigned int iSHORT           = 96;   // fixed types.
const unsigned int iUSHORT          = 97;
const unsigned int iLONG            = 98;
const unsigned int iULONG           = 99;
const unsigned int iBYTE            = 100;
const unsigned int iSTRING          = 101;  // pool types.
const unsigned int iGUID            = 102;
const unsigned int iBLOB            = 103;

inline int IsRidType(ULONG ix) {LIMITED_METHOD_CONTRACT;  return ix <= iRidMax; }
inline int IsCodedTokenType(ULONG ix) {LIMITED_METHOD_CONTRACT;  return (ix >= iCodedToken) && (ix <= iCodedTokenMax); }
inline int IsRidOrToken(ULONG ix) {LIMITED_METHOD_CONTRACT; return ix <= iCodedTokenMax; }
inline int IsHeapType(ULONG ix) {LIMITED_METHOD_CONTRACT;  return ix >= iSTRING; }
inline int IsFixedType(ULONG ix) {LIMITED_METHOD_CONTRACT;  return (ix < iSTRING) && (ix > iCodedTokenMax); }
#endif


enum MDPools {
    MDPoolStrings,                      // Id for the string pool.
    MDPoolGuids,                        // ...the GUID pool.
    MDPoolBlobs,                        // ...the blob pool.
    MDPoolUSBlobs,                      // ...the user string pool.

    MDPoolCount,                        // Count of pools, for array sizing.
}; // enum MDPools


struct CCodedTokenDef
{
    ULONG       m_cTokens;              // Count of tokens.
    const mdToken *m_pTokens;           // Array of tokens.
    const char  *m_pName;               // Name of the coded-token type.
};

struct CMiniColDef
{
    BYTE        m_Type;                 // Type of the column.
    BYTE        m_oColumn;              // Offset of the column.
    BYTE        m_cbColumn;             // Size of the column.
};

struct CMiniTableDef
{
    CMiniColDef *m_pColDefs;            // Array of field defs.
    BYTE        m_cCols;                // Count of columns in the table.
    BYTE        m_iKey;                 // Column which is the key, if any.
    USHORT      m_cbRec;                // Size of the records.
};
struct CMiniTableDefEx
{
    CMiniTableDef   m_Def;              // Table definition.
    const char  * const *m_pColNames;   // Array of column names.
    const char  *m_pName;               // Name of the table.
};

#endif // _METAMODELPUB_H_
// eof ------------------------------------------------------------------------
