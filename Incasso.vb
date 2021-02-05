﻿Imports System.Xml
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions

Module Incasso

    Sub Create_Incasso_Journals()
        'goed nadenken over het genereren van een naam voor een (groep) journaaltransactie
        'Dgv_incasso vervangen door dst
        Dim isd As Date = SPAS.Dtp_Incasso_start.Value
        Dim s1 = Year(isd) & "-" & Month(isd) & "-01"
        Dim overhead As Integer
        overhead = QuerySQL("SELECT value FROM settings WHERE label = 'overhead'")

        Dim journal_name = "Contract incasso " & Month(isd) & "-" & Year(isd)
        If QuerySQL("Select count(*) FROM journal WHERE name='" & journal_name & "'") > 0 Then
            MsgBox(journal_name & " bestaat al, graag eerst verwijderen alvorens een nieuwe aan te maken.")
            Exit Sub
        End If

        Collect_data(Create_Incasso_Bookings(s1))
        'Clipboard.Clear()
        'Clipboard.SetText(Create_Incasso_Bookings(s1, SPAS.Dtp_Incasso_end.Value))



        Dim SQLstr = "
                INSERT INTO journal
                    (date,status,amt1,description,source, fk_account,fk_relation,name) VALUES "

        For x As Integer = 0 To dst.Tables(0).Rows.Count - 1

            SQLstr &= "('" &
                SPAS.Dtp_Incasso_start.Value & "','Open','" & 'date/status
                Cur2(dst.Tables(0).Rows(x)(3)) & "','" & 'donation->amt1
                dst.Tables(0).Rows(x)(1) & "','Incasso','" & 'description/source
                dst.Tables(0).Rows(x)(5) & "','" & 'fk_account
                dst.Tables(0).Rows(x)(6) & "','" &
                journal_name & "')," 'fk_relation/name

            If dst.Tables(0).Rows(x)(4) > 0 Then
                SQLstr &= "('" &
                SPAS.Dtp_Incasso_start.Value & "','Open','" & 'date/status
                Cur2(dst.Tables(0).Rows(x)(4)) & "','" & 'overhead->amt1
                dst.Tables(0).Rows(x)(1) & "','Incasso','" & 'description/source
                overhead & "','" &   'incasso
                dst.Tables(0).Rows(x)(6) & "','" &
                journal_name & "')," 'fk_relation/name
            End If

        Next
        'MsgBox(Left(SQLstr, Strings.Len(SQLstr) - 1))
        RunSQL(Left(SQLstr, Strings.Len(SQLstr) - 1), "NULL", "Create_Incasso_Journals")
    End Sub
    Sub Create_SEPA_XML()


        Dim isd As Date = SPAS.Dtp_Incasso_start.Value
        Dim s1 = Year(isd) & "-" & Month(isd) & "-01"
        Dim MsgId = "Contract incasso " & Month(isd) & "-" & Year(isd)

        Dim f As System.IO.StreamWriter
        Dim filename = "Incassojob_" & Month(isd) & "_" & Year(isd) & ".xml"

        Collect_data(Create_Incasso_Totals(s1))
        'Clipboard.Clear()
        'Clipboard.SetText(Create_Incasso_Totals(s1))
        'MsgBox("Create_Incasso_Totals(s1, SPAS.Dtp_Incasso_end.Value)")

        Dim nr As Integer = dst.Tables(0).Rows(0)(1) + dst.Tables(0).Rows(1)(1)
        Dim amt = Replace(CDbl(dst.Tables(0).Rows(0)(2) + dst.Tables(0).Rows(1)(2)).ToString("F2"), ",", ".")

        Dim pi = MsgId
        Dim Inc_date As Date = Format(Date.Today, "yyyy-MM-dd")
        Dim text_child = QuerySQL("Select value From settings WHERE label='bank_kind'")
        Dim text_elder = QuerySQL("Select value From settings WHERE label='bank_oudere'")
        'retrieve account data

        Collect_data("SELECT owner,accountno,bic,id2 FROM bankacc WHERE accountno='" &
                     SPAS.Cmx_Incasso_Bankaccount.Text & "'")
        If IsDBNull(dst.Tables(0).Rows(0)(2)) Or IsDBNull(dst.Tables(0).Rows(0)(3)) Then
            MsgBox("Van een incassorekening moet de BIC en bank id ingevuld zijn.")
            Exit Sub
        End If

        Dim fnd As String = dst.Tables(0).Rows(0)(0)
        Dim iban As String = dst.Tables(0).Rows(0)(1)
        Dim bic = Strings.Trim(dst.Tables(0).Rows(0)(2))
        Dim id2 = Strings.Trim(dst.Tables(0).Rows(0)(3))



        Collect_data(Create_Incasso(s1))

        ' Dim amtstr As String = CStr(amt.ToString) & ".00"

        Dim SelectFolder As New FolderBrowserDialog

        With SelectFolder
            .SelectedPath = My.Settings._excassopath
            .ShowNewFolderButton = True
        End With

        If (SelectFolder.ShowDialog() = DialogResult.OK) Then
            filename = SelectFolder.SelectedPath & "\" & filename
            My.Settings._excassopath = SelectFolder.SelectedPath
        End If


        f = My.Computer.FileSystem.OpenTextFileWriter(filename, False)

        'H E A D E R ====================

        f.WriteLine("<?xml version=""1.0"" encoding=""UTF-8"" ?>")
        f.WriteLine("<Document xmlns=""urn:iso:std:iso:20022:tech:xsd:pain.008.001.02"" xmlns:xsi=""http://www.w3.org/2001/xmlSchema-instance"">")
        f.WriteLine("<!-- HOET -->")
        f.WriteLine("<CstmrDrctDbtInitn>")
        f.WriteLine(Tabs(1) & "<GrpHdr>")
        f.WriteLine(Tabs(2) & "<MsgId>" & MsgId & "</MsgId>")
        f.WriteLine(Tabs(2) & "<CreDtTm>" & Format(Date.Now, "yyyy-MM-ddTHH:mm:ss") & "</CreDtTm>")
        f.WriteLine(Tabs(2) & "<NbOfTxs>" & nr.ToString & "</NbOfTxs>")
        f.WriteLine(Tabs(2) & "<CtrlSum>" & amt & "</CtrlSum>")
        f.WriteLine(Tabs(2) & "<InitgPty>")
        f.WriteLine(Tabs(3) & "<Nm>" & fnd & "</Nm>")
        f.WriteLine(Tabs(2) & "</InitgPty>")
        f.WriteLine(Tabs(1) & "</GrpHdr>")

        'payment info
        f.WriteLine(Tabs(1) & "<PmtInf>")

        f.WriteLine(Tabs(2) & "<PmtInfId>" & pi & "</PmtInfId>")
        f.WriteLine(Tabs(2) & "<PmtMtd>DD</PmtMtd>")
        f.WriteLine(Tabs(2) & "<BtchBookg>true</BtchBookg>")
        f.WriteLine(Tabs(2) & "<PmtTpInf>")
        f.WriteLine(Tabs(3) & "<SvcLvl>")
        f.WriteLine(Tabs(4) & "<Cd>SEPA</Cd>")
        f.WriteLine(Tabs(3) & "</SvcLvl>")
        f.WriteLine(Tabs(3) & "<LclInstrm>")
        f.WriteLine(Tabs(4) & "<Cd>CORE</Cd>")
        f.WriteLine(Tabs(3) & "</LclInstrm>")
        f.WriteLine(Tabs(4) & "<SeqTp>RCUR</SeqTp>")
        f.WriteLine(Tabs(2) & "</PmtTpInf>")

        f.WriteLine(Tabs(2) & "<ReqdColltnDt>" & Format(Inc_date, "yyyy-MM-dd") & "</ReqdColltnDt>")
        f.WriteLine(Tabs(2) & "<Cdtr>")
        f.WriteLine(Tabs(3) & "<Nm>" & fnd & "</Nm>")
        f.WriteLine(Tabs(2) & "</Cdtr>")
        f.WriteLine(Tabs(2) & "<CdtrAcct>")
        f.WriteLine(Tabs(3) & "<Id>")
        f.WriteLine(Tabs(4) & "<IBAN>" & iban & "</IBAN>")
        f.WriteLine(Tabs(3) & "</Id>")
        f.WriteLine(Tabs(2) & "</CdtrAcct>")
        f.WriteLine(Tabs(2) & "<CdtrAgt>")
        f.WriteLine(Tabs(3) & "<FinInstnId>")
        f.WriteLine(Tabs(4) & "<BIC>" & bic & "</BIC>")
        f.WriteLine(Tabs(3) & "</FinInstnId>")
        f.WriteLine(Tabs(2) & "</CdtrAgt>")
        f.WriteLine(Tabs(2) & "<ChrgBr>SLEV</ChrgBr>")
        f.WriteLine(Tabs(2) & "<CdtrSchmeId>")
        f.WriteLine(Tabs(3) & "<Id>")
        f.WriteLine(Tabs(4) & "<PrvtId>")
        f.WriteLine(Tabs(5) & "<Othr>")
        f.WriteLine(Tabs(6) & "<Id>" & id2 & "</Id>")
        f.WriteLine(Tabs(6) & "<SchmeNm>")
        f.WriteLine(Tabs(7) & "<Prtry>SEPA</Prtry>")
        f.WriteLine(Tabs(6) & "</SchmeNm>")
        f.WriteLine(Tabs(5) & "</Othr>")
        f.WriteLine(Tabs(4) & "</PrvtId>")
        f.WriteLine(Tabs(3) & "</Id>")
        f.WriteLine(Tabs(2) & "</CdtrSchmeId>")

        'individual payments
        For i = 0 To nr - 1
            'Dim ttype = IIf(dst.Tables(0).Rows(i)(3) = "Kind", "KINDEREN", "OUDEREN")
            Dim relmsg = IIf(dst.Tables(0).Rows(i)(3) = "Kind", text_child, text_elder)
            Dim relnam = dst.Tables(0).Rows(i)(0)
            Dim iban2 = dst.Tables(0).Rows(i)(2)
            Dim mancod = dst.Tables(0).Rows(i)(4)
            Dim mandat = Format(CDate(dst.Tables(0).Rows(i)(5)), "yyyy-MM-dd")
            Dim gift = Replace(dst.Tables(0).Rows(i)(1).ToString, ",", ".")


            f.WriteLine(Tabs(2) & "<DrctDbtTxInf>")
            f.WriteLine(Tabs(3) & "<PmtId>")
            f.WriteLine(Tabs(4) & "<EndToEndId>" & Format(Date.Today, "yyyy-MM-dd") & "-" & Strings.Right("-0000" & i + 1, 6) & "</EndToEndId>")
            f.WriteLine(Tabs(3) & "</PmtId>")
            f.WriteLine(Tabs(4) & "<InstdAmt Ccy=""EUR"">" & gift & "</InstdAmt>")
            f.WriteLine(Tabs(3) & "<DrctDbtTx>")
            f.WriteLine(Tabs(4) & "<MndtRltdInf>")

            f.WriteLine(Tabs(5) & "<MndtId>" & mancod & "</MndtId>")
            f.WriteLine(Tabs(5) & "<DtOfSgntr>" & mandat & "</DtOfSgntr>")
            f.WriteLine(Tabs(5) & "<AmdmntInd>false</AmdmntInd>")

            f.WriteLine(Tabs(4) & "</MndtRltdInf>")
            f.WriteLine(Tabs(3) & "</DrctDbtTx>")
            f.WriteLine(Tabs(3) & "<DbtrAgt>")
            f.WriteLine(Tabs(4) & "<FinInstnId></FinInstnId>")
            f.WriteLine(Tabs(3) & "</DbtrAgt>")
            f.WriteLine(Tabs(3) & "<Dbtr>")
            f.WriteLine(Tabs(4) & "<Nm>" & relnam & "</Nm>")
            f.WriteLine(Tabs(3) & "<PstlAdr>")
            f.WriteLine(Tabs(4) & "<Ctry>NL</Ctry>")
            f.WriteLine(Tabs(3) & "</PstlAdr>")
            f.WriteLine(Tabs(3) & "</Dbtr>")
            f.WriteLine(Tabs(3) & "<DbtrAcct>")
            f.WriteLine(Tabs(4) & "<Id>")
            f.WriteLine(Tabs(5) & "<IBAN>" & iban2 & "</IBAN>")
            f.WriteLine(Tabs(4) & "</Id>")
            f.WriteLine(Tabs(3) & "</DbtrAcct>")
            f.WriteLine(Tabs(3) & "<Purp>")
            f.WriteLine(Tabs(4) & "<Cd>OTHR</Cd>")
            f.WriteLine(Tabs(3) & "</Purp>")
            f.WriteLine(Tabs(3) & "<RmtInf>")
            f.WriteLine(Tabs(4) & "<Ustrd>" & relmsg & "</Ustrd>")
            f.WriteLine(Tabs(3) & "</RmtInf>")
            f.WriteLine(Tabs(2) & "</DrctDbtTxInf>")
        Next

        f.WriteLine(Tabs(1) & "</PmtInf>")
        f.WriteLine("</CstmrDrctDbtInitn>")
        f.WriteLine("</Document>")

        f.Close()

        MsgBox("De incassojob is gecreëerd en beschikbaar.")

    End Sub

    Sub Format_dvg_incasso_overview1()

        If SPAS.Dgv_Bank_Account.Rows.Count = 0 Then Exit Sub  'de vraag is of dit correct is
        Try
            With SPAS.Dgv_Bank_Account

                .Columns(0).HeaderText = "Doeltype"
                .Columns(1).Width = 100
                .Columns(0).ReadOnly = True


                .Columns(1).HeaderText = "Aantal"
                .Columns(2).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                .Columns(1).Width = 150
                .Columns(1).ReadOnly = True

                .Columns(2).HeaderText = "Bedrag"
                .Columns(2).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(2).Width = 150
                .Columns(2).DefaultCellStyle.Format = "N2"
                .Columns(2).ReadOnly = True

                '.Columns(2).DefaultCellStyle.ForeColor = Color.Blue
                '.Columns(0).Visible = False

            End With
        Catch
        End Try

    End Sub






    '=============================================================================================================
    '==============  E   X   C   A   S   S   O   ================================================================= 
    '=============================================================================================================
    '@@@ ERROR 1: bij overmaken wordt het bedrag niet van de juiste post afgetrokken (eerst intern, dan extra, dan 
    'contract
    'ERROR 2: CP naam nog niet in omschrijving van journaalpost 
    'ERROR 3: schiet naar budget view ook al is "nulwaarden" geselecteerd
    'ERROR 4: bij meerdere excasso's op een dag is de naam niet uniek/Jobnummer wordt niet opgehoogd na creatie van een nieuwe job
    'ERROR 5: Geen toets of bedragen hoger zijn dan saldo als deze niet individueel bewerkt worden
    'ERROR 6: 
    'OPEN 5: selecteren van bestaande excasso's: moeten gepresenteerd worden 


    Sub Convert_Null_to_0()
        For x As Integer = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1
            For y = 2 To 7
                If IsDBNull(SPAS.Dgv_Excasso2.Rows(x).Cells(y).Value) Then SPAS.Dgv_Excasso2.Rows(x).Cells(y).Value = CDec(0)
            Next
        Next
    End Sub
    Sub Set_Excasso_Nullvalues
        If SPAS.Dgv_Excasso2.Rows.Count > 0 Then
            'Convert_Null_to_0()
            For x As Integer = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1
                SPAS.Dgv_Excasso2.Rows(x).Cells(6).Value = CDec(0)
                SPAS.Dgv_Excasso2.Rows(x).Cells(7).Value = 0
            Next
            SPAS.Lbl_Excasso_Items_Totaal.Text = 0
            SPAS.Lbl_Excasso_Items_Contract.Text = 0
            SPAS.Lbl_Excasso_Items_Extra.Text = 0
            SPAS.Lbl_Excasso_Items_Intern.Text = 0
            SPAS.Lbl_Excasso_Contractwaarde.Text = 0
            SPAS.Lbl_Excasso_Extra.Text = 0
            SPAS.Lbl_Excasso_Intern.Text = 0
            SPAS.Lbl_Excasso_Totaal.Text = 0
            SPAS.Lbl_Excasso_Tot_Gen_MLD.Text = 0
            SPAS.Lbl_Excasso_Tot_Gen.Text = 0
            Calculate_CP_Allowance()
        End If
    End Sub

    Sub Calculate_Excasso_Amounts(ByVal col As Integer)
        'purpose of this module is to calculate excasso amounts based on the balances per beneficiairy
        Dim j_cntr, j_extr, j_inte, j_amt As Decimal


        For x As Integer = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1

            'rows
            j_cntr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(col).Value)
            j_extr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(4).Value)
            j_inte = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(5).Value)
            j_amt = j_cntr + j_extr + j_inte
            If j_amt < 0 Then j_amt = 0
            SPAS.Dgv_Excasso2.Rows(x).Cells(6).Value = j_amt
            SPAS.Dgv_Excasso2.Rows(x).Cells(7).Value = j_amt * Tbx2Dec(SPAS.Tbx_Excasso_Exchange_rate.Text)

            'total amounts
            't_cntr = t_cntr + j_cntr
            't_extr = t_extr + j_extr
            't_inte = t_inte + j_inte
            't_amt = t_amt + j_amt
        Next

        'Hier kan ik wellicht startsaldo's in meenemen

        'SPAS.Lbl_Excasso_Contractwaarde.Text = Tbx2Int(t_cntr)
        'SPAS.Lbl_Excasso_Extra.Text = Tbx2Int(t_extr)
        'SPAS.Lbl_Excasso_Intern.Text = Tbx2Int(t_inte)
        'SPAS.Lbl_Excasso_Totaal.Text = Tbx2Int(t_amt)

    End Sub

    Sub Calculate_Excasso_Totals()
        'This module calculate the total and number of amounts, both excasso amount as 
        'underlaying contracts, extra gifts and internal fund payments. 
        'If the total amount is less than the underlying amounts, then the following rule applies
        'Contract amounts has priority above extra amount has priority above internal funding
        'For contract we always take what is highest: contract budget or saldo

        Dim j_cntr, j_extr, j_inte, j_amt, diff As Decimal
        Dim cnt0, cnt1, cnt2, cnt3 As Integer
        Dim col_tot1, col_tot2, col_tot3 As Decimal
        Dim msg As String = ""

        For x As Integer = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1
            If CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(2).Value) > CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(3).Value) Then
                j_cntr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(2).Value)
            Else
                j_cntr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(3).Value)
            End If
            j_extr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(4).Value)
            j_inte = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(5).Value)

            j_amt = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(6).Value)

            diff = j_amt - (j_cntr + j_extr + j_inte)
            If diff < 0 Then  'groter dan 0 zou niet mogen kunnen!
                j_inte = j_inte + diff
                If j_inte < 0 Then
                    j_extr = j_extr + j_inte
                    j_inte = 0
                    If j_extr < 0 Then
                        j_cntr = j_cntr + j_extr
                        j_extr = 0
                    End If
                End If
            End If
            msg &= vbCr &
                    "Diff: " & diff &
                    "  /j_inte: " & j_inte &
                    "  /j_extr: " & j_extr &
                    "  /j_cntr: " & j_cntr

            If j_amt > 0 Then
                cnt0 = cnt0 + 1
                cnt1 = cnt1 + IIf(j_cntr > 0, 1, 0)
                cnt2 = cnt2 + IIf(j_extr > 0, 1, 0)
                cnt3 = cnt3 + IIf(j_inte > 0, 1, 0)
                col_tot1 = col_tot1 + j_cntr
                col_tot2 = col_tot2 + j_extr
                col_tot3 = col_tot3 + j_inte
                'SPAS.Dgv_Excasso2.Rows(x).Cells(6).Value = j_amt
            End If


        Next x
        'MsgBox(msg)

        SPAS.Lbl_Excasso_Items_Totaal.Text = cnt0
        SPAS.Lbl_Excasso_Items_Contract.Text = cnt1
        SPAS.Lbl_Excasso_Items_Extra.Text = cnt2
        SPAS.Lbl_Excasso_Items_Intern.Text = cnt3
        SPAS.Lbl_Excasso_Contractwaarde.Text = Tbx2Int(col_tot1)
        SPAS.Lbl_Excasso_Extra.Text = Tbx2Int(col_tot2)
        SPAS.Lbl_Excasso_Intern.Text = Tbx2Int(col_tot3)
        SPAS.Lbl_Excasso_Totaal.Text = Tbx2Int(col_tot1 + col_tot2 + col_tot3)
        SPAS.Lbl_Excasso_Totaal_MDL.Text = CInt(SPAS.Lbl_Excasso_Totaal.Text * Tbx2Dec(SPAS.Tbx_Excasso_Exchange_rate.Text))
        SPAS.Lbl_Excasso_Tot_Gen.Text = Tbx2Int(SPAS.Lbl_Excasso_Totaal.Text) + Tbx2Int(SPAS.Lbl_Excasso_CP_Totaal.Text) * 1
        SPAS.Lbl_Excasso_Tot_Gen_MLD.Text = CInt(CInt(SPAS.Lbl_Excasso_Tot_Gen.Text) * Tbx2Dec(SPAS.Tbx_Excasso_Exchange_rate.Text))
        SPAS.Btn_Excasso_CP_Calculate.Enabled = True

    End Sub

    Sub Calculate_CP_Allowance()
        Dim ci As Integer = SPAS.Lbl_Excasso_Items_Contract.Text
        Dim cw As Integer = SPAS.Lbl_Excasso_Contractwaarde.Text
        Dim n1 As Integer = SPAS.Tbx_Excasso_Norm1.Text
        Dim Val1 As Integer = IIf(SPAS.Btn_Excasso_Base1.Text = "€", n1 * ci, (n1 / 100) * cw)

        Dim ei As Integer = SPAS.Lbl_Excasso_Items_Extra.Text
        Dim ew As Integer = SPAS.Lbl_Excasso_Extra.Text
        Dim n2 As Integer = SPAS.Tbx_Excasso_Norm2.Text
        Dim Val2 As Integer = IIf(SPAS.Btn_Excasso_Base2.Text = "€", n2 * ei, (n2 / 100) * ew)

        Dim ii As Integer = SPAS.Lbl_Excasso_Items_Intern.Text
        Dim iw As Integer = SPAS.Lbl_Excasso_Intern.Text
        Dim n3 As Integer = SPAS.Tbx_Excasso_Norm3.Text
        Dim Val3 As Integer = IIf(SPAS.Btn_Excasso_Base3.Text = "€", n3 * ii, (n3 / 100) * iw)

        SPAS.Lbl_Excasso_Contract.Text = SPAS.Lbl_Excasso_Items_Contract.Text & " contracten, €" _
        & SPAS.Lbl_Excasso_Contractwaarde.Text & " à"
        SPAS.Lbl_Excasso_Extr.Text = SPAS.Lbl_Excasso_Items_Extra.Text & " extra giften, €" _
        & SPAS.Lbl_Excasso_Extra.Text & " à"
        SPAS.Lbl_Excasso_Internal.Text = SPAS.Lbl_Excasso_Items_Intern.Text & " uit fonds, €" _
        & SPAS.Lbl_Excasso_Intern.Text & " à"
        'SPAS.Lbl_Excasso_Totalen.Text = "Totaal " & SPAS.Lbl_Excasso_Items_Totaal.Text & " posten/€" &
        'SPAS.Lbl_Excasso_Totaal.Text & "; voor CP:"


        SPAS.Tbx_Excasso_CP1.Text = Tbx2Int(Val1)
        SPAS.Tbx_Excasso_CP2.Text = Tbx2Int(Val2)
        SPAS.Tbx_Excasso_CP3.Text = Tbx2Int(Val3)
        'SPAS.Lbl_Excasso_Items_Totaal.Text = Tbx2Int(CInt(SPAS.Lbl_Excasso_Items_Contract.Text) +
        'CInt(SPAS.Lbl_Excasso_Items_Contract.Text) + CInt(SPAS.Lbl_Excasso_Items_Contract.Text))



    End Sub

    Sub Format_dvg_incasso()

        If SPAS.Dgv_Incasso.Rows.Count = 0 Then Exit Sub  'de vraag is of dit correct is
        Try
            With SPAS.Dgv_Incasso

                .Columns(0).HeaderText = "Donateur"
                .Columns(0).Width = 205
                .Columns(1).HeaderText = "Bedrag"
                .Columns(1).Width = 90
                .Columns(1).DefaultCellStyle.Format = "N2"
                .Columns(1).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(2).HeaderText = "Iban"
                .Columns(2).Width = 180
                .Columns(3).HeaderText = "Doeltype"
                .Columns(3).Width = 90
                .Columns(4).HeaderText = "Mandaatcode"
                .Columns(4).Width = 90

                .Columns(5).HeaderText = "Mandaatdatum"
                .Columns(5).Width = 90

            End With
        Catch
        End Try

    End Sub

    Sub Format_dvg_excasso()

        If SPAS.Dgv_Excasso2.Rows.Count = 0 Then Exit Sub  'de vraag is of dit correct is
        Try
            With SPAS.Dgv_Excasso2

                .Columns(0).HeaderText = "Id"
                .Columns(1).HeaderText = "Account naam"
                .Columns(2).HeaderText = "Maand budget"
                .Columns(3).HeaderText = "Saldo Contract"
                .Columns(4).HeaderText = "Saldo Extra gift"
                .Columns(5).HeaderText = "Saldo Fondsen"
                .Columns(6).HeaderText = "Uit te keren (EUR)"
                .Columns(7).HeaderText = "Uit te keren (MLD)"

                For c = 2 To 6
                    .Columns(c).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    .Columns(c).DefaultCellStyle.Format = "N2"
                    .Columns(c).Width = 80
                Next

                '.Columns(3).Visible = False
                .Columns(0).Width = 60
                .Columns(1).Width = 200

                For c = 0 To 5
                    .Columns(c).ReadOnly = True
                Next

                .Columns(6).DefaultCellStyle.ForeColor = Color.Blue
                .Columns(6).ReadOnly = False
                .Columns(7).DefaultCellStyle.ForeColor = Color.Green
                .Columns(7).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(7).Width = 80


            End With
        Catch
        End Try
        For x = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1
            For y = 0 To SPAS.Dgv_Excasso2.Columns.Count - 1
                If IsDBNull(SPAS.Dgv_Excasso2.Rows(x).Cells(y).Value) Then
                    SPAS.Dgv_Excasso2.Rows(x).Cells(y).Value = 0
                    SPAS.Dgv_Excasso2.Rows(x).Cells(y).Style.ForeColor = Color.LightGray
                End If
            Next y
        Next


    End Sub

    Sub Save_Excasso_job()
        'Controles:
        Dim exch As Decimal = CDec(SPAS.Tbx_Excasso_Exchange_rate.Text)
        Dim errmsg As String = ""
        Dim overhead = QuerySQL("SELECT value FROM settings WHERE label = 'overhead'")
        'If SPAS.Lbl_Excasso_Status.Text = "Verwerkt" Then errmsg &= "- verwerkte jobs kunnen niet verwijderd worden." & vbCrLf
        If SPAS.Lbl_Excasso_Totaal.Text = "0" And SPAS.Lbl_Excasso_CP_Totaal.Text = "0" Then errmsg &= "- het totaalbedrag is 0. " & vbCrLf
        'If SPAS.Btn_Excasso_CP_Calculate.Enabled = True Then errmsg &= "- CP berekening is niet ververst." & vbCrLf
        'If SPAS.Btn_Excasso_Exchrate.Enabled Then errmsg &= "- Wijziging in wisselkoers is niet doorgevoerd." & vbCrLf
        'If CDec(SPAS.Tbx_Excasso_Exchange_rate.Text) = 0 Then errmsg &= "- Wisselkoers mag niet 0 zijn."
        If errmsg <> "" Then
            MsgBox("Er zijn de volgende fouten geconstateerd:" & vbCrLf & errmsg)
            Exit Sub
        End If

        If SPAS.Btn_Excasso_CP_Calculate.Enabled = True Then
            If MsgBox("U heeft de CP-bijdrage nog niet gecalculeerd. Wilt u doorgaan?", vbYesNo) = vbNo Then Exit Sub
        End If
        If CDec(SPAS.Tbx_Excasso_Exchange_rate.Text) = 0 Then
            If MsgBox("De wisselkoers is 0 of nog niet ververst. Wilt u doorgaan?", vbYesNo) = vbNo Then Exit Sub
        End If
        'Dim cntold As Integer = 0
        Dim j_name As String
        If Strings.Left(SPAS.Cmx_Excasso_Select.SelectedItem, 13) <> "Nieuwe lijst " Then
            j_name = SPAS.Cmx_Excasso_Select.SelectedItem
            'cntold = QuerySQL("SELECT count(*) FROM journal WHERE name ILIKE '%" & j_name & "'")
            RunSQL("DELETE FROM journal WHERE name ILIKE '%" & j_name & "'", "NULL", "Save_Excasso_job 1")
            'MsgBox(cntold)
            'this is an existing excasso that need to be deleted first
        Else
            j_name = "Excasso-" &
                IIf(SPAS.Cbx_Uitkering_Kind.Checked, "K", "") &
                IIf(SPAS.Cbx_Uitkering_Oudere.Checked, "O", "") &
                IIf(SPAS.Cbx_Uitkering_Overig.Checked, "V", "") & "-" &
                Left(Mid(SPAS.Cmx_Excasso_Select.Text, 14), 4) & "-" &
                SPAS.Dtp_Excasso_Start.Value
            Dim cnt = QuerySQL("SELECT count(distinct name) FROM journal WHERE name LIKE '" & j_name & "%'")
            j_name &= "-" & (cnt + 1).ToString

        End If



        'determine values that are valid for all journalpost within this job:
        Dim j_amt1, j_budg, j_extr, j_inte, dif1 As Decimal
        Dim j_fkac As Integer
        'Dim j_name = SPAS.Lbl_Excasso_Job_Name.Text
        Dim j_date = SPAS.Dtp_Excasso_Start.Value.Year & "-" & SPAS.Dtp_Excasso_Start.Value.Month &
            "-" & SPAS.Dtp_Excasso_Start.Value.Day
        Dim SQLstr = "INSERT INTO journal(name, date,status,source,amt1,amt2,fk_account,description,type,cpinfo,iban) VALUES"
        Dim j_desc As String = "", j_desc2 As String = ""
        Dim j_cpinfo As String = SPAS.Lbl_Excasso_CPid.Text & "-0-0-0-0-0-0"
        Dim j_cp_fk = QuerySQL("Select id From account where f_key='" & SPAS.Lbl_Excasso_CPid.Text & "'")
        Dim j_iban = Strings.Trim(QuerySQL("
SELECT bankacc.accountno FROM cp LEFT JOIN bankacc ON bankacc.id = cp.fk_bankacc_id WHERE cp.id='" & SPAS.Lbl_Excasso_CPid.Text & "'"))



        'run the list
        '@@@ test if paid amount is lower than the budgetted/determined amounts that the difference is deducted for 
        '(1) Internal (2) Extra donation (3) Contract payment
        For x As Integer = 0 To SPAS.Dgv_Excasso2.Rows.Count - 1

            j_amt1 = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(6).Value)
            'j_budg = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(IIf(SPAS.Rbn_Excasso_Maandbudget.Checked, 2, 3)).Value)
            j_extr = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(4).Value)
            j_inte = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(5).Value)
            j_fkac = SPAS.Dgv_Excasso2.Rows(x).Cells(0).Value
            j_desc = "Uitkering aan " & SPAS.Dgv_Excasso2.Rows(x).Cells(1).Value
            j_desc2 = "Distribution costs " & SPAS.Cmx_Excasso_Select.SelectedText


            If CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(2).Value) > CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(3).Value) Then
                j_budg = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(2).Value)
            Else
                j_budg = CDec(SPAS.Dgv_Excasso2.Rows(x).Cells(3).Value)
            End If


            dif1 = j_amt1 - j_budg - j_extr - j_inte
            If dif1 < 0 Then  'groter dan 0 zou niet mogen kunnen!
                j_inte = j_inte + dif1
                If j_inte < 0 Then
                    j_extr = j_extr + j_inte
                    j_inte = 0
                    If j_extr < 0 Then
                        j_budg = j_budg + j_extr
                        j_extr = 0
                    End If
                End If
            End If


            Dim j_budg1 As String = j_budg.ToString
            j_budg1 = "-" & Replace(j_budg1, ",", ".")
            Dim j_extr1 As String = j_extr.ToString
            j_extr1 = "-" & Replace(j_extr1, ",", ".")
            Dim j_inte1 As String = j_inte.ToString
            j_inte1 = "-" & Replace(j_inte1, ",", ".")

            'MsgBox(j_budg1)
            If j_budg > 0 Then
                SQLstr &= "('" & j_name & "','" & j_date & "','" & "Open" & "','" & "Uitkering" & "','" &
                          j_budg1 & "','" & -CInt(j_budg * exch) & "','" & j_fkac & "','" &
                          j_desc & "','Contract','" & j_cpinfo & "','" & j_iban & "'),"
            End If
            If j_extr > 0 Then
                SQLstr &= "('" & j_name & "','" & j_date & "','" & "Open" & "','" & "Uitkering" & "','" &
                          j_extr1 & "','" & -CInt(j_extr * exch) & "','" & j_fkac & "','" &
                          j_desc & "','Extra','" & j_cpinfo & "','" & j_iban & "'),"
            End If
            If j_inte > 0 Then
                SQLstr &= "('" & j_name & "','" & j_date & "','" & "Open" & "','" & "Uitkering" & "','" &
                          j_inte1 & "','" & -CInt(j_inte * exch) & "','" & j_fkac & "','" &
                          j_desc & "','Internal','" & j_cpinfo & "','" & j_iban & "'),"
            End If
        Next
        'cp transactie toevoegen
        Dim j_cp = CDec(SPAS.Lbl_Excasso_CP_Totaal.Text)
        If j_cp > 0 Then
            'from overhead
            SQLstr &= "('Intern tbv CP " & j_name & "','" & j_date & "','" & "Verwerkt" & "','" & "Intern" & "','" &
                             Cur2(j_cp) * -1 & "','" & -CInt(j_cp * exch) & "','" & overhead &
                             "','" & j_desc2 & "', 'CP','" & j_cpinfo & "',''),"
            'to CP account
            SQLstr &= "('Intern tbv CP " & j_name & "','" & j_date & "','" & "Verwerkt" & "','" & "Intern" & "','" &
                             Cur2(j_cp) & "','" & CInt(j_cp * exch) & "','" & j_cp_fk &
                             "','" & j_desc2 & "', 'CP','" & j_cpinfo & "',''),"
            'from CP account 
            SQLstr &= "('" & j_name & "','" & j_date & "','" & "Open" & "','" & "Uitkering" & "','" &
                             Cur2(j_cp) * -1 & "','" & -CInt(j_cp * exch) & "','" & j_cp_fk &
                             "','" & j_desc2 & "', 'CP','" & j_cpinfo & "','" & j_iban & "'),"
        End If
        'Clipboard.Clear()
        'Clipboard.SetText(SQLstr)
        'MsgBox(SQLstr)
        SQLstr = Strings.Left(SQLstr, Strings.Len(SQLstr) - 1) 'remove the last comma
        RunSQL(SQLstr, "NULL", "Save Excasso job 2")
        'If cntold > 0 Then
        If Strings.Left(SPAS.Cmx_Excasso_Select.SelectedItem, 13) = "Nieuwe lijst " Then
            SPAS.Cmx_Excasso_Select.Items.Add(j_name)
            SPAS.Cmx_Excasso_Select.SelectedIndex = SPAS.Cmx_Excasso_Select.Items.Count - 1
        End If


    End Sub



    Sub Fill_Cmx_Excasso_Select_Combined()
        'this module combines existing excasso jobs and potential new ones (based on cp) in one combobox

        SPAS.Cmx_Excasso_Select.Items.Clear()

        Collect_data("
                    SELECT distinct(name) FROM journal
                    WHERE name ILIKE '%Excasso%'
                    AND status = 'Open'
                    GROUP By name, status
                    ")
        For x As Integer = 0 To dst.Tables(0).Rows.Count - 1
            SPAS.Cmx_Excasso_Select.Items.Add(dst.Tables(0).Rows(x)(0))
        Next

        Collect_data("
                    SELECT DISTINCT(cp.name), cp.name_add, cp.id FROM cp
                    LEFT JOIN target ta on fk_cp_id = cp.id
                    LEFT JOIN contract co on fk_target_id = ta.id
                    WHERE co.enddate > current_date
                    AND cp.active = 'True' 
                    ")
        For x As Integer = 0 To dst.Tables(0).Rows.Count - 1
            SPAS.Cmx_Excasso_Select.Items.Add("Nieuwe lijst " & dst.Tables(0).Rows(x)(0) _
            & ", " & dst.Tables(0).Rows(x)(1) & " [" & dst.Tables(0).Rows(x)(2) & "]")

        Next


    End Sub
    Function Create_Incasso(date_start As String)
        Dim SQLstr = "
            SELECT Concat(r.name, ', ', r.name_add), 
            sum((co.donation+co.overhead)/co.term),
            r.iban, ta.ttype, 
            CASE 
	            WHEN ta.ttype = 'Kind' Then Concat('k', r.reference)
	            WHEN ta.ttype = 'Oudere' Then Concat('o',r.reference)
                WHEN ta.ttype = 'Overig' Then Concat('v',r.reference)
            END,
            CASE 
	            WHEN ta.ttype = 'Kind' Then r.date1
	            WHEN ta.ttype = 'Oudere' Then r.date2
                WHEN ta.ttype = 'Oudere' Then r.date3
            END
            FROM contract co 
            LEFT JOIN Target ta ON co.fk_target_id = ta.id
            LEFT JOIN Relation r ON co.fk_relation_id = r.id
            WHERE co.autcol = True 
            AND co.startdate <= '" & date_start & "' 
            AND co.enddate > '" & date_start & "'
            AND 
            ((r.date1 <='" & date_start & "' AND ta.ttype = 'Kind') OR
            (r.date2 <='" & date_start & "' AND ta.ttype = 'Oudere') OR
            (r.date3 <='" & date_start & "' AND ta.ttype = 'Oudere'))
            GROUP BY  r.reference, r.name, r.name_add, r.iban, ta.ttype, r.date1, r.date2, r.date3
            ORDER by  ta.ttype, r.reference

"
        Return SQLstr
        'Clipboard.Clear()
        'Clipboard.SetText(SQLstr)

    End Function

    Sub Manage_StartSaldo()
        'van alle accounts waar het startsaldo ongelijk aan 0 is: creëer een startsaldo in bulk onder de source startsaldo
        'en type "Extra"? (kan ook overloop zijn van contract vorig jaar)
        'stappen
        '1) verwijder alle startsaldo's' 
        '2) load startsaldo's van account
        '3) creëer accounts met startsaldi
        Collect_data("select id, name, startsaldo from account where startsaldo::money>'0'")
        Dim SQLstr As String = "INSERT INTO journal
                    (date,status,amt1,description,source, fk_account,name) VALUES "

        For x As Integer = 0 To dst.Tables(0).Rows.Count - 1

            SQLstr &= "('" &
                "2020-01-01','Verwerkt','" & 'date/status
                Cur2(dst.Tables(0).Rows(x)(2)) & "','" & 'donation->amt1
                dst.Tables(0).Rows(x)(1) & "','Internal','" & 'description/source
                dst.Tables(0).Rows(x)(0) & "','" & 'fk_account
                "Startsaldo " & dst.Tables(0).Rows(x)(1) & "'),"

        Next x

        'MsgBox(Left(SQLstr, Strings.Len(SQLstr) - 1))
        'Clipboard.Clear()
        'Clipboard.SetText(SQLstr)
        RunSQL(Left(SQLstr, Strings.Len(SQLstr) - 1), "NULL", "Create_Incasso_Journals")




    End Sub

End Module
