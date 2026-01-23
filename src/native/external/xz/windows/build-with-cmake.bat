@rem # SPDX-License-Identifier: 0BSD
@rem # Author: Lasse Collin
@rem #
@rem ########################################################################
@rem #
@rem # This builds XZ Utils with CMake + MinGW-w64 (GCC or Clang/LLVM).
@rem # See INSTALL-MinGW-w64_with_CMake.txt for detailed instructions.
@rem #
@rem # Summary of command line arguments:
@rem #
@rem # %1 = Path to CMake's bin directory. Example:
@rem #      C:\devel\cmake\bin
@rem #
@rem # %2 = Path to MinGW-w64's bin directory. Example:
@rem #      C:\devel\mingw64\bin
@rem #
@rem # %3 = ON or OFF: Set to ON to build liblzma.dll or OFF for
@rem #      static liblzma.a. With OFF, the *.exe files won't
@rem #      depend on liblzma.dll.
@rem #
@rem ########################################################################

setlocal
set PATH=%1;%2;%PATH%

md build || exit /b
cd build || exit /b

cmake -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release -DXZ_NLS=OFF -DBUILD_SHARED_LIBS=%3 ..\.. || exit /b
mingw32-make || exit /b
mingw32-make test || exit /b

@rem liblzma.dll might not exist so ignore errors.
strip xz.exe xzdec.exe lzmadec.exe lzmainfo.exe liblzma.dll
exit /b 0
