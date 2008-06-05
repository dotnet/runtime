#!/bin/sh
# 
# dtrace-prelink.sh: DTrace helper script for Mono
# 
# Authors:
#   Andreas Faerber <andreas.faerber@web.de>
# 

# Assume that PIC object files live in .libs/, non-PIC code in ./
PIC=no
if test "$1" = "--pic"; then
	PIC=yes
	shift
fi

OBJ="$1"
PROV="$2"

shift
shift

FILES="$*"

OBJS=
TMPDIR=.dtrace
mkdir -p "${TMPDIR}"

# Extract relevant object files to temporary directories
for FILE in ${FILES}; do
	if echo "${FILE}" | grep .la > /dev/null; then
		LIBDIR=`dirname ${FILE}`
		LIB=".libs/`basename ${FILE} .la`.a"
		DIR="${TMPDIR}/`basename ${FILE}`"
		mkdir -p ${DIR}
		(cd "${DIR}" && ${AR} x "../../${LIBDIR}/${LIB}")
		TMPOBJS=`ls -1 "${DIR}"`
		for TMPOBJ in ${TMPOBJS}; do
			LO=`basename "${TMPOBJ}" .o`.lo
			SRCOBJ="${TMPOBJ}"
			if test x${PIC} = xyes; then
				SRCOBJ=".libs/${SRCOBJ}"
			fi
			# Overwrite with original version
			cp "${LIBDIR}/${SRCOBJ}" "${DIR}/${TMPOBJ}" || cp "${LIBDIR}/${TMPOBJ}" "${DIR}/${TMPOBJ}" || exit
			# Add to list
			OBJS="${OBJS} ${DIR}/${TMPOBJ}"
		done
	fi
	if echo "${FILE}" | grep .lo > /dev/null; then
		DIR=`dirname ${FILE}`
		SRCOBJ=`basename ${FILE} .lo`.o
		if test x${PIC} = xyes; then
			SRCOBJ=".libs/${SRCOBJ}"
		fi
		OBJS="${OBJS} ${DIR}/${SRCOBJ}"
	fi
done

# Run dtrace -G over the temporary objects
${DTRACE} -G ${DTRACEFLAGS} -s "${PROV}" -o "${OBJ}" ${OBJS} || exit

# Update the archives with the temporary, modified object files so that they are linked in
for FILE in ${FILES}; do
	if echo "${FILE}" | grep .la > /dev/null; then
		LIBDIR=`dirname ${FILE}`
		LIB=".libs/`basename ${FILE} .la`.a"
		DIR="${TMPDIR}/`basename ${FILE}`"
		(cd "${DIR}" && ${AR} r "../../${LIBDIR}/${LIB}" *.o)
	fi
	# .lo files were modified in-place
done

rm -rf "${TMPDIR}"

