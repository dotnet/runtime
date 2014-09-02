/*
 * sgen-tagged-pointer.h: Macros for tagging and untagging pointers.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#ifndef __MONO_SGEN_TAGGED_POINTER_H__
#define __MONO_SGEN_TAGGED_POINTER_H__

#define SGEN_POINTER_IS_TAGGED_1(p)	((mword)(p) & 1)
#define SGEN_POINTER_TAG_1(p)		((void*)((mword)(p) | 1))
#define SGEN_POINTER_UNTAG_1(p)		((void*)((mword)(p) & ~1))

#define SGEN_POINTER_IS_TAGGED_2(p)	((mword)(p) & 2)
#define SGEN_POINTER_TAG_2(p)		((void*)((mword)(p) | 2))
#define SGEN_POINTER_UNTAG_2(p)		((void*)((mword)(p) & ~2))

#define SGEN_POINTER_IS_TAGGED_1_OR_2(p)	((mword)(p) & 3)
#define SGEN_POINTER_UNTAG_12(p)	((void*)((mword)(p) & ~3))

#endif
