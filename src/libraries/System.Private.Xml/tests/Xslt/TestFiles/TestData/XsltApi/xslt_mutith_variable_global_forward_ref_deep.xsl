<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:variable name="A" select="$B"/>
  <xsl:variable name="B" select="$C"/>
  <xsl:variable name="C" select="$D"/>
  <xsl:variable name="D" select="$E"/>
  <xsl:variable name="E" select="$F"/>
  <xsl:variable name="F" select="$G"/>
  <xsl:variable name="G" select="$H"/>
  <xsl:variable name="H" select="$I"/>
  <xsl:variable name="I" select="$J"/>
  <xsl:variable name="J" select="$K"/>
  <xsl:variable name="K" select="$L"/>
  <xsl:variable name="L" select="$M"/>
  <xsl:variable name="M" select="$N"/>
  <xsl:variable name="N" select="$O"/>
  <xsl:variable name="O" select="$P"/>
  <xsl:variable name="P" select="$R"/>
  <xsl:variable name="R" select="$S"/>
  <xsl:variable name="S" select="$T"/>
  <xsl:variable name="T" select="$U"/>
  <xsl:variable name="U" select="$V"/>
  <xsl:variable name="V" select="$Y"/>
  <xsl:variable name="Y" select="$Z"/>
  <xsl:variable name="Z"/>

  <xsl:variable name="A1" select="$B"/>
  <xsl:variable name="B1" select="$C"/>
  <xsl:variable name="C1" select="$D"/>
  <xsl:variable name="D1" select="$E"/>
  <xsl:variable name="E1" select="$F"/>
  <xsl:variable name="F1" select="$G"/>
  <xsl:variable name="G1" select="$H"/>
  <xsl:variable name="H1" select="$I"/>
  <xsl:variable name="I1" select="$J"/>
  <xsl:variable name="J1" select="$K"/>
  <xsl:variable name="K1" select="$L"/>
  <xsl:variable name="L1" select="$M"/>
  <xsl:variable name="M1" select="$N"/>
  <xsl:variable name="N1" select="$O"/>
  <xsl:variable name="O1" select="$P"/>
  <xsl:variable name="P1" select="$R"/>
  <xsl:variable name="R1" select="$S"/>
  <xsl:variable name="S1" select="$T"/>
  <xsl:variable name="T1" select="$U"/>
  <xsl:variable name="U1" select="$V"/>
  <xsl:variable name="V1" select="$Y"/>
  <xsl:variable name="Y1" select="$Z"/>
  <xsl:variable name="Z1"/>

  <xsl:variable name="A2" select="$B2 > $C2 > $D2 > $E2 > $F2 > $G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="B2" select="$C2 > $D2 > $E2 > $F2 > $G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="C2" select="$D2 > $E2 > $F2 > $G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="D2" select="$E2 > $F2 > $G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="E2" select="$F2 > $G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="F2" select="$G2 > $H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="G2" select="$H2 > $I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="H2" select="$I2 > $J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="I2" select="$J2 > $K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="J2" select="$K2 > $L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="K2" select="$L2 > $M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="L2" select="$M2 > $N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="M2" select="$N2 > $O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="N2" select="$O2 > $P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="O2" select="$P2 > $R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="P2" select="$R2 > $S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="R2" select="$S2 > $T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="S2" select="$T2 > $U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="T2" select="$U2 > $V2 > $Y2 > $Z2"/>
  <xsl:variable name="U2" select="$V2 > $Y2 > $Z2"/>
  <xsl:variable name="V2" select="$Y2 > $Z2"/>
  <xsl:variable name="Y2" select="$Z2"/>
  <xsl:variable name="Z2"/>


  <xsl:variable name="Z3"/>
  <xsl:variable name="R3" select="$S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="O3" select="$P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="V3" select="$Y3 > $Z3"/>
  <xsl:variable name="Y3" select="$Z3"/>
  <xsl:variable name="S3" select="$T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="F3" select="$G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="G3" select="$H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="T3" select="$U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="A3" select="$B3 > $C3 > $D3 > $E3 > $F3 > $G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="J3" select="$K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="I3" select="$J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="B3" select="$C3 > $D3 > $E3 > $F3 > $G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="L3" select="$M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="M3" select="$N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="C3" select="$D3 > $E3 > $F3 > $G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="U3" select="$V3 > $Y3 > $Z3"/>
  <xsl:variable name="E3" select="$F3 > $G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="H3" select="$I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="D3" select="$E3 > $F3 > $G3 > $H3 > $I3 > $J3 > $K3 > $L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="K3" select="$L3 > $M3 > $N3 > $O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="N3" select="$O3 > $P3 > $R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  <xsl:variable name="P3" select="$R3 > $S3 > $T3 > $U3 > $V3 > $Y3 > $Z3"/>
  
  
</xsl:stylesheet>