// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _APPDOMAIN_HELPER_H_
#define _APPDOMAIN_HELPER_H_

#ifndef FEATURE_REMOTING
#error FEATURE_REMOTING is not set, please do not include appdomainhelper.h
#endif

// Marshal a single object into a serialized blob.
//
//

class AppDomainHelper {

    friend class MarshalCache;

    // A pair of helper to move serialization info between managed byte-array and
    // unmanaged blob.
    static void AppDomainHelper::CopyEncodingToByteArray(IN PBYTE   pbData, 
                                                         IN DWORD   cbData, 
                                                         OUT OBJECTREF* pArray);

    static void AppDomainHelper::CopyByteArrayToEncoding(IN U1ARRAYREF* pArray,
                                                         OUT PBYTE*   ppbData,
                                                         OUT DWORD*   pcbData);

public:
    // Marshal a single object into a serialized blob.
    static void AppDomainHelper::MarshalObject(IN OBJECTREF  *orObject,
                                               OUT U1ARRAYREF *porBlob);
    
    static void AppDomainHelper::MarshalObject(IN ADID pDomain,
                                               IN OBJECTREF  *orObject,
                                               OUT U1ARRAYREF *porBlob);
    // Marshal one object into a seraialized blob.
    static void AppDomainHelper::MarshalObject(IN AppDomain *pDomain,
                                               IN OBJECTREF  *orObject,
                                               OUT BYTE    **ppbBlob,
                                               OUT DWORD    *pcbBlob);

    // Marshal two objects into serialized blobs.
    static void AppDomainHelper::MarshalObjects(IN AppDomain *pDomain,
                                                IN OBJECTREF  *orObject1,
                                                IN OBJECTREF  *orObject2,
                                                OUT BYTE    **ppbBlob1,
                                                OUT DWORD    *pcbBlob1,
                                                OUT BYTE    **ppbBlob2,
                                                OUT DWORD    *pcbBlob2);

    // Unmarshal a single object from a serialized blob.
    static void AppDomainHelper::UnmarshalObject(IN AppDomain   *pDomain,
                                                 IN U1ARRAYREF  *porBlob,
                                                 OUT OBJECTREF  *porObject);

    // Unmarshal a single object from a serialized blob.
    static void AppDomainHelper::UnmarshalObject(IN AppDomain   *pDomain,
                                                 IN BYTE        *pbBlob,
                                                 IN DWORD        cbBlob,
                                                 OUT OBJECTREF  *porObject);

    // Unmarshal two objects from serialized blobs.
    static void AppDomainHelper::UnmarshalObjects(IN AppDomain   *pDomain,
                                                  IN BYTE        *pbBlob1,
                                                  IN DWORD        cbBlob1,
                                                  IN BYTE        *pbBlob2,
                                                  IN DWORD        cbBlob2,
                                                  OUT OBJECTREF  *porObject1,
                                                  OUT OBJECTREF  *porObject2);

    // Copy an object from the given appdomain into the current appdomain.
    static OBJECTREF AppDomainHelper::CrossContextCopyFrom(IN AppDomain *pAppDomain,
                                                           IN OBJECTREF *orObject);
    // Copy an object to the given appdomain from the current appdomain.
    static OBJECTREF AppDomainHelper::CrossContextCopyTo(IN AppDomain *pAppDomain,
                                                         IN OBJECTREF  *orObject);
    // Copy an object from the given appdomain into the current appdomain.
    static OBJECTREF AppDomainHelper::CrossContextCopyFrom(IN ADID dwDomainId,
                                                           IN OBJECTREF *orObject);
    // Copy an object to the given appdomain from the current appdomain.
    static OBJECTREF AppDomainHelper::CrossContextCopyTo(IN ADID dwDomainId,
                                                         IN OBJECTREF  *orObject);

};

// Cache the bits needed to serialize/deserialize managed objects that will be
// passed across appdomain boundaries during a stackwalk. The serialization is
// performed lazily the first time it's needed and remains valid throughout the
// stackwalk. The last deserialized object is cached and tagged with its
// appdomain context. It's valid as long as we're walking frames within the same
// appdomain.
//
class MarshalCache
{
public:
    MarshalCache()
    {
        LIMITED_METHOD_CONTRACT;
        ZeroMemory(this, sizeof(*this));
    }

    ~MarshalCache()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pbObj1)
            delete [] m_pbObj1;
        if (m_pbObj2)
            delete [] m_pbObj2;
    }

    void EnsureSerializationOK()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        } 
        CONTRACTL_END;
        if ((m_sGC.m_orInput1 != NULL && (m_pbObj1 == NULL || m_cbObj1 == 0)) ||
            (m_sGC.m_orInput2 != NULL && (m_pbObj2 == NULL || m_cbObj2 == 0)))
        {
            // Serialization went bad -> Throw exception indicating so.
            COMPlusThrow(kSecurityException, IDS_UNMARSHALABLE_DEMAND_OBJECT);
        }
    }

    void EnsureDeserializationOK()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        } 
        CONTRACTL_END;
        if ((m_pbObj1 != NULL && m_sGC.m_orOutput1 == NULL ) ||
            (m_pbObj2 != NULL && m_sGC.m_orOutput2 == NULL ) )
        {
            // DeSerialization went bad -> Throw exception indicating so.
            COMPlusThrow(kSecurityException, IDS_UNMARSHALABLE_DEMAND_OBJECT);
        }
    }

#ifndef DACCESS_COMPILE
    
    // Set the original value of the first cached object.
    void SetObject(OBJECTREF orObject)
    {
        LIMITED_METHOD_CONTRACT;
        m_pOriginalDomain = ::GetAppDomain();
        m_sGC.m_orInput1 = orObject;
    }

    // Set the original values of both cached objects.
    void SetObjects(OBJECTREF orObject1, OBJECTREF orObject2)
    {
        LIMITED_METHOD_CONTRACT;
        m_pOriginalDomain = ::GetAppDomain();
        m_sGC.m_orInput1 = orObject1;
        m_sGC.m_orInput2 = orObject2;
    }

#endif //!DACCESS_COMPILE
    
    // Get a copy of the first object suitable for use in the given appdomain.
    OBJECTREF GetObject(AppDomain *pDomain)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        CheckADValidity(pDomain, ADV_RUNNINGIN);

        // No transition -- just return original object.
        if (pDomain == m_pOriginalDomain) {
            if (m_fObjectUpdated)
                UpdateObjectFinish();
            return m_sGC.m_orInput1;
        }

        // We've already deserialized the object into the correct context.
        if (pDomain == m_pCachedDomain)
            return m_sGC.m_orOutput1;

        // If we've updated the object in a different appdomain from the one we
        // originally started in, the cached object will be more up to date than
        // the original. Resync the objects.
        if (m_fObjectUpdated)
            UpdateObjectFinish();

        // Check whether we've serialized the original input object yet.
        if (m_pbObj1 == NULL && m_sGC.m_orInput1 != NULL)
        {
            AppDomainHelper::MarshalObject(m_pOriginalDomain,
                                          &m_sGC.m_orInput1,
                                          &m_pbObj1,
                                          &m_cbObj1);
            EnsureSerializationOK();
        }

        // Deserialize into the correct context.
        if (m_pbObj1 != NULL)
        {
            AppDomainHelper::UnmarshalObject(pDomain,
                                            m_pbObj1,
                                            m_cbObj1,
                                            &m_sGC.m_orOutput1);
            EnsureDeserializationOK();
        }
        m_pCachedDomain = pDomain;

        return m_sGC.m_orOutput1;
    }

    // As above, but retrieve both objects.
    OBJECTREF GetObjects(AppDomain *pDomain, OBJECTREF *porObject2)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        CheckADValidity(pDomain, ADV_RUNNINGIN);
        // No transition -- just return original objects.
        if (pDomain == m_pOriginalDomain) {
            if (m_fObjectUpdated)
                UpdateObjectFinish();
            *porObject2 = m_sGC.m_orInput2;
            return m_sGC.m_orInput1;
        }

        // We've already deserialized the objects into the correct context.
        if (pDomain == m_pCachedDomain) {
            *porObject2 = m_sGC.m_orOutput2;
            return m_sGC.m_orOutput1;
        }

        // If we've updated the object in a different appdomain from the one we
        // originally started in, the cached object will be more up to date than
        // the original. Resync the objects.
        if (m_fObjectUpdated)
            UpdateObjectFinish();

        // Check whether we've serialized the original input objects yet.
        if ((m_pbObj1 == NULL && m_sGC.m_orInput1 != NULL) ||
            (m_pbObj2 == NULL && m_sGC.m_orInput2 != NULL))
        {
            AppDomainHelper::MarshalObjects(m_pOriginalDomain,
                                           &m_sGC.m_orInput1,
                                           &m_sGC.m_orInput2,
                                           &m_pbObj1,
                                           &m_cbObj1,
                                           &m_pbObj2,
                                           &m_cbObj2);
            EnsureSerializationOK();

        }
        if (m_pbObj1 != NULL || m_pbObj2 != NULL)
        {
            // Deserialize into the correct context.
            AppDomainHelper::UnmarshalObjects(pDomain,
                                              m_pbObj1,
                                              m_cbObj1,
                                              m_pbObj2,
                                              m_cbObj2,
                                              &m_sGC.m_orOutput1,
                                              &m_sGC.m_orOutput2);
            EnsureDeserializationOK();
        }
        m_pCachedDomain = pDomain;

        *porObject2 = m_sGC.m_orOutput2;
        return m_sGC.m_orOutput1;
    }

    // Change the first object (updating the cacheing information
    // appropriately).
    void UpdateObject(AppDomain *pDomain, OBJECTREF orObject)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        // The cached serialized blob is now useless.
		CheckADValidity(pDomain, ADV_RUNNINGIN);        
        if (m_pbObj1)
            delete [] m_pbObj1;
        m_pbObj1 = NULL;
        m_cbObj1 = 0;

        // The object we have now is valid in it's own appdomain, so place that
        // in the object cache.
        m_pCachedDomain = pDomain;
        m_sGC.m_orOutput1 = orObject;

        // If the object is updated in the original context, just use the new
        // value as is. In this case we have the data to re-marshal the updated
        // object as normal, so we can consider the cache fully updated and exit
        // now.
        if (pDomain == m_pOriginalDomain) {
            m_sGC.m_orInput1 = orObject;
            m_fObjectUpdated = false;
            return;
        }

        // We want to avoid re-marshaling the updated value as long as possible
        // (it might be updated again before we need its value in a different
        // context). So set a flag to indicate that the object must be
        // re-marshaled when the value is queried in a new context.
        m_fObjectUpdated = true;
    }

    // This structure is public only so that it can be GC protected. Do not
    // access the fields directly, they change in an unpredictable fashion due
    // to the lazy cacheing algorithm.
    struct _gc {
        OBJECTREF   m_orInput1;
        OBJECTREF   m_orInput2;
        OBJECTREF   m_orOutput1;
        OBJECTREF   m_orOutput2;
    }           m_sGC;

private:

    // Called after one or more calls to UpdateObject to marshal the updated
    // object back into its original context (it's assumed we're called in this
    // context).
    void UpdateObjectFinish()
    {
		CONTRACTL
		{
			THROWS;
			GC_TRIGGERS;
			MODE_COOPERATIVE;
			PRECONDITION(m_fObjectUpdated && m_pbObj1 == NULL);
		}
		CONTRACTL_END;
        AppDomainHelper::MarshalObject(m_pCachedDomain,
                                      &m_sGC.m_orOutput1,
                                      &m_pbObj1,
                                      &m_cbObj1);
        AppDomainHelper::UnmarshalObject(m_pOriginalDomain,
                                        m_pbObj1,
                                        m_cbObj1,
                                        &m_sGC.m_orInput1);
        m_fObjectUpdated = false;
    }

    BYTE       *m_pbObj1;
    DWORD       m_cbObj1;
    BYTE       *m_pbObj2;
    DWORD       m_cbObj2;
    AppDomain  *m_pCachedDomain;
    AppDomain  *m_pOriginalDomain;
    bool        m_fObjectUpdated;
};

#endif
