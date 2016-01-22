set( COMCRYPT_WKS_SOURCES
  Cryptography.cpp
  X509Certificate.cpp
)

add_library_clr( comcrypt_wks ${COMCRYPT_WKS_SOURCES})
