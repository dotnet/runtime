// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <windows.h>
#include <Objbase.h>
#include <xplatform.h>
#include <platformdefines.h>

//
// Standard function to call the managed COM.
//
template <class T> class CCWTestTemplate
{
public:
	static HRESULT CallManagedCom(IUnknown* pUnk, int* fooSuccessVal)
	{
		T *pTargetInterface = NULL;
		(*fooSuccessVal) = -1;

		HRESULT hr = pUnk->QueryInterface(_uuidof(T), reinterpret_cast<void**>(&pTargetInterface));
		if (FAILED(hr))
			return hr;

		hr = pTargetInterface->Foo(fooSuccessVal);
		pTargetInterface->Release();

		return hr;
	}
};


//
// Non Nested Interface:
//

//
// IInterfaceComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("52E5F852-BD3E-4DF2-8826-E1EC39557943")) IInterfaceComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("8FDE13DC-F917-44FF-AAC8-A638FD27D647")) IInterfaceVisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleFalse
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("0A2EF649-371D-4480-B0C7-07F455C836D3")) IInterfaceVisibleFalse : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleFalse(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleFalse>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceWithoutVisible
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("FB504D72-39C4-457F-ACF4-3E5D8A31AAE4")) IInterfaceWithoutVisible : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceWithoutVisible(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceWithoutVisible>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceNotPublic
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("11320010-13FA-4B40-8580-8CF92EE70774")) IInterfaceNotPublic : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceNotPublic(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceNotPublic>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrueNoGuid
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("ad50a327-d23a-38a4-9d6e-b32b32acf572")) IInterfaceVisibleTrueNoGuid : IUnknown
{
	STDMETHOD(Foo1)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo2)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo3)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo4)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo5)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrueNoGuid(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleTrueNoGuid>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrueNoGuidGenericInterface
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("384f0b5c-28d0-368c-8c7e-5e31a84a5c84")) IInterfaceVisibleTrueNoGuidGenericInterface : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
	STDMETHOD(Foo9)(int* fooSuccessVal, int listInt[]) = 0;
	STDMETHOD(Foo10)(int* fooSuccessVal, void* intCollection, void* stringCollection) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrueNoGuidGenericInterface(IUnknown* pUnk, int* fooSuccessVal)
{
	IInterfaceVisibleTrueNoGuidGenericInterface *pTargetInterface = NULL;
	(*fooSuccessVal) = -1;

	HRESULT hr = pUnk->QueryInterface(_uuidof(IInterfaceVisibleTrueNoGuidGenericInterface), reinterpret_cast<void**>(&pTargetInterface));
	if (FAILED(hr))
		return hr;

	hr = pTargetInterface->Foo(fooSuccessVal);
	if (FAILED(hr))
	{
		pTargetInterface->Release();
		return hr;
	}

	hr = pTargetInterface->Foo9(fooSuccessVal, NULL);
	if (FAILED(hr))
		pTargetInterface->Release();
	else
		hr = (HRESULT)(pTargetInterface->Release());

	return hr;
}

//
// IInterfaceNotVisibleNoGuid
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("b45587ec-9671-35bc-8b8e-f6bfb18a4d3a")) IInterfaceNotVisibleNoGuid : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceNotVisibleNoGuid(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceNotVisibleNoGuid>::CallManagedCom(pUnk, fooSuccessVal);
}


//
// IInterfaceGenericVisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("BA4B32D4-1D73-4605-AD0A-900A31E75BC3")) IInterfaceGenericVisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceGenericVisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceGenericVisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceComImport_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("943759D7-3552-43AD-9C4D-CC2F787CF36E")) IInterfaceComImport_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceComImport_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceComImport_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrue_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("75DE245B-0CE3-4B07-8761-328906C750B7")) IInterfaceVisibleTrue_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrue_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleTrue_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleFalse_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("C73D96C3-B005-42D6-93F5-E30AEE08C66C")) IInterfaceVisibleFalse_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleFalse_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleFalse_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrue_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("60B3917B-9CC2-40F2-A975-CD6898DA697F")) IInterfaceVisibleTrue_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrue_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleTrue_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleFalse_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("2FC59DDB-B1D0-4678-93AF-6A48E838B705")) IInterfaceVisibleFalse_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleFalse_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleFalse_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceVisibleTrue_VisibleFalse
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("C82C25FC-FBAD-4EA9-BED1-343C887464B5")) IInterfaceVisibleTrue_VisibleFalse : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceVisibleTrue_VisibleFalse(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceVisibleTrue_VisibleFalse>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// IInterfaceNotPublic_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("8A4C1691-5615-4762-8568-481DC671F9CE")) IInterfaceNotPublic_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_InterfaceNotPublic_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<IInterfaceNotPublic_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}


//
// Nested Interfaces:
//

//
// INestedInterfaceComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("1D927BC5-1530-4B8E-A183-995425CE4A0A")) INestedInterfaceComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("39209692-2568-4B1E-A6C8-A5C7F141D278")) INestedInterfaceVisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleFalse
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("1CE4B033-4927-447A-9F91-998357B32ADF")) INestedInterfaceVisibleFalse : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleFalse(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleFalse>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceWithoutVisible
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("C770422A-C363-49F1-AAA1-3EC81A452816")) INestedInterfaceWithoutVisible : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceWithoutVisible(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceWithoutVisible>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceNotPublic
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("F776FF8A-0673-49C2-957A-33C2576062ED")) INestedInterfaceNotPublic : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceNotPublic(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceNotPublic>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceNestedInClass
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("B31B4EC1-3B59-41C4-B3A0-CF89638CB837")) INestedInterfaceNestedInClass : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceNestedInClass(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceNestedInClass>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceNestedInClassNoGuid
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("486bcec9-904d-3445-871c-e7084a52eb1a")) INestedInterfaceNestedInClassNoGuid : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceNestedInClassNoGuid(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceNestedInClassNoGuid>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleTrueNoGuid
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("0ea2cb33-db9f-3655-9240-47ef1dea0f1e")) INestedInterfaceVisibleTrueNoGuid : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleTrueNoGuid(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleTrueNoGuid>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceGenericVisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("CAFBD2FF-710A-4E83-9229-42FA16963424")) INestedInterfaceGenericVisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceGenericVisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceGenericVisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceComImport_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("C57D849A-A1A9-4CDC-A609-789D79F9332C")) INestedInterfaceComImport_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceComImport_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceComImport_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleTrue_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("81F28686-F257-4B7E-A47F-57C9775BE2CE")) INestedInterfaceVisibleTrue_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleTrue_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleTrue_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleFalse_ComImport
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("FAAB7E6C-8548-429F-AD34-0CEC3EBDD7B7")) INestedInterfaceVisibleFalse_ComImport : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleFalse_ComImport(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleFalse_ComImport>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleTrue_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("BEFD79A9-D8E6-42E4-8228-1892298460D7")) INestedInterfaceVisibleTrue_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleTrue_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleTrue_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleFalse_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("5C497454-EA83-4F79-B990-4EB28505E801")) INestedInterfaceVisibleFalse_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleFalse_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleFalse_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceVisibleTrue_VisibleFalse
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("A17CF08F-EEC4-4EA5-B12C-5A603101415D")) INestedInterfaceVisibleTrue_VisibleFalse : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceVisibleTrue_VisibleFalse(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceVisibleTrue_VisibleFalse>::CallManagedCom(pUnk, fooSuccessVal);
}

//
// INestedInterfaceNotPublic_VisibleTrue
// Interface definition and auxiliary function that will call the managed COM.
//
struct __declspec(uuid("40B723E9-E1BE-4F55-99CD-D2590D191A53")) INestedInterfaceNotPublic_VisibleTrue : IUnknown
{
	STDMETHOD(Foo)(int* fooSuccessVal) = 0;
};

extern "C" DLL_EXPORT HRESULT _stdcall CCWTest_NestedInterfaceNotPublic_VisibleTrue(IUnknown* pUnk, int* fooSuccessVal)
{
	return CCWTestTemplate<INestedInterfaceNotPublic_VisibleTrue>::CallManagedCom(pUnk, fooSuccessVal);
}

