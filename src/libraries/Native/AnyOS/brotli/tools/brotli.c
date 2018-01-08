/* Copyright 2014 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Command line interface for Brotli library. */

#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>

#include "../common/constants.h"
#include "../common/version.h"
#include <brotli/decode.h>
#include <brotli/encode.h>

#if !defined(_WIN32)
#include <unistd.h>
#include <utime.h>
#define MAKE_BINARY(FILENO) (FILENO)
#else
#include <io.h>
#include <share.h>
#include <sys/utime.h>

#define MAKE_BINARY(FILENO) (_setmode((FILENO), _O_BINARY), (FILENO))

#if !defined(__MINGW32__)
#define STDIN_FILENO _fileno(stdin)
#define STDOUT_FILENO _fileno(stdout)
#define S_IRUSR S_IREAD
#define S_IWUSR S_IWRITE
#endif

#define fdopen _fdopen
#define isatty _isatty
#define unlink _unlink
#define utimbuf _utimbuf
#define utime _utime

#define fopen ms_fopen
#define open ms_open

#define chmod(F, P) (0)
#define chown(F, O, G) (0)

#if defined(_MSC_VER) && (_MSC_VER >= 1400)
#define fseek _fseeki64
#define ftell _ftelli64
#endif

static FILE* ms_fopen(const char* filename, const char* mode) {
  FILE* result = 0;
  fopen_s(&result, filename, mode);
  return result;
}

static int ms_open(const char* filename, int oflag, int pmode) {
  int result = -1;
  _sopen_s(&result, filename, oflag | O_BINARY, _SH_DENYNO, pmode);
  return result;
}
#endif  /* WIN32 */

typedef enum {
  COMMAND_COMPRESS,
  COMMAND_DECOMPRESS,
  COMMAND_HELP,
  COMMAND_INVALID,
  COMMAND_TEST_INTEGRITY,
  COMMAND_NOOP,
  COMMAND_VERSION
} Command;

#define DEFAULT_LGWIN 22
#define DEFAULT_SUFFIX ".br"
#define MAX_OPTIONS 20

typedef struct {
  /* Parameters */
  int quality;
  int lgwin;
  BROTLI_BOOL force_overwrite;
  BROTLI_BOOL junk_source;
  BROTLI_BOOL copy_stat;
  BROTLI_BOOL verbose;
  BROTLI_BOOL write_to_stdout;
  BROTLI_BOOL test_integrity;
  BROTLI_BOOL decompress;
  const char* output_path;
  const char* suffix;
  int not_input_indices[MAX_OPTIONS];
  size_t longest_path_len;
  size_t input_count;

  /* Inner state */
  int argc;
  char** argv;
  char* modified_path;  /* Storage for path with appended / cut suffix */
  int iterator;
  int ignore;
  BROTLI_BOOL iterator_error;
  uint8_t* buffer;
  uint8_t* input;
  uint8_t* output;
  const char* current_input_path;
  const char* current_output_path;
  FILE* fin;
  FILE* fout;
} Context;

/* Parse up to 5 decimal digits. */
static BROTLI_BOOL ParseInt(const char* s, int low, int high, int* result) {
  int value = 0;
  int i;
  for (i = 0; i < 5; ++i) {
    char c = s[i];
    if (c == 0) break;
    if (s[i] < '0' || s[i] > '9') return BROTLI_FALSE;
    value = (10 * value) + (c - '0');
  }
  if (i == 0) return BROTLI_FALSE;
  if (i > 1 && s[0] == '0') return BROTLI_FALSE;
  if (s[i] != 0) return BROTLI_FALSE;
  if (value < low || value > high) return BROTLI_FALSE;
  *result = value;
  return BROTLI_TRUE;
}

/* Returns "base file name" or its tail, if it contains '/' or '\'. */
static const char* FileName(const char* path) {
  const char* separator_position = strrchr(path, '/');
  if (separator_position) path = separator_position + 1;
  separator_position = strrchr(path, '\\');
  if (separator_position) path = separator_position + 1;
  return path;
}

/* Detect if the program name is a special alias that infers a command type. */
static Command ParseAlias(const char* name) {
  /* TODO: cast name to lower case? */
  const char* unbrotli = "unbrotli";
  size_t unbrotli_len = strlen(unbrotli);
  name = FileName(name);
  /* Partial comparison. On Windows there could be ".exe" suffix. */
  if (strncmp(name, unbrotli, unbrotli_len) == 0) {
    char terminator = name[unbrotli_len];
    if (terminator == 0 || terminator == '.') return COMMAND_DECOMPRESS;
  }
  return COMMAND_COMPRESS;
}

static Command ParseParams(Context* params) {
  int argc = params->argc;
  char** argv = params->argv;
  int i;
  int next_option_index = 0;
  size_t input_count = 0;
  size_t longest_path_len = 1;
  BROTLI_BOOL command_set = BROTLI_FALSE;
  BROTLI_BOOL quality_set = BROTLI_FALSE;
  BROTLI_BOOL output_set = BROTLI_FALSE;
  BROTLI_BOOL keep_set = BROTLI_FALSE;
  BROTLI_BOOL lgwin_set = BROTLI_FALSE;
  BROTLI_BOOL suffix_set = BROTLI_FALSE;
  BROTLI_BOOL after_dash_dash = BROTLI_FALSE;
  Command command = ParseAlias(argv[0]);

  for (i = 1; i < argc; ++i) {
    const char* arg = argv[i];
    /* C99 5.1.2.2.1: "members argv[0] through argv[argc-1] inclusive shall
       contain pointers to strings"; NULL and 0-length are not forbidden. */
    size_t arg_len = arg ? strlen(arg) : 0;

    if (arg_len == 0) {
      params->not_input_indices[next_option_index++] = i;
      continue;
    }

    /* Too many options. The expected longest option list is:
       "-q 0 -w 10 -o f -D d -S b -d -f -k -n -v --", i.e. 16 items in total.
       This check is an additinal guard that is never triggered, but provides an
       additional guard for future changes. */
    if (next_option_index > (MAX_OPTIONS - 2)) {
      return COMMAND_INVALID;
    }

    /* Input file entry. */
    if (after_dash_dash || arg[0] != '-' || arg_len == 1) {
      input_count++;
      if (longest_path_len < arg_len) longest_path_len = arg_len;
      continue;
    }

    /* Not a file entry. */
    params->not_input_indices[next_option_index++] = i;

    /* '--' entry stop parsing arguments. */
    if (arg_len == 2 && arg[1] == '-') {
      after_dash_dash = BROTLI_TRUE;
      continue;
    }

    /* Simple / coalesced options. */
    if (arg[1] != '-') {
      size_t j;
      for (j = 1; j < arg_len; ++j) {
        char c = arg[j];
        if (c >= '0' && c <= '9') {
          if (quality_set) return COMMAND_INVALID;
          quality_set = BROTLI_TRUE;
          params->quality = c - '0';
          continue;
        } else if (c == 'c') {
          if (output_set) return COMMAND_INVALID;
          output_set = BROTLI_TRUE;
          params->write_to_stdout = BROTLI_TRUE;
          continue;
        } else if (c == 'd') {
          if (command_set) return COMMAND_INVALID;
          command_set = BROTLI_TRUE;
          command = COMMAND_DECOMPRESS;
          continue;
        } else if (c == 'f') {
          if (params->force_overwrite) return COMMAND_INVALID;
          params->force_overwrite = BROTLI_TRUE;
          continue;
        } else if (c == 'h') {
          /* Don't parse further. */
          return COMMAND_HELP;
        } else if (c == 'j' || c == 'k') {
          if (keep_set) return COMMAND_INVALID;
          keep_set = BROTLI_TRUE;
          params->junk_source = TO_BROTLI_BOOL(c == 'j');
          continue;
        } else if (c == 'n') {
          if (!params->copy_stat) return COMMAND_INVALID;
          params->copy_stat = BROTLI_FALSE;
          continue;
        } else if (c == 't') {
          if (command_set) return COMMAND_INVALID;
          command_set = BROTLI_TRUE;
          command = COMMAND_TEST_INTEGRITY;
          continue;
        } else if (c == 'v') {
          if (params->verbose) return COMMAND_INVALID;
          params->verbose = BROTLI_TRUE;
          continue;
        } else if (c == 'V') {
          /* Don't parse further. */
          return COMMAND_VERSION;
        } else if (c == 'Z') {
          if (quality_set) return COMMAND_INVALID;
          quality_set = BROTLI_TRUE;
          params->quality = 11;
          continue;
        }
        /* o/q/w/D/S with parameter is expected */
        if (c != 'o' && c != 'q' && c != 'w' && c != 'D' && c != 'S') {
          return COMMAND_INVALID;
        }
        if (j + 1 != arg_len) return COMMAND_INVALID;
        i++;
        if (i == argc || !argv[i] || argv[i][0] == 0) return COMMAND_INVALID;
        params->not_input_indices[next_option_index++] = i;
        if (c == 'o') {
          if (output_set) return COMMAND_INVALID;
          params->output_path = argv[i];
        } else if (c == 'q') {
          if (quality_set) return COMMAND_INVALID;
          quality_set = ParseInt(argv[i], BROTLI_MIN_QUALITY,
                                 BROTLI_MAX_QUALITY, &params->quality);
          if (!quality_set) return COMMAND_INVALID;
        } else if (c == 'w') {
          if (lgwin_set) return COMMAND_INVALID;
          lgwin_set = ParseInt(argv[i], 0,
                               BROTLI_MAX_WINDOW_BITS, &params->lgwin);
          if (!lgwin_set) return COMMAND_INVALID;
          if (params->lgwin != 0 && params->lgwin < BROTLI_MIN_WINDOW_BITS) {
            return COMMAND_INVALID;
          }
        } else if (c == 'S') {
          if (suffix_set) return COMMAND_INVALID;
          suffix_set = BROTLI_TRUE;
          params->suffix = argv[i];
        }
      }
    } else {  /* Double-dash. */
      arg = &arg[2];
      if (strcmp("best", arg) == 0) {
        if (quality_set) return COMMAND_INVALID;
        quality_set = BROTLI_TRUE;
        params->quality = 11;
      } else if (strcmp("decompress", arg) == 0) {
        if (command_set) return COMMAND_INVALID;
        command_set = BROTLI_TRUE;
        command = COMMAND_DECOMPRESS;
      } else if (strcmp("force", arg) == 0) {
        if (params->force_overwrite) return COMMAND_INVALID;
        params->force_overwrite = BROTLI_TRUE;
      } else if (strcmp("help", arg) == 0) {
        /* Don't parse further. */
        return COMMAND_HELP;
      } else if (strcmp("keep", arg) == 0) {
        if (keep_set) return COMMAND_INVALID;
        keep_set = BROTLI_TRUE;
        params->junk_source = BROTLI_FALSE;
      } else if (strcmp("no-copy-stat", arg) == 0) {
        if (!params->copy_stat) return COMMAND_INVALID;
        params->copy_stat = BROTLI_FALSE;
      } else if (strcmp("rm", arg) == 0) {
        if (keep_set) return COMMAND_INVALID;
        keep_set = BROTLI_TRUE;
        params->junk_source = BROTLI_TRUE;
      } else if (strcmp("stdout", arg) == 0) {
        if (output_set) return COMMAND_INVALID;
        output_set = BROTLI_TRUE;
        params->write_to_stdout = BROTLI_TRUE;
      } else if (strcmp("test", arg) == 0) {
        if (command_set) return COMMAND_INVALID;
        command_set = BROTLI_TRUE;
        command = COMMAND_TEST_INTEGRITY;
      } else if (strcmp("verbose", arg) == 0) {
        if (params->verbose) return COMMAND_INVALID;
        params->verbose = BROTLI_TRUE;
      } else if (strcmp("version", arg) == 0) {
        /* Don't parse further. */
        return COMMAND_VERSION;
      } else {
        /* key=value */
        const char* value = strrchr(arg, '=');
        size_t key_len;
        if (!value || value[1] == 0) return COMMAND_INVALID;
        key_len = (size_t)(value - arg);
        value++;
        if (strncmp("lgwin", arg, key_len) == 0) {
          if (lgwin_set) return COMMAND_INVALID;
          lgwin_set = ParseInt(value, 0,
                               BROTLI_MAX_WINDOW_BITS, &params->lgwin);
          if (!lgwin_set) return COMMAND_INVALID;
          if (params->lgwin != 0 && params->lgwin < BROTLI_MIN_WINDOW_BITS) {
            return COMMAND_INVALID;
          }
        } else if (strncmp("output", arg, key_len) == 0) {
          if (output_set) return COMMAND_INVALID;
          params->output_path = value;
        } else if (strncmp("quality", arg, key_len) == 0) {
          if (quality_set) return COMMAND_INVALID;
          quality_set = ParseInt(value, BROTLI_MIN_QUALITY,
                                 BROTLI_MAX_QUALITY, &params->quality);
          if (!quality_set) return COMMAND_INVALID;
        } else if (strncmp("suffix", arg, key_len) == 0) {
          if (suffix_set) return COMMAND_INVALID;
          suffix_set = BROTLI_TRUE;
          params->suffix = value;
        } else {
          return COMMAND_INVALID;
        }
      }
    }
  }

  params->input_count = input_count;
  params->longest_path_len = longest_path_len;
  params->decompress = (command == COMMAND_DECOMPRESS);
  params->test_integrity = (command == COMMAND_TEST_INTEGRITY);

  if (input_count > 1 && output_set) return COMMAND_INVALID;
  if (params->test_integrity) {
    if (params->output_path) return COMMAND_INVALID;
    if (params->write_to_stdout) return COMMAND_INVALID;
  }
  if (strchr(params->suffix, '/') || strchr(params->suffix, '\\')) {
    return COMMAND_INVALID;
  }

  return command;
}

static void PrintVersion(void) {
  int major = BROTLI_VERSION >> 24;
  int minor = (BROTLI_VERSION >> 12) & 0xFFF;
  int patch = BROTLI_VERSION & 0xFFF;
  fprintf(stdout, "brotli %d.%d.%d\n", major, minor, patch);
}

static void PrintHelp(const char* name) {
  /* String is cut to pieces with length less than 509, to conform C90 spec. */
  fprintf(stdout,
"Usage: %s [OPTION]... [FILE]...\n",
          name);
  fprintf(stdout,
"Options:\n"
"  -#                          compression level (0-9)\n"
"  -c, --stdout                write on standard output\n"
"  -d, --decompress            decompress\n"
"  -f, --force                 force output file overwrite\n"
"  -h, --help                  display this help and exit\n");
  fprintf(stdout,
"  -j, --rm                    remove source file(s)\n"
"  -k, --keep                  keep source file(s) (default)\n"
"  -n, --no-copy-stat          do not copy source file(s) attributes\n"
"  -o FILE, --output=FILE      output file (only if 1 input file)\n");
  fprintf(stdout,
"  -q NUM, --quality=NUM       compression level (%d-%d)\n",
          BROTLI_MIN_QUALITY, BROTLI_MAX_QUALITY);
  fprintf(stdout,
"  -t, --test                  test compressed file integrity\n"
"  -v, --verbose               verbose mode\n");
  fprintf(stdout,
"  -w NUM, --lgwin=NUM         set LZ77 window size (0, %d-%d) (default:%d)\n",
          BROTLI_MIN_WINDOW_BITS, BROTLI_MAX_WINDOW_BITS, DEFAULT_LGWIN);
  fprintf(stdout,
"                              window size = 2**NUM - 16\n"
"                              0 lets compressor choose the optimal value\n");
  fprintf(stdout,
"  -S SUF, --suffix=SUF        output file suffix (default:'%s')\n",
          DEFAULT_SUFFIX);
  fprintf(stdout,
"  -V, --version               display version and exit\n"
"  -Z, --best                  use best compression level (11) (default)\n"
"Simple options could be coalesced, i.e. '-9kf' is equivalent to '-9 -k -f'.\n"
"With no FILE, or when FILE is -, read standard input.\n"
"All arguments after '--' are treated as files.\n");
}

static const char* PrintablePath(const char* path) {
  return path ? path : "con";
}

static BROTLI_BOOL OpenInputFile(const char* input_path, FILE** f) {
  *f = NULL;
  if (!input_path) {
    *f = fdopen(MAKE_BINARY(STDIN_FILENO), "rb");
    return BROTLI_TRUE;
  }
  *f = fopen(input_path, "rb");
  if (!*f) {
    fprintf(stderr, "failed to open input file [%s]: %s\n",
            PrintablePath(input_path), strerror(errno));
    return BROTLI_FALSE;
  }
  return BROTLI_TRUE;
}

static BROTLI_BOOL OpenOutputFile(const char* output_path, FILE** f,
                                  BROTLI_BOOL force) {
  int fd;
  *f = NULL;
  if (!output_path) {
    *f = fdopen(MAKE_BINARY(STDOUT_FILENO), "wb");
    return BROTLI_TRUE;
  }
  fd = open(output_path, O_CREAT | (force ? 0 : O_EXCL) | O_WRONLY | O_TRUNC,
            S_IRUSR | S_IWUSR);
  if (fd < 0) {
    fprintf(stderr, "failed to open output file [%s]: %s\n",
            PrintablePath(output_path), strerror(errno));
    return BROTLI_FALSE;
  }
  *f = fdopen(fd, "wb");
  if (!*f) {
    fprintf(stderr, "failed to open output file [%s]: %s\n",
            PrintablePath(output_path), strerror(errno));
    return BROTLI_FALSE;
  }
  return BROTLI_TRUE;
}

/* Copy file times and permissions.
   TODO: this is a "best effort" implementation; honest cross-platform
   fully featured implementation is way too hacky; add more hacks by request. */
static void CopyStat(const char* input_path, const char* output_path) {
  struct stat statbuf;
  struct utimbuf times;
  int res;
  if (input_path == 0 || output_path == 0) {
    return;
  }
  if (stat(input_path, &statbuf) != 0) {
    return;
  }
  times.actime = statbuf.st_atime;
  times.modtime = statbuf.st_mtime;
  utime(output_path, &times);
  res = chmod(output_path, statbuf.st_mode & (S_IRWXU | S_IRWXG | S_IRWXO));
  if (res != 0) {
    fprintf(stderr, "setting access bits failed for [%s]: %s\n",
            PrintablePath(output_path), strerror(errno));
  }
  res = chown(output_path, (uid_t)-1, statbuf.st_gid);
  if (res != 0) {
    fprintf(stderr, "setting group failed for [%s]: %s\n",
            PrintablePath(output_path), strerror(errno));
  }
  res = chown(output_path, statbuf.st_uid, (gid_t)-1);
  if (res != 0) {
    fprintf(stderr, "setting user failed for [%s]: %s\n",
            PrintablePath(output_path), strerror(errno));
  }
}

static BROTLI_BOOL NextFile(Context* context) {
  const char* arg;
  size_t arg_len;

  /* Iterator points to last used arg; increment to search for the next one. */
  context->iterator++;

  /* No input path; read from console. */
  if (context->input_count == 0) {
    if (context->iterator > 1) return BROTLI_FALSE;
    context->current_input_path = NULL;
    /* Either write to the specified path, or to console. */
    context->current_output_path = context->output_path;
    return BROTLI_TRUE;
  }

  /* Skip option arguments. */
  while (context->iterator == context->not_input_indices[context->ignore]) {
    context->iterator++;
    context->ignore++;
  }

  /* All args are scanned already. */
  if (context->iterator >= context->argc) return BROTLI_FALSE;

  /* Iterator now points to the input file name. */
  arg = context->argv[context->iterator];
  arg_len = strlen(arg);
  /* Read from console. */
  if (arg_len == 1 && arg[0] == '-') {
    context->current_input_path = NULL;
    context->current_output_path = context->output_path;
    return BROTLI_TRUE;
  }

  context->current_input_path = arg;
  context->current_output_path = context->output_path;

  if (context->output_path) return BROTLI_TRUE;
  if (context->write_to_stdout) return BROTLI_TRUE;

  strcpy(context->modified_path, arg);
  context->current_output_path = context->modified_path;
  /* If output is not specified, input path suffix should match. */
  if (context->decompress) {
    size_t suffix_len = strlen(context->suffix);
    char* name = (char*)FileName(context->modified_path);
    char* name_suffix;
    size_t name_len = strlen(name);
    if (name_len < suffix_len + 1) {
      fprintf(stderr, "empty output file name for [%s] input file\n",
              PrintablePath(arg));
      context->iterator_error = BROTLI_TRUE;
      return BROTLI_FALSE;
    }
    name_suffix = name + name_len - suffix_len;
    if (strcmp(context->suffix, name_suffix) != 0) {
      fprintf(stderr, "input file [%s] suffix mismatch\n",
              PrintablePath(arg));
      context->iterator_error = BROTLI_TRUE;
      return BROTLI_FALSE;
    }
    name_suffix[0] = 0;
    return BROTLI_TRUE;
  } else {
    strcpy(context->modified_path + arg_len, context->suffix);
    return BROTLI_TRUE;
  }
}

static BROTLI_BOOL OpenFiles(Context* context) {
  BROTLI_BOOL is_ok = OpenInputFile(context->current_input_path, &context->fin);
  if (!context->test_integrity && is_ok) {
    is_ok = OpenOutputFile(
        context->current_output_path, &context->fout, context->force_overwrite);
  }
  return is_ok;
}

static BROTLI_BOOL CloseFiles(Context* context, BROTLI_BOOL success) {
  BROTLI_BOOL is_ok = BROTLI_TRUE;
  if (!context->test_integrity && context->fout) {
    if (!success && context->current_output_path) {
      unlink(context->current_output_path);
    }
    if (fclose(context->fout) != 0) {
      if (success) {
        fprintf(stderr, "fclose failed [%s]: %s\n",
                PrintablePath(context->current_output_path), strerror(errno));
      }
      is_ok = BROTLI_FALSE;
    }

    /* TOCTOU violation, but otherwise it is impossible to set file times. */
    if (success && is_ok && context->copy_stat) {
      CopyStat(context->current_input_path, context->current_output_path);
    }
  }

  if (context->fin) {
    if (fclose(context->fin) != 0) {
      if (is_ok) {
        fprintf(stderr, "fclose failed [%s]: %s\n",
                PrintablePath(context->current_input_path), strerror(errno));
      }
      is_ok = BROTLI_FALSE;
    }
  }
  if (success && context->junk_source && context->current_input_path) {
    unlink(context->current_input_path);
  }

  context->fin = NULL;
  context->fout = NULL;

  return is_ok;
}

static const size_t kFileBufferSize = 1 << 16;

static BROTLI_BOOL DecompressFile(Context* context, BrotliDecoderState* s) {
  size_t available_in = 0;
  const uint8_t* next_in = NULL;
  size_t available_out = kFileBufferSize;
  uint8_t* next_out = context->output;
  BrotliDecoderResult result = BROTLI_DECODER_RESULT_NEEDS_MORE_INPUT;
  for (;;) {
    if (next_out != context->output) {
      if (!context->test_integrity) {
        size_t out_size = (size_t)(next_out - context->output);
        fwrite(context->output, 1, out_size, context->fout);
        if (ferror(context->fout)) {
          fprintf(stderr, "failed to write output [%s]: %s\n",
                  PrintablePath(context->current_output_path), strerror(errno));
          return BROTLI_FALSE;
        }
      }
      available_out = kFileBufferSize;
      next_out = context->output;
    }

    if (result == BROTLI_DECODER_RESULT_NEEDS_MORE_INPUT) {
      if (feof(context->fin)) {
        fprintf(stderr, "corrupt input [%s]\n",
                PrintablePath(context->current_input_path));
        return BROTLI_FALSE;
      }
      available_in = fread(context->input, 1, kFileBufferSize, context->fin);
      next_in = context->input;
      if (ferror(context->fin)) {
        fprintf(stderr, "failed to read input [%s]: %s\n",
                PrintablePath(context->current_input_path), strerror(errno));
      return BROTLI_FALSE;
      }
    } else if (result == BROTLI_DECODER_RESULT_NEEDS_MORE_OUTPUT) {
      /* Nothing to do - output is already written. */
    } else if (result == BROTLI_DECODER_RESULT_SUCCESS) {
      if (available_in != 0 || !feof(context->fin)) {
        fprintf(stderr, "corrupt input [%s]\n",
                PrintablePath(context->current_input_path));
        return BROTLI_FALSE;
      }
      return BROTLI_TRUE;
    } else {
      fprintf(stderr, "corrupt input [%s]\n",
              PrintablePath(context->current_input_path));
      return BROTLI_FALSE;
    }

    result = BrotliDecoderDecompressStream(
        s, &available_in, &next_in, &available_out, &next_out, 0);
  }
}

static BROTLI_BOOL DecompressFiles(Context* context) {
  while (NextFile(context)) {
    BROTLI_BOOL is_ok = BROTLI_TRUE;
    BrotliDecoderState* s = BrotliDecoderCreateInstance(NULL, NULL, NULL);
    if (!s) {
      fprintf(stderr, "out of memory\n");
      return BROTLI_FALSE;
    }
    is_ok = OpenFiles(context);
    if (is_ok && !context->current_input_path &&
        !context->force_overwrite && isatty(STDIN_FILENO)) {
      fprintf(stderr, "Use -h help. Use -f to force input from a terminal.\n");
      is_ok = BROTLI_FALSE;
    }
    if (is_ok) is_ok = DecompressFile(context, s);
    BrotliDecoderDestroyInstance(s);
    if (!CloseFiles(context, is_ok)) is_ok = BROTLI_FALSE;
    if (!is_ok) return BROTLI_FALSE;
  }
  return BROTLI_TRUE;
}

static BROTLI_BOOL CompressFile(Context* context, BrotliEncoderState* s) {
  size_t available_in = 0;
  const uint8_t* next_in = NULL;
  size_t available_out = kFileBufferSize;
  uint8_t* next_out = context->output;
  BROTLI_BOOL is_eof = BROTLI_FALSE;

  for (;;) {
    if (available_in == 0 && !is_eof) {
      available_in = fread(context->input, 1, kFileBufferSize, context->fin);
      next_in = context->input;
      if (ferror(context->fin)) {
        fprintf(stderr, "failed to read input [%s]: %s\n",
                PrintablePath(context->current_input_path), strerror(errno));
        return BROTLI_FALSE;
      }
      is_eof = feof(context->fin) ? BROTLI_TRUE : BROTLI_FALSE;
    }

    if (!BrotliEncoderCompressStream(s,
        is_eof ? BROTLI_OPERATION_FINISH : BROTLI_OPERATION_PROCESS,
        &available_in, &next_in, &available_out, &next_out, NULL)) {
      /* Should detect OOM? */
      fprintf(stderr, "failed to compress data [%s]\n",
              PrintablePath(context->current_input_path));
      return BROTLI_FALSE;
    }

    if (available_out != kFileBufferSize) {
      size_t out_size = kFileBufferSize - available_out;
      fwrite(context->output, 1, out_size, context->fout);
      if (ferror(context->fout)) {
        fprintf(stderr, "failed to write output [%s]: %s\n",
                PrintablePath(context->current_output_path), strerror(errno));
        return BROTLI_FALSE;
      }
      available_out = kFileBufferSize;
      next_out = context->output;
    }

    if (BrotliEncoderIsFinished(s)) return BROTLI_TRUE;
  }
}

static BROTLI_BOOL CompressFiles(Context* context) {
  while (NextFile(context)) {
    BROTLI_BOOL is_ok = BROTLI_TRUE;
    BrotliEncoderState* s = BrotliEncoderCreateInstance(NULL, NULL, NULL);
    if (!s) {
      fprintf(stderr, "out of memory\n");
      return BROTLI_FALSE;
    }
    BrotliEncoderSetParameter(s,
        BROTLI_PARAM_QUALITY, (uint32_t)context->quality);
    BrotliEncoderSetParameter(s,
        BROTLI_PARAM_LGWIN, (uint32_t)context->lgwin);
    is_ok = OpenFiles(context);
    if (is_ok && !context->current_output_path &&
        !context->force_overwrite && isatty(STDOUT_FILENO)) {
      fprintf(stderr, "Use -h help. Use -f to force output to a terminal.\n");
      is_ok = BROTLI_FALSE;
    }
    if (is_ok) is_ok = CompressFile(context, s);
    BrotliEncoderDestroyInstance(s);
    if (!CloseFiles(context, is_ok)) is_ok = BROTLI_FALSE;
    if (!is_ok) return BROTLI_FALSE;
  }
  return BROTLI_TRUE;
}

int main(int argc, char** argv) {
  Command command;
  Context context;
  BROTLI_BOOL is_ok = BROTLI_TRUE;
  int i;

  context.quality = 11;
  context.lgwin = DEFAULT_LGWIN;
  context.force_overwrite = BROTLI_FALSE;
  context.junk_source = BROTLI_FALSE;
  context.copy_stat = BROTLI_TRUE;
  context.test_integrity = BROTLI_FALSE;
  context.verbose = BROTLI_FALSE;
  context.write_to_stdout = BROTLI_FALSE;
  context.decompress = BROTLI_FALSE;
  context.output_path = NULL;
  context.suffix = DEFAULT_SUFFIX;
  for (i = 0; i < MAX_OPTIONS; ++i) context.not_input_indices[i] = 0;
  context.longest_path_len = 1;
  context.input_count = 0;

  context.argc = argc;
  context.argv = argv;
  context.modified_path = NULL;
  context.iterator = 0;
  context.ignore = 0;
  context.iterator_error = BROTLI_FALSE;
  context.buffer = NULL;
  context.current_input_path = NULL;
  context.current_output_path = NULL;
  context.fin = NULL;
  context.fout = NULL;

  command = ParseParams(&context);

  if (command == COMMAND_COMPRESS || command == COMMAND_DECOMPRESS ||
      command == COMMAND_TEST_INTEGRITY) {
    if (is_ok) {
      size_t modified_path_len =
          context.longest_path_len + strlen(context.suffix) + 1;
      context.modified_path = (char*)malloc(modified_path_len);
      context.buffer = (uint8_t*)malloc(kFileBufferSize * 2);
      if (!context.modified_path || !context.buffer) {
        fprintf(stderr, "out of memory\n");
        is_ok = BROTLI_FALSE;
      } else {
        context.input = context.buffer;
        context.output = context.buffer + kFileBufferSize;
      }
    }
  }

  if (!is_ok) command = COMMAND_NOOP;

  switch (command) {
    case COMMAND_NOOP:
      break;

    case COMMAND_VERSION:
      PrintVersion();
      break;

    case COMMAND_COMPRESS:
      is_ok = CompressFiles(&context);
      break;

    case COMMAND_DECOMPRESS:
    case COMMAND_TEST_INTEGRITY:
      is_ok = DecompressFiles(&context);
      break;

    case COMMAND_HELP:
    case COMMAND_INVALID:
    default:
      PrintHelp(FileName(argv[0]));
      is_ok = (command == COMMAND_HELP);
      break;
  }

  if (context.iterator_error) is_ok = BROTLI_FALSE;

  free(context.modified_path);
  free(context.buffer);

  if (!is_ok) exit(1);
  return 0;
}
