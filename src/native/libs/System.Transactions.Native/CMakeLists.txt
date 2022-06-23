project(System.Transactions.Native CXX)

set(NATIVETRANSACTION_SOURCES
    DtcProxyShim.cpp
    EnlistmentNotifyShim.cpp
    NotificationShimFactory.cpp
    Phase0NotifyShim.cpp
    ResourceManagerNotifyShim.cpp
    stdafx.cpp
    TransactionShim.cpp
    VoterNotifyShim.cpp
)

if (GEN_SHARED_LIB)
    add_library(System.Transactions.Native
        SHARED
        ${NATIVETRANSACTION_SOURCES}
        System.Transactions.Native.def
        # This will add versioning to the library
        ${VERSION_FILE_RC_PATH}
    )

#    GENERATE_EXPORT_HEADER(System.Transactions.Native
#         BASE_NAME System.Transactions.Native
#         EXPORT_MACRO_NAME System.Transactions.Native_EXPORT
#         EXPORT_FILE_NAME System.Transactions.Native_Export.h
#         STATIC_DEFINE System.Transactions.Native_BUILT_AS_STATIC
#    )

    install (TARGETS System.Transactions.Native DESTINATION .)
    install (FILES $<TARGET_PDB_FILE:System.Transactions.Native> DESTINATION .)

endif ()

add_library(System.Transactions.Native-Static
    STATIC
    ${NATIVETRANSACTION_SOURCES}
)

install (TARGETS System.Transactions.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)

