/*
 * Copyright (c) 2000-2013 Apple Inc. All rights reserved.
 *
 * @APPLE_OSREFERENCE_LICENSE_HEADER_START@
 *
 * This file contains Original Code and/or Modifications of Original Code
 * as defined in and that are subject to the Apple Public Source License
 * Version 2.0 (the 'License'). You may not use this file except in
 * compliance with the License. The rights granted to you under the License
 * may not be used to create, or enable the creation or redistribution of,
 * unlawful or unlicensed copies of an Apple operating system, or to
 * circumvent, violate, or enable the circumvention or violation of, any
 * terms of an Apple operating system software license agreement.
 *
 * Please obtain a copy of the License at
 * http://www.opensource.apple.com/apsl/ and read it before using this file.
 *
 * The Original Code and all software distributed under the License are
 * distributed on an 'AS IS' basis, WITHOUT WARRANTY OF ANY KIND, EITHER
 * EXPRESS OR IMPLIED, AND APPLE HEREBY DISCLAIMS ALL SUCH WARRANTIES,
 * INCLUDING WITHOUT LIMITATION, ANY WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE, QUIET ENJOYMENT OR NON-INFRINGEMENT.
 * Please see the License for the specific language governing rights and
 * limitations under the License.
 *
 * @APPLE_OSREFERENCE_LICENSE_HEADER_END@
 */
/*
 * Copyright (c) 1982, 1986, 1993
 *	The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *	This product includes software developed by the University of
 *	California, Berkeley and its contributors.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 *	@(#)icmp_var.h	8.1 (Berkeley) 6/10/93
 * $FreeBSD: src/sys/netinet/icmp_var.h,v 1.15.2.1 2001/02/24 21:35:18 bmilekic Exp $
 */

#ifndef _NETINET_ICMP_VAR_H_
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wreserved-id-macro"
#define _NETINET_ICMP_VAR_H_
#pragma clang diagnostic pop
#include <sys/appleapiopts.h>

/*
 * Variables related to this implementation
 * of the internet control message protocol.
 */
struct  icmpstat {
/* statistics related to icmp packets generated */
	u_int32_t       icps_error;     /* # of calls to icmp_error */
	u_int32_t       icps_oldshort;  /* no error 'cuz old ip too short */
	u_int32_t       icps_oldicmp;   /* no error 'cuz old was icmp */
	u_int32_t       icps_outhist[ICMP_MAXTYPE + 1];
/* statistics related to input messages processed */
	u_int32_t       icps_badcode;   /* icmp_code out of range */
	u_int32_t       icps_tooshort;  /* packet < ICMP_MINLEN */
	u_int32_t       icps_checksum;  /* bad checksum */
	u_int32_t       icps_badlen;    /* calculated bound mismatch */
	u_int32_t       icps_reflect;   /* number of responses */
	u_int32_t       icps_inhist[ICMP_MAXTYPE + 1];
	u_int32_t       icps_bmcastecho;/* b/mcast echo requests dropped */
	u_int32_t       icps_bmcasttstamp; /* b/mcast tstamp requests dropped */
};

/*
 * Names for ICMP sysctl objects
 */
#define ICMPCTL_MASKREPL        1       /* allow replies to netmask requests */
#define ICMPCTL_STATS           2       /* statistics (read-only) */
#define ICMPCTL_ICMPLIM         3
#define ICMPCTL_TIMESTAMP       4       /* allow replies to time stamp requests */
#define ICMPCTL_MAXID           5

#ifdef BSD_KERNEL_PRIVATE
#define ICMPCTL_NAMES { \
	{ 0, 0 }, \
	{ "maskrepl", CTLTYPE_INT }, \
	{ "stats", CTLTYPE_STRUCT }, \
	{ "icmplim", CTLTYPE_INT }, \
	{ "icmptimestamp", CTLTYPE_INT }, \
}

SYSCTL_DECL(_net_inet_icmp);
#ifdef ICMP_BANDLIM
extern boolean_t badport_bandlim(int which);
#endif
#define BANDLIM_ICMP_UNREACH 0
#define BANDLIM_ICMP_ECHO 1
#define BANDLIM_ICMP_TSTAMP 2
#define BANDLIM_MAX 4

extern struct   icmpstat icmpstat;
#endif /* BSD_KERNEL_PRIVATE */
#endif /* _NETINET_ICMP_VAR_H_ */