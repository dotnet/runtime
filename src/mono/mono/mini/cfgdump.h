/**
 * \file
 * Copyright (C) 2016 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MINI_CFGDUMP_H__
#define __MINI_CFGDUMP_H__
#include <glib.h>

#define CONSTANT_POOL_MAX_SIZE 8000

#define BEGIN_GROUP 0x00
#define BEGIN_GRAPH 0x01
#define CLOSE_GROUP 0x02

#define POOL_NEW 0x00
#define POOL_STRING 0x01
#define POOL_ENUM 0x02
#define POOL_KLASS 0x03
#define POOL_METHOD 0x04
#define POOL_NULL 0x05
#define POOL_NODE_CLASS 0x06
#define POOL_FIELD 0x07
#define POOL_SIGNATURE 0x08

#define PROPERTY_POOL 0x00
#define PROPERTY_INT 0x01
#define PROPERTY_LONG 0x02
#define PROPERTY_DOUBLE 0x03
#define PROPERTY_FLOAT 0x04
#define PROPERTY_TRUE 0x05
#define PROPERTY_FALSE 0x06
#define PROPERTY_ARRAY 0x07
#define PROPERTY_SUBGRAPH 0x08

#define KLASS 0x00
#define ENUM_KLASS 0x01


#define DEFAULT_PORT 4445
#define DEFAULT_HOST "127.0.0.1"

typedef enum {
	PT_STRING,
	PT_METHOD,
	PT_KLASS,
	PT_OPTYPE,
	PT_INPUTTYPE,
	PT_ENUMKLASS,
	PT_SIGNATURE
} pool_type;

struct _MonoGraphDumper {
	int fd;
	GHashTable *constant_pool;
	short next_cp_id;
	GHashTable *insn2id;
	int next_insn_id;
};

typedef struct _MonoGraphDumper MonoGraphDumper;

struct _ConstantPoolEntry {
	pool_type pt;
	void *data;
};

typedef struct _ConstantPoolEntry ConstantPoolEntry;
#endif /* __MINI_CFGDUMP_H__ */
