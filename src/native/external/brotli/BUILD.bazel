# Description:
#   Brotli is a generic-purpose lossless compression algorithm.

load(":compiler_config_setting.bzl", "create_msvc_config")

package(
    default_visibility = ["//visibility:public"],
)

licenses(["notice"])  # MIT

exports_files(["LICENSE"])

config_setting(
    name = "darwin",
    values = {"cpu": "darwin"},
    visibility = ["//visibility:public"],
)

config_setting(
    name = "darwin_x86_64",
    values = {"cpu": "darwin_x86_64"},
    visibility = ["//visibility:public"],
)

config_setting(
    name = "windows",
    values = {"cpu": "x64_windows"},
    visibility = ["//visibility:public"],
)

config_setting(
    name = "windows_msvc",
    values = {"cpu": "x64_windows_msvc"},
    visibility = ["//visibility:public"],
)

config_setting(
    name = "windows_msys",
    values = {"cpu": "x64_windows_msys"},
    visibility = ["//visibility:public"],
)

create_msvc_config()

STRICT_C_OPTIONS = select({
    ":msvc": [],
    ":clang-cl": [
        "/W4",
        "-Wconversion",
        "-Wlong-long",
        "-Wmissing-declarations",
        "-Wmissing-prototypes",
        "-Wno-strict-aliasing",
        "-Wshadow",
        "-Wsign-compare",
        "-Wno-sign-conversion",
    ],
    "//conditions:default": [
        "--pedantic-errors",
        "-Wall",
        "-Wconversion",
        "-Werror",
        "-Wextra",
        "-Wlong-long",
        "-Wmissing-declarations",
        "-Wmissing-prototypes",
        "-Wno-strict-aliasing",
        "-Wshadow",
        "-Wsign-compare",
    ],
})

filegroup(
    name = "public_headers",
    srcs = glob(["c/include/brotli/*.h"]),
)

filegroup(
    name = "common_headers",
    srcs = glob(["c/common/*.h"]),
)

filegroup(
    name = "common_sources",
    srcs = glob(["c/common/*.c"]),
)

filegroup(
    name = "dec_headers",
    srcs = glob(["c/dec/*.h"]),
)

filegroup(
    name = "dec_sources",
    srcs = glob(["c/dec/*.c"]),
)

filegroup(
    name = "enc_headers",
    srcs = glob(["c/enc/*.h"]),
)

filegroup(
    name = "enc_sources",
    srcs = glob(["c/enc/*.c"]),
)

cc_library(
    name = "brotli_inc",
    hdrs = [":public_headers"],
    copts = STRICT_C_OPTIONS,
    strip_include_prefix = "c/include",
)

cc_library(
    name = "brotlicommon",
    srcs = [":common_sources"],
    hdrs = [":common_headers"],
    copts = STRICT_C_OPTIONS,
    deps = [":brotli_inc"],
)

cc_library(
    name = "brotlidec",
    srcs = [":dec_sources"],
    hdrs = [":dec_headers"],
    copts = STRICT_C_OPTIONS,
    deps = [":brotlicommon"],
)

cc_library(
    name = "brotlienc",
    srcs = [":enc_sources"],
    hdrs = [":enc_headers"],
    copts = STRICT_C_OPTIONS,
    linkopts = select({
        ":clang-cl": [],
        ":msvc": [],
        "//conditions:default": ["-lm"],
    }),
    deps = [":brotlicommon"],
)

cc_binary(
    name = "brotli",
    srcs = ["c/tools/brotli.c"],
    copts = STRICT_C_OPTIONS,
    linkstatic = 1,
    deps = [
        ":brotlidec",
        ":brotlienc",
    ],
)

filegroup(
    name = "dictionary",
    srcs = ["c/common/dictionary.bin"],
)
