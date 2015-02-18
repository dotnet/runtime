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
MYHOSTNAME=$(hostname -s)
MYFQDN=$(hostname -f)
MYDATE=$(date +%F)
MYTIME=$(date +%T)

echo "            </results>" >> TestResults_op_il_seq_point.xml
echo "          </test-suite>" >> TestResults_op_il_seq_point.xml
echo "        </results>" >> TestResults_op_il_seq_point.xml
echo "      </test-suite>" >> TestResults_op_il_seq_point.xml
echo "    </results>" >> TestResults_op_il_seq_point.xml
echo "  </test-suite>" >> TestResults_op_il_seq_point.xml
echo "</test-results>" >> TestResults_op_il_seq_point.xml

echo "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>" > TestResults_op_il_seq_point.xml.header
echo "<!--This file represents the results of running a test suite-->" >> TestResults_op_il_seq_point.xml.header
echo "<test-results name=\"regression-tests.dummy\" total=\"${TOTAL}\" failures=\"${FAILURES}\" not-run=\"0\" date=\"${MYDATE}\" time=\"${MYTIME}\">" >> TestResults_op_il_seq_point.xml.header
echo "  <environment nunit-version=\"2.4.8.0\" clr-version=\"4.0.30319.17020\" os-version=\"Unix ${MYUNAME}\" platform=\"Unix\" cwd=\"${PWD}\" machine-name=\"${MYHOSTNAME}\" user=\"${USER}\" user-domain=\"${MYFQDN}\" />" >> TestResults_op_il_seq_point.xml.header
echo "  <culture-info current-culture=\"${MYLOCALE}\" current-uiculture=\"${MYLOCALE}\" />" >> TestResults_op_il_seq_point.xml.header
echo "  <test-suite name=\"op_il_seq_point-tests.dummy\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> TestResults_op_il_seq_point.xml.header
echo "    <results>" >> TestResults_op_il_seq_point.xml.header
echo "      <test-suite name=\"MonoTests\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> TestResults_op_il_seq_point.xml.header
echo "        <results>" >> TestResults_op_il_seq_point.xml.header
echo "          <test-suite name=\"op_il_seq_point\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> TestResults_op_il_seq_point.xml.header
echo "            <results>" >> TestResults_op_il_seq_point.xml.header

cat TestResults_op_il_seq_point.xml.header TestResults_op_il_seq_point.xml > TestResults_op_il_seq_point.xml.new
mv TestResults_op_il_seq_point.xml.new TestResults_op_il_seq_point.xml
rm -f TestResults_op_il_seq_point.xml.header
