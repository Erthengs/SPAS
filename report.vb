﻿Imports System.Windows.Forms.VisualStyles
Imports Microsoft.EntityFrameworkCore.Metadata

Module report
    Function Report_table(report_year)
        If CInt(report_year) >= CInt(QuerySQL("select min(extract (year from date)) from journal")) Then
            Return "journal"

        Else
            Return "journal_archive"
        End If

    End Function
    Function Bank_table(report_year)
        If CInt(report_year) >= CInt(QuerySQL("select min(extract (year from date)) from journal")) Then
            Return "bank"
        Else
            Return "bank_archive"
        End If
    End Function


    Sub Report_overview()


        Dim jtable, btable, yearcheck_j2 As String
        jtable = Report_table(report_year)
        btable = Bank_table(report_year)
        yearcheck_j2 = "and extract(year from date)=" & report_year
        'Dim tabname As String
        'For x = 0 To 1
        'tabname = SPAS.TC_Rapportage.TabPages(x).Text
        'If InStr(1, tabname, " (") > 0 Then SPAS.TC_Rapportage.TabPages(x).Text = Trim(Strings.Left(tabname, InStr(1, tabname, " (")))
        'SPAS.TC_Rapportage.TabPages(x).Text &= " (" & report_year & ")"
        'Next x



        Dim sqlstr As String = ""
        Dim select_j2 = "select sum(amt1) from " & jtable & " j2 left join account a2 on a2.id = j2.fk_account where a2.fk_accgroup_id = a.fk_accgroup_id and j2.status != 'Open' "
        Dim select_j = "select sum(amt1) from " & jtable & " j left join account a on a.id = j.fk_account where extract(year from j.date)=" & report_year & " and status != 'Open' and a.fk_accgroup_id in (select id from accgroup where type = '"
        Dim select_sum = "(select sum(amt1) from " & jtable & " j left join account a2 on a2.id = j.fk_account where extract(year from j.date)=" & report_year
        Dim un As String = ""
        Dim gtype() As String = {"Inkomsten", "Uitgaven", "Transit"}
        Dim rl As Integer = 0

        For i = 0 To 2

            un = IIf(i > 0, "union ", "")
            sqlstr &= un &
        "	select " & rl & ", '" & gtype(i) & "', null, null, null, null, null, null
            union select " & rl + 1 & ", ag.name
            ,(" & select_j2 & yearcheck_j2 & " and j2.source = 'Closing')
            ,(" & select_j2 & yearcheck_j2 & " and j2.source = 'Incasso')
            ,(" & select_j2 & yearcheck_j2 & " and j2.source = 'Bank')
            ,(" & select_j2 & yearcheck_j2 & " and j2.source = 'Intern')
            ,(" & select_j2 & yearcheck_j2 & " and j2.source = 'Uitkering')
            ,(" & select_j2 & yearcheck_j2 & ")
            from " & jtable & " j left join account a on a.id = fk_account left join accgroup ag on ag.id = a.fk_accgroup_id
            where status != 'Open' and ag.type = '" & gtype(i) & "' and extract(year from date)=" & CInt(report_year) & " group by ag.name, a.fk_accgroup_id
            --having (select sum(amt1) from " & jtable & " j2 left join account a2 on a2.id = j2.fk_account where a2.fk_accgroup_id = a.fk_accgroup_id) != '0.00'
            union select " & rl + 2 & ", 'Totaal' 
            ,(" & select_j & gtype(i) & "') and j.source = 'Closing')
            ,(" & select_j & gtype(i) & "') and j.source = 'Incasso')
            ,(" & select_j & gtype(i) & "') and j.source = 'Bank')
            ,(" & select_j & gtype(i) & "') and j.source = 'Intern')
            ,(" & select_j & gtype(i) & "') and j.source = 'Uitkering')
            ,(" & select_j & gtype(i) & "') and j.status != 'Open')
            union select " & rl + 3 & ", null, null, null, null, null, null, null "
            rl = rl + 4
        Next i

        sqlstr &= "
            --union select 12, null, null, null, null, null, null, null 
            union select 12, 'Totaal generaal' 
            ," & select_sum & " and j.source = 'Closing' and status != 'Open')
            ," & select_sum & " and j.source = 'Incasso' and status != 'Open')
            ," & select_sum & " and j.source = 'Bank' and status != 'Open')
            ," & select_sum & " and j.source = 'Intern' and status != 'Open')
            ," & select_sum & " and j.source = 'Uitkering' and status != 'Open')
            ," & select_sum & " and j.status != 'Open')

        "
        SPAS.ToClipboard(sqlstr, True)


        Load_Datagridview(SPAS.Dgv_Rapportage_Overzicht, sqlstr, "rapportagefout report_1")
        'FORMATTING REPORT
        Try
            With SPAS.Dgv_Rapportage_Overzicht
                .Columns(0).Visible = False
                .Columns(1).HeaderText = "Categorie"
                .Columns(2).HeaderText = "Saldo 1/1"
                .Columns(3).HeaderText = "Automatische Incasso"
                .Columns(4).HeaderText = "Overige Banktrans."
                .Columns(5).HeaderText = "Interne boekingen"
                .Columns(6).HeaderText = "Uitkering"
                .Columns(7).HeaderText = "Saldo 31/12"
                .Columns(1).Width = 230

                For k = 2 To 7
                    .Columns(k).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    .Columns(k).Width = 120
                    .Columns(k).DefaultCellStyle.Format = "N2"
                    .Columns(k).ReadOnly = True
                Next
                Dim spc As Integer
                For r = 0 To .Rows.Count - 1
                    spc = .Rows(r).Cells(0).Value
                    If spc = 0 Or spc = 2 Or spc = 4 Or spc = 6 Or spc = 8 Or spc = 10 Or spc = 12 Then
                        .Rows(r).DefaultCellStyle.ForeColor = Color.Blue
                        .Rows(r).DefaultCellStyle.Font = New Font("Calibri", 12, FontStyle.Bold)
                        If spc = 12 Then
                            .Rows(r).DefaultCellStyle.BackColor = IIf(btable = "bank_archive", Color.LightGray, Color.Yellow)

                        End If
                        If spc = 0 Or spc = 4 Or spc = 8 Then
                            .Rows(r).DefaultCellStyle.BackColor = IIf(btable = "bank_archive", Color.Gainsboro, Color.PaleGreen)
                        End If

                    End If
                Next r

            End With
        Catch ex As Exception

        End Try
    End Sub

    Sub Drill_down_Report_overview(ByVal i As Integer, ByVal j As Integer)


        Dim source As String = ""
        Dim accgroup As String

        Select Case j
            Case 2 : source = "Closing"
            Case 3 : source = "Incasso"
            Case 4 : source = "Bank"
            Case 5 : source = "Intern"
            Case 6 : source = "Uitkering"
            Case Else
                Exit Sub
        End Select
        Dim bedrag As Integer = SPAS.Dgv_Rapportage_Overzicht.CurrentCell.Value

        accgroup = SPAS.Dgv_Rapportage_Overzicht.Rows(i).Cells(1).Value
        'MsgBox(accgroup & " " & source & " " & bedrag)

        Dim sql As String = "select j.date, a.name,j.amt1,j.name, j.type, j.description, j.iban,  ag.name,  j.fk_bank, j.id 
                             from " & Report_table(report_year) & " j left join account a on a.id = j.fk_account  left join accgroup ag on ag.id = a.fk_accgroup_id
                             where extract(year from j.date)=" & report_year & "and j.source='" & source & "' and ag.name='" & accgroup & "' and j.status != 'Open' order by j.date desc"

        SPAS.ToClipboard(sql, True)

        Load_Datagridview(SPAS.Dgv_Report_6, sql, "boekingen")
        Format_drill_down()
        SPAS.TC_Rapportage.SelectedTab = SPAS.TC_Boekingen

    End Sub

    Sub Drill_down_Bank_overview(ByVal i As Integer, ByVal j As Integer)


        Dim sqlpart1 As String = ""

        Select Case j
            Case 3 : sqlpart1 = " and b.name='_startsaldo_'"
            Case 4, 5, 6
            Case 7 : sqlpart1 = " and iban2 in (select accountno from bankacc)"
            Case Else
                Exit Sub
        End Select

        Dim bedrag As Integer = SPAS.Dgv_Rapportage_Overzicht.CurrentCell.Value
        Dim sql As String = "select b.date, b.seqorder, b.name, b.credit, b.debit, b.code, b.description, fk_journal_name
                             from " & Bank_table(report_year) & " b 
                             where iban='" & Trim(SPAS.Dgv_Rapportage_Overzicht.Rows(i).Cells(1).Value) & "' and extract(year from b.date)= " & report_year & sqlpart1 &
                             " order by b.seqorder desc"

        SPAS.ToClipboard(sql, True)

        Load_Datagridview(SPAS.Dgv_Report_6, sql, "drilldown banktransacties")
        Format_drill_down_bank()
        SPAS.TC_Rapportage.SelectedTab = SPAS.TC_Boekingen

    End Sub




    Sub Report_Bank_overview()

        Dim yearcheck = " And b2.iban = ba.accountno And extract(year from Date)=" & report_year


        Dim sql2 As String = "
        select iban As Rekeningnummer, ba.name As Rekeningnaam, null As Startsaldo, sum(credit) As Bij,sum(debit) As Af, sum(credit-debit) As Mutatiesaldo, null As Banksaldo
        from " & Bank_table(report_year) & " b left join bankacc ba on b.iban=ba.accountno
        where extract (year from date)=" & report_year & " and b.name != '_startsaldo_'
        group by iban, ba.name
        union select 'Totalen banksaldi', null, null, sum(credit),sum(debit), sum(credit-debit), null
        from " & Bank_table(report_year) & " b left join bankacc ba on b.iban=ba.accountno
        where extract (year from date)=" & report_year & " and b.name != '_startsaldo_'

"
        Dim sql3 = "
        select iban, sum(credit-debit) from " & Bank_table(report_year) & " b 
        where extract (year from date)=" & report_year & " and b.name = '_startsaldo_'
        group by iban
        union select 'Totalen', sum(credit-debit)
        from " & Bank_table(report_year) & " b 
        where extract (year from date)=" & report_year & " and b.name = '_startsaldo_'
"

        SPAS.ToClipboard(sql2, True)
        Collect_data(sql3)
        Dim farray() As String = {"T200", "T250", "N100", "N100", "N100", "N100", "N100"}
        Load_Datagridview(SPAS.Dgv_Rapportage_Overzicht, sql2, "rapportagefout Report_Bank_overview")
        SPAS.Format_Datagridview(SPAS.Dgv_Rapportage_Overzicht, farray)

        With SPAS.Dgv_Rapportage_Overzicht
            For r = 0 To .Rows.Count - 1

                'Debug.Print(dst.Tables(0).Rows(r)(1))
                .Rows(r).Cells(2).Value = dst.Tables(0).Rows(r)(1)
                .Rows(r).Cells(6).Value = .Rows(r).Cells(5).Value + dst.Tables(0).Rows(r)(1)
            Next r
        End With
    End Sub


    Sub Report_Closing()

        Dim saldo_date_start As String = "'Startsaldo 1-1-" & report_year + 1 & "' as name,'" & report_year + 1 & "-01-01' as date,'Verwerkt',"
        Dim tables As String = "journal j left join account a on a.id=j.fk_account left join accgroup g on g.id= a.fk_accgroup_id "
        Dim sql2 As String = ""
        Dim sql3 = "INSERT INTO journal(name, date, status, amt1, description, source, fk_account, type) VALUES" & vbCrLf
        Dim sql1 As String
        sql1 = "
    drop table if exists postings;
    select a.type as accounttype, " & saldo_date_start & " 
    'Startsaldo '||a.name as overgangspost, 'Closing' as source, fk_account As Accountnr, j.type as Journaaltype, sum(amt1) as Bedrag, null As Verrekening, null As Eindtotaal 
    into temp table postings
    from " & tables & "
    where a.type = 'Specifiek (doel)' and extract (year from date)=2023 --g.type = 'Inkomsten' and j.status != 'Open' and
    group by a.type, a.name, j.fk_account, j.type
    having sum(amt1) !=0::money
    --order by a.type
    ------------------------------------------------------------------------------------------------------------
    -- 2 haal de totaal van de kosten op

    union ------------------uitgaven
    select 'Uitgaven'," & saldo_date_start & "
    'Totaal uitgaven', 'Closing',null, null,
    (select sum(amt1) from  " & tables & " where extract (year from date)=2023 and g.type = 'Uitgaven' and j.status != 'Open'), 
    null As Verrekening, null As Eindtotaal 

    union ----------------------------overhead
    select 'Generiek (overhead)', 'Startsaldo 1-1-2024', '2024-01-01','Verwerkt',
    'Overhead', 'Closing',null, null,
     (select sum(amt1) from  " & tables & " where a.type = 'Anders' and g.type != 'Uitgaven' and g.name = 'Overhead'),
     null As Verrekening, null As Eindtotaal 


    union----- fondsen--------------------------
    select a.type as accounttype, " & saldo_date_start & " 
    'Startsaldo '||a.name as overgangspost, 'Closing' as source, fk_account, null,sum(amt1) as Bedrag, null As Verrekening, null As Eindtotaal 
    from " & tables & "
    where  extract (year from date)=2023 and a.type = 'Generiek (fonds)' and j.status != 'Open' and g.type = 'Inkomsten'
    group by a.type, a.name, j.fk_account
    having sum(amt1) !='0'::money

    union ----------------------------transit

    select 'Transit', " & saldo_date_start & "
    'Startsaldo '||a.name as overgangspost, 'Closing' as source, fk_account, null,sum(amt1) as Bedrag, null As Verrekening, null As Eindtotaal 
    from " & tables & "
    where extract (year from date)=2023  and j.status != 'Open' and g.type = 'Transit'
    group by a.type, a.name, j.fk_account
    having sum(amt1) !='0'::money
    order by accounttype;


    select * from postings
"

        'RunSQL(sql1, "NULL", "Report_Closing")
        SPAS.ToClipboard(sql1, True)
        Load_Datagridview(SPAS.Dgv_Report_Year_Closing, sql1, "Report_Closing")
        For r = 0 To SPAS.Dgv_Report_Year_Closing.Rows.Count - 1
            If SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(1).Value = "Transitoria" Then
            End If

        Next r




        Dim amt = 0.00
        Dim af As Integer = 0
        For r = 0 To SPAS.Dgv_Report_Year_Closing.Rows.Count - 1
            'amt = Replace(SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(3).Value, ".", "")
            'amt = Replace(amt, ",", ".")
            If InStr(SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(5).Value, "Startsaldo Algemeen,Fonds") Then af = r

            If InStr(SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(0).Value, "v") > 0 Then 'oVerhead, uitgaVen
                SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(9).Value = Format(SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(8).Value, "N2")
                SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(10).Value = 0
                amt += SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(9).Value
            Else
                SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(9).Value = 0
                SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(10).Value = Format(SPAS.Dgv_Report_Year_Closing.Rows(r).Cells(8).Value, "N2")
            End If
        Next
        SPAS.Dgv_Report_Year_Closing.Rows(af).Cells(9).Value = amt
        SPAS.Dgv_Report_Year_Closing.Rows(af).Cells(10).Value = Format(SPAS.Dgv_Report_Year_Closing.Rows(af).Cells(10).Value + amt, "N2")
        SPAS.Lbl_Report_total.Text = SPAS.Dgv_Report_Year_Closing.Rows(af).Cells(10).Value

        SPAS.Format_Datagridview(SPAS.Dgv_Report_Year_Closing, {"T135", "H000", "H000", "H000", "T300", "H000", "T085", "T080", "N085", "N085", "N085"})
        SPAS.Dgv_Report_Year_Closing.Columns(10).DefaultCellStyle.ForeColor = Color.DarkGreen

        If SPAS.Dgv_Report_Year_Closing.Rows(af).Cells(10).Value < 0 Then
            SPAS.Dgv_Report_Year_Closing.Rows(af).DefaultCellStyle.ForeColor = Color.Red
            MsgBox("Het algemeen fonds bevat onvoldoende middelen om te overhead en uitgaven te verdisconteren, vul deze s.v.p. aan.")

        End If


    End Sub

    Sub Format_drill_down()
        'select j.date, a.name,j.amt1,j.name, j.type, j.description, j.iban,  ag.name,  j.fk_bank 
        Try
            With SPAS.Dgv_Report_6

                .Columns(0).HeaderText = "Datum"
                .Columns(0).Width = 80
                .Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(1).HeaderText = "Account"
                .Columns(1).Width = 140
                .Columns(2).HeaderText = "Bedrag"
                .Columns(2).Width = 70
                .Columns(2).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(2).DefaultCellStyle.Format = "N2"
                .Columns(2).ReadOnly = True
                .Columns(3).HeaderText = "Naam transactie"
                .Columns(3).Width = 150
                .Columns(4).HeaderText = "Type"
                .Columns(4).Visible = False
                .Columns(5).HeaderText = "Omschrijving transactie"
                .Columns(5).Width = 400
                .Columns(6).HeaderText = "IBAN"
                .Columns(6).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(6).Visible = False
                .Columns(7).HeaderText = "Accountgroep"
                .Columns(7).Width = 125
                .Columns(8).Visible = False
                .Columns(9).Visible = False

            End With
        Catch ex As Exception
        End Try
    End Sub

    Sub Format_drill_down_bank()
        'select j.date, a.name,j.amt1,j.name, j.type, j.description, j.iban,  ag.name,  j.fk_bank 
        Try
            With SPAS.Dgv_Report_6

                .Columns(0).HeaderText = "Datum"
                .Columns(0).Width = 80
                .Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Columns(1).HeaderText = "Afschrift"
                .Columns(1).Width = 50
                .Columns(2).HeaderText = "Naam"
                .Columns(2).Width = 140
                .Columns(3).HeaderText = "Bij"
                .Columns(4).HeaderText = "Bij"
                For k = 3 To 4
                    .Columns(k).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    .Columns(k).DefaultCellStyle.Format = "N2"
                    .Columns(k).ReadOnly = True
                    .Columns(k).Width = 70
                Next k
                .Columns(5).HeaderText = "code"
                .Columns(5).Width = 40
                .Columns(5).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                .Columns(5).DefaultCellStyle.ForeColor = Color.Gray
                .Columns(6).HeaderText = "Omschrijving"
                .Columns(6).Width = 500
                .Columns(7).Visible = False


            End With
        Catch ex As Exception
        End Try
    End Sub

End Module
