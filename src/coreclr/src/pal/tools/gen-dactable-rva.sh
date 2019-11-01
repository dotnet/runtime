nm $1 | grep g_dacTable | cut -f 1 -d' ' | head -n 1 | awk '{ print "#define DAC_TABLE_RVA 0x" $1}' > $2
