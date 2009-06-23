#!/usr/bin/env bash

LOW=2000
HIGH=2000

function create_iface () {
	COUNT=$1
	echo "public interface Iface_$COUNT {"
	for i in `seq 1 $COUNT`;
	do
		echo "	int Method_$i (int a, int b, int c, int d);"
	done 
	echo "}"
	echo
}


function create_impl () {
	COUNT=$1
	echo "public class Impl_$COUNT : Iface_$COUNT {"
	for i in `seq 1 $COUNT`;
	do
		echo "	public virtual int Method_$i (int a, int b, int c, int d) { return a - b - c - d + ${i}; }"
	done 
	echo "}"
	echo
}


function create_static_part () {
	IFACE=$1
	echo "	static Iface_$IFACE var_$IFACE = new Impl_$IFACE ();"
	echo "	static int Test_$IFACE () {
		int res = 0;
		int r;
	"

	for i in `seq 1 $IFACE`;
	do	
		echo "		if ((r = var_${IFACE}.Method_$i (10,5,3,2)) != ${i}) {
			Console.WriteLine(\"iface $IFACE method $i returned {0}\", r);
			res = 1;
		}"

	done

	echo "		return res;
	}"
}


function test_iface () {
	IFACE=$1
	echo "		res |= Test_$IFACE ();"
}


####Part that split the output

echo "using System;

"

for i in `seq $LOW $HIGH`;
do
	create_iface $i
	create_impl $i
done



echo "
public class Driver
{
"


for i in `seq $LOW $HIGH`;
do
	create_static_part $i
done


echo "
	public static int Main ()
	{
		int res = 0;"


for i in `seq $LOW $HIGH`;
do
	test_iface  $i
done


echo "		return res;
	}
}"



