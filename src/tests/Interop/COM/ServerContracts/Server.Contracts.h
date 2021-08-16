// Created by Microsoft (R) C/C++ Compiler

#pragma once
#pragma pack(push, 8)

#include <comdef.h>
#include <inspectable.h>

struct HFA_4
{
    float x;
    float y;
    float z;
    float w;
};

struct __declspec(uuid("05655a94-a915-4926-815d-a9ea648baad9"))
INumericTesting : IUnknown
{
      virtual HRESULT STDMETHODCALLTYPE Add_Byte (
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out,retval]*/ unsigned char * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Short (
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out,retval]*/ short * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UShort (
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out,retval]*/ unsigned short * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Int (
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[out,retval]*/ int * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UInt (
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[out,retval]*/ unsigned int * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Long (
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out,retval]*/ __int64 * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_ULong (
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out,retval]*/ unsigned __int64 * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Float (
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out,retval]*/ float * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Double (
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Byte_Ref (
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[in,out]*/ unsigned char * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Short_Ref (
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[in,out]*/ short * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UShort_Ref (
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[in,out]*/ unsigned short * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Int_Ref (
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[in,out]*/ int * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UInt_Ref (
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[in,out]*/ unsigned int * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Long_Ref (
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[in,out]*/ __int64 * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_ULong_Ref (
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[in,out]*/ unsigned __int64 * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Float_Ref (
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[in,out]*/ float * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Double_Ref (
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[in,out]*/ double * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Byte_Out (
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out]*/ unsigned char * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Short_Out (
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out]*/ short * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UShort_Out (
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out]*/ unsigned short * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Int_Out (
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[out]*/ int * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_UInt_Out (
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[out]*/ unsigned int * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Long_Out (
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out]*/ __int64 * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_ULong_Out (
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out]*/ unsigned __int64 * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Float_Out (
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out]*/ float * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_Double_Out (
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out]*/ double * c ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_ManyInts11 (
        /*[in]*/ int i1,
        /*[in]*/ int i2,
        /*[in]*/ int i3,
        /*[in]*/ int i4,
        /*[in]*/ int i5,
        /*[in]*/ int i6,
        /*[in]*/ int i7,
        /*[in]*/ int i8,
        /*[in]*/ int i9,
        /*[in]*/ int i10,
        /*[in]*/ int i11,
        /*[out]*/ int * result ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_ManyInts12 (
        /*[in]*/ int i1,
        /*[in]*/ int i2,
        /*[in]*/ int i3,
        /*[in]*/ int i4,
        /*[in]*/ int i5,
        /*[in]*/ int i6,
        /*[in]*/ int i7,
        /*[in]*/ int i8,
        /*[in]*/ int i9,
        /*[in]*/ int i10,
        /*[in]*/ int i11,
        /*[in]*/ int i12,
        /*[out]*/ int * result ) = 0;
};

struct __declspec(uuid("7731cb31-e063-4cc8-bcd2-d151d6bc8f43"))
IArrayTesting : IUnknown
{
      virtual HRESULT STDMETHODCALLTYPE Mean_Byte_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ unsigned char * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Short_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ short * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UShort_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ unsigned short * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Int_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ int * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UInt_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ unsigned int * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Long_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ __int64 * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_ULong_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ unsigned __int64 * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Float_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ float * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Double_LP_PreLen (
        /*[in]*/ int len,
        /*[in]*/ double * d,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Byte_LP_PostLen (
        /*[in]*/ unsigned char * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Short_LP_PostLen (
        /*[in]*/ short * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UShort_LP_PostLen (
        /*[in]*/ unsigned short * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Int_LP_PostLen (
        /*[in]*/ int * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UInt_LP_PostLen (
        /*[in]*/ unsigned int * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Long_LP_PostLen (
        /*[in]*/ __int64 * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_ULong_LP_PostLen (
        /*[in]*/ unsigned __int64 * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Float_LP_PostLen (
        /*[in]*/ float * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Double_LP_PostLen (
        /*[in]*/ double * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Byte_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Short_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UShort_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Int_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_UInt_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Long_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_ULong_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Float_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Mean_Double_SafeArray_OutLen (
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal ) = 0;
};

struct __declspec(uuid("7044c5c0-c6c6-4713-9294-b4a4e86d58cc"))
IStringTesting : IUnknown
{
      virtual HRESULT STDMETHODCALLTYPE Add_LPStr (
        /*[in]*/ LPSTR a,
        /*[in]*/ LPSTR b,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_LPWStr (
        /*[in]*/ LPWSTR a,
        /*[in]*/ LPWSTR b,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Add_BStr (
        /*[in]*/ BSTR a,
        /*[in]*/ BSTR b,
        /*[out,retval]*/ BSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPStr (
        /*[in]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPStr_Ref (
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPStr_InRef (
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPStr_Out (
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR * b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPStr_OutAttr (
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPStr (
        /*[in,out]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPStr_Ref (
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPStr_InRef (
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPStr_Out (
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR * b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPStr_OutAttr (
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWStr (
        /*[in]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWStr_Ref (
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWStr_InRef (
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWStr_Out (
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWStr_OutAttr (
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPWStr (
        /*[in,out]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPWStr_Ref (
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPWStr_InRef (
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPWStr_Out (
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_SB_LPWStr_OutAttr (
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_BStr (
        /*[in]*/ BSTR a,
        /*[out,retval]*/ BSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_BStr_Ref (
        /*[in,out]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_BStr_InRef (
        /*[in]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_BStr_Out (
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR * b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_BStr_OutAttr (
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR b ) = 0;
      virtual HRESULT STDMETHODCALLTYPE Reverse_LPWSTR_With_LCID (
          /*[in]*/ LPWSTR a,
          /*[in]*/ LCID lcid,
          /*[out]*/ LPWSTR*  b) = 0;
      virtual HRESULT STDMETHODCALLTYPE Pass_Through_LCID(
        /*[in]*/ LCID lcidFromCulture,
        /*[out]*/ LCID* outLcid) = 0;
};

struct __declspec(uuid("592386a5-6837-444d-9de3-250815d18556"))
IErrorMarshalTesting : IUnknown
{
      virtual HRESULT STDMETHODCALLTYPE Throw_HResult (
        /*[in]*/ int hresultToReturn ) = 0;
      virtual int STDMETHODCALLTYPE Return_As_HResult (
        /*[in]*/ int hresultToReturn ) = 0;
      virtual int STDMETHODCALLTYPE Return_As_HResult_Struct (
        /*[in]*/ int hresultToReturn ) = 0;
};

enum IDispatchTesting_Exception
{
    IDispatchTesting_Exception_Disp,
    IDispatchTesting_Exception_HResult,
};

struct __declspec(uuid("a5e04c1c-474e-46d2-bbc0-769d04e12b54"))
IDispatchTesting : IDispatch
{
    virtual HRESULT STDMETHODCALLTYPE DoubleNumeric_ReturnByRef (
        /*[in]*/ unsigned char b1,
        /*[in,out]*/ unsigned char *b2,
        /*[in]*/ short s1,
        /*[in,out]*/ short *s2,
        /*[in]*/ unsigned short us1,
        /*[in,out]*/ unsigned short *us2,
        /*[in]*/ int i1,
        /*[in,out]*/ int *i2,
        /*[in]*/ unsigned int ui1,
        /*[in,out]*/ unsigned int *ui2,
        /*[in]*/ __int64 l1,
        /*[in,out]*/ __int64 *l2,
        /*[in]*/ unsigned __int64 ul1,
        /*[in,out]*/ unsigned __int64 *ul2 ) = 0;
    virtual HRESULT STDMETHODCALLTYPE Add_Float_ReturnAndUpdateByRef (
        /*[in]*/ float a,
        /*[in,out]*/ float *b,
        /*[out,retval]*/ float * pRetVal ) = 0;
    virtual HRESULT STDMETHODCALLTYPE Add_Double_ReturnAndUpdateByRef (
        /*[in]*/ double a,
        /*[in,out]*/ double *b,
        /*[out,retval]*/ double * pRetVal ) = 0;
    virtual HRESULT STDMETHODCALLTYPE TriggerException (
        /*[in]*/ enum IDispatchTesting_Exception excep,
        /*[in]*/ int errorCode) = 0;

    // Special cases
    virtual HRESULT STDMETHODCALLTYPE DoubleHVAValues(
        /*[in,out]*/ HFA_4 *input,
        /*[out,retval]*/ HFA_4 *pRetVal) = 0;

    virtual HRESULT STDMETHODCALLTYPE ExplicitGetEnumerator(
        /* [retval][out] */ IUnknown** retval) = 0;
};

struct __declspec(uuid("83AFF8E4-C46A-45DB-9D91-2ADB5164545E"))
IEventTesting : IDispatch
{
    virtual HRESULT STDMETHODCALLTYPE FireEvent() = 0;
};

struct __declspec(uuid("28ea6635-42ab-4f5b-b458-4152e78b8e86"))
TestingEvents : IDispatch
{
#define DISPATCHTESTINGEVENTS_DISPID_ONEVENT 100
    // void OnEvent(_In_z_ BSTR t);
};

struct __declspec(uuid("98cc27f0-d521-4f79-8b63-e980e3a92974"))
IAggregationTesting : IUnknown
{
    // Check if the current object is aggregated
    virtual HRESULT STDMETHODCALLTYPE IsAggregated(
        _Out_ VARIANT_BOOL *isAggregated) = 0;

    // Check if the two object represent an aggregated pair
    virtual HRESULT STDMETHODCALLTYPE AreAggregated(
        _In_ IUnknown *aggregateMaybe1,
        _In_ IUnknown *aggregateMaybe2,
        _Out_ VARIANT_BOOL *areAggregated) = 0;
};

struct __declspec(uuid("E6D72BA7-0936-4396-8A69-3B76DA1108DA"))
IColorTesting : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE AreColorsEqual(
        _In_ OLE_COLOR managed,
        _In_ OLE_COLOR native,
        _Out_ _Ret_ BOOL* areEqual) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetRed(
        _Out_ _Ret_ OLE_COLOR* color) = 0;
};

struct __declspec(uuid("6C9E230E-411F-4219-ABFD-E71F2B84FD50"))
ILicenseTesting : IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE SetNextDenyLicense(_In_ VARIANT_BOOL denyLicense) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetLicense(_Out_ BSTR *lic) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetNextLicense(_In_z_ LPCOLESTR lic) = 0;
};

struct __declspec(uuid("FB6DF997-4CEF-4DF7-ADBD-E7FA395A7E0C"))
IDefaultInterfaceTesting : IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE DefOnInterfaceRet2(_Out_ int *p) = 0;

    virtual HRESULT STDMETHODCALLTYPE DefOnClassRet3(_Out_ int *p) = 0;

    virtual HRESULT STDMETHODCALLTYPE DefOnInterfaceRet5(_Out_ int *p) = 0;
};

struct __declspec(uuid("9B3CE792-F063-427D-B48E-4354094BF7A0"))
IDefaultInterfaceTesting2 : IUnknown
{
    // Empty
};

struct __declspec(uuid("3021236a-2a9e-4a29-bf14-533842c55262"))
IInspectableTesting : IUnknown
{
};

struct __declspec(uuid("e9e1ccf9-8e93-4850-ac1c-a71692cb68c5"))
IInspectableTesting2 : IInspectable
{
    virtual HRESULT STDMETHODCALLTYPE Add(_In_ int i, _In_ int j, _Out_ _Ret_ int* retVal) = 0;
};

struct __declspec(uuid("57f396a1-58a0-425f-8807-9f938a534984"))
ITrackMyLifetimeTesting : IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE GetAllocationCountCallback(_Outptr_ void** fptr) = 0;
};

#pragma pack(pop)
