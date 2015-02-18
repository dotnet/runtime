#!/bin/sh

TOTAL=$(grep -c "<test-case" TestResults_op_il_seq_point.xml)
FAILURES=$(grep -c "<failure>" TestResults_op_il_seq_point.xml)
if [ "$FAILURES" -eq "0" ]
then
	PASS="True"
else
	PASS="False"
fi
MYLOCALE=$(echo $LANG | cut -f1 -d'.')
MYUNAME=$(uname -r)
MYHOSTNAME=$(hostname)
MYFQDN=$(hostname --fqdn)
MYDATE=$(date +%F)
MYTIME=$(date +%T)

echo "            </results>" >> TestResults_op_il_seq_point.xml
echo "          </test-suite>" >> TestResults_op_il_seq_point.xml
echo "        </results>" >> TestResults_op_il_seq_point.xml
echo "      </test-suite>" >> TestResults_op_il_seq_point.xml
echo "    </results>" >> TestResults_op_il_seq_point.xml
echo "  </test-suite>" >> TestResults_op_il_seq_point.xml
echo "</test-results>" >> TestResults_op_il_seq_point.xml

sed -i "1i\ \ \ \ \ \ \ \ \ \ \ \ <results>" TestResults_op_il_seq_point.xml
sed -i "1i\ \ \ \ \ \ \ \ \ \ <test-suite name=\"op_il_seq_point\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" TestResults_op_il_seq_point.xml
sed -i "1i\ \ \ \ \ \ \ \ <results>" TestResults_op_il_seq_point.xml
sed -i "1i\ \ \ \ \ \ <test-suite name=\"MonoTests\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" TestResults_op_il_seq_point.xml
sed -i "1i\ \ \ \ <results>" TestResults_op_il_seq_point.xml
sed -i "1i\ \ <test-suite name=\"op_il_seq_point-tests.dummy\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" TestResults_op_il_seq_point.xml
sed -i "1i\ \ <culture-info current-culture=\"${MYLOCALE}\" current-uiculture=\"${MYLOCALE}\" />" TestResults_op_il_seq_point.xml
sed -i "1i\ \ <environment nunit-version=\"2.4.8.0\" clr-version=\"4.0.30319.17020\" os-version=\"Unix ${MYUNAME}\" platform=\"Unix\" cwd=\"${PWD}\" machine-name=\"${MYHOSTNAME}\" user=\"${USER}\" user-domain=\"${MYFQDN}\" />" TestResults_op_il_seq_point.xml
sed -i "1i<test-results name=\"regression-tests.dummy\" total=\"${TOTAL}\" failures=\"${FAILURES}\" not-run=\"0\" date=\"${MYDATE}\" time=\"${MYTIME}\">" TestResults_op_il_seq_point.xml
sed -i "1i<!--This file represents the results of running a test suite-->" TestResults_op_il_seq_point.xml
sed -i "1i<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>" TestResults_op_il_seq_point.xml
