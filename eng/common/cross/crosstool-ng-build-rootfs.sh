#!/usr/bin/env bash

# Builds a distro-free glibc rootfs (sysroot) for cross-compiling with the host clang.
# Nothing in the rootfs is ever executed: it only provides headers, crt objects and
# link-time libraries. The C/C++ ABI floor (glibc, libstdc++/libgcc_s) is built from
# source with crosstool-ng; the remaining dependencies are built from pinned upstream
# tarballs.
#
# To run this script standalone, the host needs a C/C++ toolchain for building crosstool-ng
# and the toolchain it bootstraps, clang/lld/llvm for cross-compiling the dependencies, and
# either wget or curl. On Ubuntu 24.04:
#
#   apt-get install build-essential autoconf automake libtool libtool-bin gperf bison flex \
#       texinfo help2man gawk libncurses-dev unzip rsync xz-utils bzip2 patch file perl python3 \
#       clang lld llvm pkg-config curl ca-certificates

set -euo pipefail

usage()
{
    echo "Usage: $0 <BuildArch> [--rootfsdir <directory>] [--cachedir <directory>]"
    echo "BuildArch can be: arm, arm64, loongarch64, ppc64le, riscv64, s390x, x64"
    echo "--rootfsdir - output directory (default: \$ROOTFS_DIR or .tools/rootfs/<BuildArch>)"
    echo "--cachedir  - directory to keep downloaded tarballs across runs"
    exit 1
}

#    name           arch         version   url                                                                                                            sha256
__Deps=(
    "ctng           *            1.29.0    https://github.com/crosstool-ng/crosstool-ng/releases/download/crosstool-ng-1.29.0/crosstool-ng-1.29.0.tar.xz  1e0c5efcf2af674993b74a1783fe78727c8d34b500ebab07eb1bb0a45c8fcc87"
    "gcc            *            8.5.0     https://mirrors.kernel.org/gnu/gcc/gcc-8.5.0/gcc-8.5.0.tar.xz                                                  d308841a511bb830a6100397b0042db24ce11f642dab6ea6ee44842e5325ed50"
    "gcc            loongarch64  14.4.0    https://mirrors.kernel.org/gnu/gcc/gcc-14.4.0/gcc-14.4.0.tar.xz                                                752b6f567beac83159c77a7680b1316bdd784738bff9a9d070112c09da90f6d9"
    "glibc          *            2.25      https://mirrors.kernel.org/gnu/glibc/glibc-2.25.tar.xz                                                         067bd9bb3390e79aa45911537d13c3721f1d9d3769931a30c2681bfee66f23a0"
    "glibc          arm          2.34      https://mirrors.kernel.org/gnu/glibc/glibc-2.34.tar.xz                                                         44d26a1fe20b8853a48f470ead01e4279e869ac149b195dda4e44a195d981ab2"
    "glibc          loongarch64  2.36      https://mirrors.kernel.org/gnu/glibc/glibc-2.36.tar.xz                                                         1c959fea240906226062cb4b1e7ebce71a9f0e3c0836c09e7e3423d434fcfe75"
    "glibc          riscv64      2.29      https://mirrors.kernel.org/gnu/glibc/glibc-2.29.tar.xz                                                         f3eeb8d57e25ca9fc13c2af3dae97754f9f643bc69229546828e3a240e2af04b"
    "icu            *            79.1      https://github.com/unicode-org/icu/releases/download/release-79.1/icu4c-79.1-sources.tgz                       06fa63ecb521048f8082f12ad988f6b678d90cbd88aafce24cbc94902b191d99"
    "krb5           *            1.22.3    https://kerberos.org/dist/krb5/1.22/krb5-1.22.3.tar.gz                                                         fe90af8441ad9fa9b064b8e53077cefb782a96f8a68276b031dc54af1789ff0b"
    "linux          *            4.19.325  https://cdn.kernel.org/pub/linux/kernel/v4.x/linux-4.19.325.tar.xz                                             607bed7de5cda31a443df4c8a78dbe5e8a9ad31afde2a4d28fe99ab4730e8de1"
    "linux          loongarch64  6.12.93   https://cdn.kernel.org/pub/linux/kernel/v6.x/linux-6.12.93.tar.xz                                              492648a87c0b69c5ac7f43be64792b9000e3439550d4e82e4a14710c49094fa3"
    "linux          riscv64      6.12.93   https://cdn.kernel.org/pub/linux/kernel/v6.x/linux-6.12.93.tar.xz                                              492648a87c0b69c5ac7f43be64792b9000e3439550d4e82e4a14710c49094fa3"
    "lldb           *            3.9.1     https://releases.llvm.org/3.9.1/lldb-3.9.1.src.tar.xz                                                          7e3311b2a1f80f4d3426e09f9459d079cab4d698258667e50a46dccbaaa460fc"
    "lttng-ust      *            2.12.10   https://lttng.org/files/lttng-ust/lttng-ust-2.12.10.tar.bz2                                                    aa802851c6d08c653ac1dd7da8f552baaa5d87e9b03c2d6a8b7a04322e53dc47"
    "openssl        *            4.1.0     https://github.com/openssl/openssl/releases/download/openssl-4.1.0/openssl-4.1.0.tar.gz                        50c17baf54cb008f6ce42d50deb4b28050a1aad711636cabd4b241c7e90cc2fd"
    "userspace-rcu  *            0.15.7    https://lttng.org/files/urcu/userspace-rcu-0.15.7.tar.bz2                                                      2556b83adc0f9b3ac8024e613e17d014d04c4c49110604ce55fcb14eae32edd3"
)

__CrossDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
__BuildArch=
__RootfsDir="${ROOTFS_DIR:-}"
__CacheDir=

while [[ $# -gt 0 ]]; do
    case "$1" in
        -h|--help) usage ;;
        --rootfsdir|-rootfsdir) shift; __RootfsDir="$1" ;;
        --cachedir|-cachedir) shift; __CacheDir="$1" ;;
        *) [[ -z "$__BuildArch" ]] && __BuildArch="$1" || usage ;;
    esac
    shift
done

case "$__BuildArch" in
    arm)
        __Triple=arm-linux-gnueabihf
        __CtngArch='CT_ARCH_ARM=y
CT_ARCH_ARCH="armv7-a"
CT_ARCH_FPU="vfpv3-d16"
CT_ARCH_FLOAT_HW=y'
        __OpenSSLTarget=linux-armv4
        ;;
    arm64)
        __Triple=aarch64-linux-gnu
        __CtngArch='CT_ARCH_ARM=y
CT_ARCH_64=y'
        __OpenSSLTarget=linux-aarch64
        ;;
    loongarch64)
        __Triple=loongarch64-linux-gnu
        __CtngArch='CT_ARCH_LOONGARCH=y
CT_ARCH_USE_MMU=y
CT_ARCH_ARCH="loongarch64"
# CT_DEMULTILIB is not set'
        __OpenSSLTarget=linux64-loongarch64
        ;;
    ppc64le)
        __Triple=powerpc64le-linux-gnu
        __CtngArch='CT_ARCH_POWERPC=y
CT_ARCH_64=y
CT_ARCH_LE=y'
        __OpenSSLTarget=linux-ppc64le
        ;;
    riscv64)
        __Triple=riscv64-linux-gnu
        __CtngArch='CT_ARCH_RISCV=y
CT_ARCH_64=y
CT_ARCH_USE_MMU=y
CT_ARCH_ARCH="rv64gc"
# CT_DEMULTILIB is not set'
        __OpenSSLTarget=linux64-riscv64
        ;;
    s390x)
        __Triple=s390x-linux-gnu
        __CtngArch='CT_ARCH_S390=y
CT_ARCH_64=y'
        __OpenSSLTarget=linux64-s390x
        ;;
    x64)
        __Triple=x86_64-linux-gnu
        __CtngArch='CT_ARCH_X86=y
CT_ARCH_64=y'
        __OpenSSLTarget=linux-x86_64
        ;;
    *)
        usage
        ;;
esac

if [[ -z "$__RootfsDir" ]]; then
    __RootfsDir="$__CrossDir/../../../.tools/rootfs/$__BuildArch"
fi

# Prints field <n> (3=version, 4=url, 5=sha256) of the entry for <name>, preferring the
# row for the current architecture over the '*' row.
dep()
{
    local name="$1" field="$2" row value=
    for row in "${__Deps[@]}"; do
        read -r -a cols <<< "$row"
        if [[ "${cols[0]}" == "$name" ]]; then
            if [[ "${cols[1]}" == "$__BuildArch" ]]; then
                echo "${cols[$((field - 1))]}"
                return
            elif [[ "${cols[1]}" == "*" ]]; then
                value="${cols[$((field - 1))]}"
            fi
        fi
    done
    echo "$value"
}

__WorkDir="$(mktemp -d)"
trap 'rm -rf "$__WorkDir"' EXIT
__CacheDir="${__CacheDir:-$__WorkDir/cache}"
mkdir -p "$__CacheDir"
JOBS="${MAXJOBS:-$(getconf _NPROCESSORS_ONLN)}"

ensureDownloadTool()
{
    if command -v wget &> /dev/null; then
        __hasWget=1
    elif command -v curl &> /dev/null; then
        __hasWget=0
    else
        >&2 echo "ERROR: either wget or curl is required by this script."
        exit 1
    fi
}

archive() { echo "$__CacheDir/$(basename "$(dep "$1" 4)")"; }

# Downloads <name> into the cache (unless it is already there) and verifies its sha256.
download()
{
    local name="$1" url file
    url="$(dep "$name" 4)"
    file="$(archive "$name")"
    if [[ ! -f "$file" ]]; then
        if [[ "$__hasWget" == 1 ]]; then
            wget -nv -O "$file.tmp" "$url"
        else
            curl -fsSL --retry 3 -o "$file.tmp" "$url"
        fi
        mv "$file.tmp" "$file"
    fi
    echo "$(dep "$name" 5)  $file" | sha256sum -c --quiet -
}

unpack()
{
    local name="$1"
    mkdir -p "$__WorkDir/src/$name"
    tar -C "$__WorkDir/src/$name" --strip-components=1 -xf "$(archive "$name")"
    echo "$__WorkDir/src/$name"
}

# Names that resolve to something for this architecture, in table order.
deps()
{
    local row name seen=" "
    for row in "${__Deps[@]}"; do
        read -r name _ <<< "$row"
        if [[ "$seen" != *" $name "* ]]; then
            seen+="$name "
            [[ -n "$(dep "$name" 3)" && "$(dep "$name" 3)" != "-" ]] && echo "$name"
        fi
    done
}

# Dependencies built on top of the base, in build order.
__BuildOrder=(userspace-rcu lttng-ust openssl icu krb5 lldb)

ensureDownloadTool
__Pids=()
for __Dep in $(deps); do
    download "$__Dep" &
    __Pids+=($!)
done
for __Pid in "${__Pids[@]}"; do
    wait "$__Pid"
done

# 1. crosstool-ng
__CtngSrc="$(unpack ctng)"
(cd "$__CtngSrc" && ./configure --enable-local && make -j"$JOBS")

# 2. base: kernel UAPI headers, glibc and the GCC runtime (cached per version set)
__Prefix="$__CacheDir/x-tools/$__BuildArch-ctng$(dep ctng 3)-gcc$(dep gcc 3)-glibc$(dep glibc 3)-linux$(dep linux 3)"
if [[ ! -f "$__Prefix/.complete" ]]; then
rm -rf "$__Prefix"
mkdir -p "$__WorkDir/ctng-build"
__CtLinux="CT_LINUX_V_$(dep linux 3 | cut -d. -f1-2 | tr . _)=y"
__CtGlibc="CT_GLIBC_V_$(dep glibc 3 | cut -d. -f1-2 | tr . _)=y"
__CtGcc="CT_GCC_V_$(dep gcc 3 | cut -d. -f1)=y"
cat > "$__WorkDir/ctng-build/defconfig" <<EOF
CT_CONFIG_VERSION="4"
CT_EXPERIMENTAL=y
CT_ALLOW_BUILD_AS_ROOT=y
CT_ALLOW_BUILD_AS_ROOT_SURE=y
CT_PREFIX_DIR="$__Prefix"
CT_LOCAL_TARBALLS_DIR="$__CacheDir"
CT_SAVE_TARBALLS=y
CT_VERIFY_DOWNLOAD_DIGEST_SHA256=y
CT_LOG_PROGRESS_BAR=n
# CT_PREFIX_DIR_RO is not set
CT_OMIT_TARGET_VENDOR=y
$__CtngArch
CT_KERNEL_LINUX=y
$__CtLinux
CT_LINUX_VERSION="$(dep linux 3)"
$__CtGlibc
$__CtGcc
CT_CC_LANG_CXX=y
# The rootfs only needs headers and libraries; don't build documentation.
CT_GLIBC_EXTRA_CONFIG_ARRAY="MAKEINFO=:"
EOF
(
    cd "$__WorkDir/ctng-build"
    "$__CtngSrc/ct-ng" defconfig DEFCONFIG=defconfig
    # crosstool-ng silently falls back to its defaults for versions it can't select.
    for __Opt in "$__CtLinux" "$__CtGlibc" "$__CtGcc"; do
        if ! grep -qx "$__Opt" .config; then
            echo "ERROR: crosstool-ng did not accept $__Opt for $__BuildArch." >&2
            exit 1
        fi
    done
    "$__CtngSrc/ct-ng" "build.$JOBS"
)
touch "$__Prefix/.complete"
fi

__CtngTriple="$(basename "$(find "$__Prefix/bin" -name '*-gcc' -type f | head -n 1)" -gcc)"
if [[ "$__CtngTriple" != "$__Triple" ]]; then
    echo "ERROR: crosstool-ng produced '$__CtngTriple', expected '$__Triple'." >&2
    exit 1
fi

# 3. collect into the layout clang and toolchain.cmake expect
mkdir -p "$__RootfsDir"
find "$__RootfsDir" -mindepth 1 -delete
cp -a --no-preserve=ownership "$__Prefix/$__Triple/sysroot/." "$__RootfsDir/"
chmod -R u+w "$__RootfsDir"

__GccVer="$(basename "$(find "$__Prefix/lib/gcc/$__Triple" -mindepth 1 -maxdepth 1 -type d | head -n 1)")"
__GccDir="$__RootfsDir/usr/lib/gcc/$__Triple/$__GccVer"
mkdir -p "$__GccDir" "$__RootfsDir/usr/include/c++"
cp -a --no-preserve=ownership "$__Prefix/lib/gcc/$__Triple/$__GccVer/"*.o "$__Prefix/lib/gcc/$__Triple/$__GccVer/"*.a "$__GccDir/"
find "$__Prefix/$__Triple" -path "$__Prefix/$__Triple/sysroot" -prune -o \
    \( -name 'libstdc++.*' -o -name 'libsupc++.*' -o -name 'libgcc_s.*' -o -name 'libatomic.*' \) -print |
    grep -v -E '\.py$|\.la$' | xargs -I{} cp -a --no-preserve=ownership {} "$__GccDir/"
cp -a --no-preserve=ownership "$__Prefix/$__Triple/include/c++/$__GccVer" "$__RootfsDir/usr/include/c++/"

# 4. dependencies, cross-compiled with the host clang against the new rootfs
__Clang="--target=$__Triple --sysroot=$__RootfsDir"
export CC="clang $__Clang" CXX="clang++ $__Clang" LD=ld.lld AR=llvm-ar RANLIB=llvm-ranlib NM=llvm-nm STRIP=llvm-strip OBJDUMP=llvm-objdump READELF=llvm-readelf
export LDFLAGS="-fuse-ld=lld"
export PKG_CONFIG_SYSROOT_DIR="$__RootfsDir" PKG_CONFIG_LIBDIR="$__RootfsDir/usr/lib/pkgconfig:$__RootfsDir/usr/share/pkgconfig"

autotools()
{
    ./configure --host="$__Triple" --prefix=/usr --libdir=/usr/lib --disable-static "$@"
    make -j"$JOBS"
    make DESTDIR="$__RootfsDir" install
    # libtool archives record unprefixed /usr/lib paths, which breaks later links against the rootfs.
    find "$__RootfsDir" -name '*.la' -delete
}

build_openssl()
{
    ./Configure "$__OpenSSLTarget" --prefix=/usr --libdir=lib no-tests no-docs no-apps
    make -j"$JOBS" build_libs
    make DESTDIR="$__RootfsDir" install_dev
}

build_icu()
{
    # Only headers and the uc/i18n libraries are needed (ICU is loaded at runtime), so skip the
    # data and tools. ICU's cross-build support only needs config/icucross.{mk,inc} from a native tree.
    mkdir host target
    (cd host && env -u CC -u CXX -u LD -u AR -u RANLIB -u NM -u STRIP -u OBJDUMP -u READELF -u LDFLAGS \
        ../source/configure --disable-tests --disable-samples --disable-extras && make config/icucross.mk config/icucross.inc)
    cd target
    ../source/configure --host="$__Triple" --with-cross-build="$PWD/../host" --prefix=/usr --libdir=/usr/lib \
        --disable-tests --disable-samples --disable-extras --disable-tools
    mkdir -p lib bin
    local dir
    for dir in stubdata common i18n; do
        make -C "$dir" -j"$JOBS"
        make -C "$dir" DESTDIR="$__RootfsDir" install
    done
}

build_krb5()
{
    # Only the GSSAPI headers and libraries are needed (libgssapi_krb5 is loaded at runtime),
    # so skip the KDC, admin tools, clients and plugins, and don't depend on OpenSSL.
    cd src
    krb5_cv_attr_constructor_destructor=yes,yes ac_cv_func_regcomp=yes ac_cv_printf_positional=yes \
        ./configure --host="$__Triple" --prefix=/usr --libdir=/usr/lib --disable-static --disable-rpath \
        --with-crypto-impl=builtin --with-tls-impl=no --without-spake-openssl --disable-pkinit \
        --without-system-verto --without-keyutils --without-libedit --without-ldap
    make DESTDIR="$__RootfsDir" install-mkdirs
    local dir
    for dir in util include lib; do
        make -C "$dir" -j"$JOBS"
        make -C "$dir" DESTDIR="$__RootfsDir" install
    done
}

build_lldb()
{
    # The SOS plugin only compiles against the public SB API headers; lldb provides the symbols at load time.
    mkdir -p "$__RootfsDir/usr/include"
    cp -R include/lldb "$__RootfsDir/usr/include/"
}

build_userspace_rcu() { autotools; }
build_lttng_ust()     { autotools --disable-man-pages --disable-examples --disable-numa; }

for __Dep in "${__BuildOrder[@]}"; do
    [[ " $(deps | tr '\n' ' ') " == *" $__Dep "* ]] || continue
    echo "Building $__Dep $(dep "$__Dep" 3)"
    (cd "$(unpack "$__Dep")" && "build_${__Dep//-/_}")
done

# 5. keep only what the host toolchain reads
rm -rf "$__RootfsDir"/{bin,sbin,etc,var,usr/bin,usr/sbin,usr/libexec,usr/share}
rm -rf "$__RootfsDir"/usr/lib*/{gconv,audit,getconf}
find "$__RootfsDir" -name '*.la' -delete
find "$__RootfsDir" -type f \( -name '*.so*' -o -name '*.a' -o -name '*.o' \) -exec llvm-strip --strip-debug {} + 2>/dev/null || true

echo "rootfs for $__BuildArch ($__Triple, glibc $(dep glibc 3), gcc $__GccVer, linux $(dep linux 3)): $__RootfsDir ($(du -sh "$__RootfsDir" | cut -f1))"
