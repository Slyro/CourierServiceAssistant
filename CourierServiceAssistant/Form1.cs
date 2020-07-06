﻿using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CourierServiceAssistant
{
    public partial class Form1 : Form
    {
        public List<Parcel> ExcelReportMailList;
        private string dbFileName;
        private readonly StringBuilder sb;
        private DateTime ReportDate;
        private List<Parcel> AllMailList;
        private DBManager Manager;
        private GoneMail GoneMail;
        private NewMail NewMail;
        private readonly UKD Ukd;
        private readonly List<string> ListOfImportedTracks;
        private readonly List<string> GoneList;
        private readonly List<string> NewList;
        private Run CurrentRun;
        private readonly string reg1 = "^[a-zA-Z]{2}[0-9]{9}[a-zA-Z]{2}$";
        private readonly string reg2 = "^[0-9]{14}$";

        private readonly Regex match;
        private readonly Regex match2;
        private DBAction DB;
        List<Report> reports;

        public Form1()
        {
            Ukd = new UKD();
            InitializeComponent();
            Load += Form1_Load;
            ExcelReportMailList = new List<Parcel>();
            ListOfImportedTracks = new List<string>();
            sb = new StringBuilder();
            GoneList = new List<string>();
            NewList = new List<string>();
            CurrentRun = new Run();
            button2.Text = "Внести изменения в Базу Данных";
            match = new Regex(reg1);
            match2 = new Regex(reg2);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            dbFileName = "Mail.db";
            Manager = new DBManager(dbFileName);
            DB = new DBAction(Manager);

            historyLabel.ResetText();

            AllMailList = DB.GetAllParcelFromDataBase(); //Получение Базового списка всех РПО


            dayOneDatePicker.Value = new DateTime(2020, 6, 3);
            dayTwoDatePicker.Value = new DateTime(2020, 6, 4);

            Dictionary<string, string> NameRoutePairs = DB.GetNameRoutePairs();
            foreach (string key in NameRoutePairs.Keys)
            {
                Ukd.AddCourier(key, NameRoutePairs[key]);
            }//Заполнение экземпляра класса UKD списком курьеров и районов, полок.

            #region Двойная буферизация для DataGridView Инвентаризации.

            var dgvType = routeDataGrid.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(routeDataGrid, true, null);

            #endregion Двойная буферизация для DataGridView Инвентаризации.

            //GetStorageReportByDay(rackDateTimePicker.Value.Date);//Выгрузка информации о пикнут отправлениях на складе на основе даты.

            //DoReport();

            RefreshRouteBox();
            RefreshReportsDate();
            RefreshCourierList();
            button2.Enabled = false;
        }

        private void GetStorageReportByDay(DateTime date)
        {
            //TODO: Создать класс порождения готовых экземпляров класса UKD в любом кол-ве, исходя из выбраного диапазона дат.
            Ukd.AddRacks(DB.GetRacksByDate(date)); //Выгрузка полочек

            Ukd.Runs = DB.GetRunsByDate(date); //Выгрузка Рейсов

            UpdateStatistic();
            totalMailLabel.Text = "На балансе УКД: " + AllMailList.Count;
        }//Заполнение экземпляра класса UKD информацией об отправлениях лежащих на полках курьеров, операторов, склад самовывоза и взятых в доставку РПО за выбраный день.
        private void UpdateStatistic()
        {
            //TODO: Выводить больше информации о складе, включая статистические данные об изменении кол-ва посылок на полках.
            statisticPanel1.Controls.Clear();
            statisticPanel2.Controls.Clear();
            foreach (var rack in Ukd.GetAllRacks)
            {
                Label label = new Label
                {
                    Text = $"{rack.Route}: {rack.TrackList.Count}",
                    Size = new Size(200, 20)
                };
                statisticPanel1.Controls.Add(label);
            }

            foreach (var run in Ukd.Runs)
            {
                Label label = new Label
                {
                    Text = $"{run.Courier}: {run.TracksInRun.Count}",
                    Size = new Size(200, 20)
                };
                statisticPanel2.Controls.Add(label);
            }

            label3.Text = "РПО на складе: " + Ukd.TrackList.Count; //почты инвентаризированно.
            label11.Text = "Всего: " + Ukd.GetCountTracksInRuns;
        }//Заполнение области "Статистика" информацией о колличестве посылок на "районах" в т.ч. окно, сортировчный цех.

        private void DoReport()
        {
            // 0, 2, 3, 5, 6, 7
            flowLayoutPanel1.Controls.Clear();
            reportLabelBase.Text += AllMailList.Count;
            reportLabelGone.Text += DB.GetGoneParcelFromDataBase().Select(x => x.TrackID).ToList().Count;

            reports = new List<Report>();
            Report.GoneByReport = DB.GetGoneParcelFromDataBase().Select(x => x.TrackID).ToList();
            Report.CurrentList = AllMailList.Select(x => x.TrackID).ToList();

            foreach (var route in Ukd.GetAllRoutes)
            {
                reports.Add(new Report(DB.GetRacksPerDayByRoute(route), DB.GetRunsPerDayByRoute(route)));
            }

            reports.RemoveAll((x) => string.IsNullOrEmpty(x.Route));

            List<GroupBox> groupBoxes = new List<GroupBox>();

            var clickableLabelFont = new Font("Century Gothic", 11, FontStyle.Underline);
            for (int i = 0; i < reports.Count; i++)
            {
                Report report = reports[i];
                groupBoxes.Add(new GroupBox() { Text = report.Route, AutoSize = true });
                groupBoxes[i].Controls.Add(new FlowLayoutPanel() { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill });
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"Посылок всего: {report.AllTracksOnRacks.Count} ({report.UniqueTracksRack.Count})", AutoSize = true }); //0
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Text = $"Среднее кол-во: {report.AvarageAllRack} ({report.AvarageUniqueRack})", AutoSize = true });
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"Убрано отчётом: {report.DeliveredTracksRack.Count}", AutoSize = true });//2
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"Остаток: {report.MustBeOnRack.Count}", AutoSize = true });//3
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Text = $"", AutoSize = true });
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"На доставку: {report.AllTracksOnRuns.Count} ({report.UniqueTracksRun.Count})", AutoSize = true });//5
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"Вручено: {report.DeliveredTracksRun.Count}", AutoSize = true });//6
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"Разбег: {report.DifferenceTracksRun.Count}", AutoSize = true });//7
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Cursor = Cursors.Hand, Font = clickableLabelFont, Text = $"На проверку: {report.NotDeliveredTracksRun.Count}", AutoSize = true });//8
                groupBoxes[i].Controls[0].Controls.Add(new Label() { Text = $"Средн. на доставку: {report.AvarageAllRun}", AutoSize = true });

                for (int j = 0; j < groupBoxes[i].Controls[0].Controls.Count; j++)
                {
                    groupBoxes[i].Controls[0].Controls[j].Click += reportclick;
                }

                flowLayoutPanel1.Controls.Add(groupBoxes[i]);
            }
        }

        private void SevenDays()
        {
            dayOneFlowPanel.Controls.Clear();
            dayTwoFlowPanel.Controls.Clear();

            UKD[] aDay = new UKD[2];
            aDay[0] = GetUKD(dayOneDatePicker.Value.Date);
            aDay[1] = GetUKD(dayTwoDatePicker.Value.Date);
            UKD One = aDay[0];
            UKD Two = aDay[1];

            UKD GetUKD(DateTime date)
            {
                UKD _ukd = new UKD { Date = date };
                Dictionary<string, string> NameRoutePairs = Ukd.GetCourierRouteDictionary();
                foreach (string key in NameRoutePairs.Keys)
                {
                    _ukd.AddCourier(key, NameRoutePairs[key]);
                }
                _ukd.AddRacks(DB.GetRacksByDate(date));
                _ukd.Runs = DB.GetRunsByDate(date);
                return _ukd;
            }


            for (int i = 0; i < One.GetAllRacks.Count; i++)
            {
                if (i == 0)
                {
                    dayOneFlowPanel.Controls.Add(new Label() { Text = dayOneDatePicker.Value.Date.ToShortDateString() });
                }
                Rack item = One.GetAllRacks[i];
                Label label = new Label() { Font = new Font("Century Gothic", 11), Padding = new Padding(-10) };
                label.Text = item.Route + ": " + item.TrackList.Count;
                label.AutoSize = true;
                dayOneFlowPanel.Controls.Add(label);
            }
            int? alpha, beta = 0;
            for (int i = 0; i < Two.GetAllRacks.Count; i++)
            {
                bool badLogic = false;
                if (dayOneDatePicker.Value > dayTwoDatePicker.Value)
                {
                    badLogic = true;
                }
                if (i == 0)
                {
                    dayTwoFlowPanel.Controls.Add(new Label() { Text = dayTwoDatePicker.Value.Date.ToShortDateString() });
                    if (badLogic)
                    {
                        dayTwoFlowPanel.Controls.Add(new Label() { Text = "Нарушена логика расчета.", AutoSize = true });
                        dayTwoFlowPanel.AutoSize = true;
                    }
                }
                Rack item = Two.GetAllRacks[i];
                Label label = new Label() { Font = new Font("Century Gothic", 11), Padding = new Padding(-10) };
                label.AutoSize = true;
                alpha = item.TrackList.Count - One.GetRackByRoute(item.Route)?.TrackList.Count;
                beta += alpha;
                if (badLogic)
                {

                }
                else
                {
                    label.Text = $"{item.Route}: {item.TrackList.Count} ({alpha})";
                }
                dayTwoFlowPanel.Controls.Add(label);
            }

            if (beta > 0)
            {
                totalFlowPanel.Controls.Add(new Label() { Text = $"Больше на {beta}", AutoSize = true });
            }
            else
            {
                totalFlowPanel.Controls.Add(new Label() { Text = $"Меньше на {-beta}", AutoSize = true });
            }



            //for (int i = 0; i < aDay.Length - 1; i++)
            //{
            //    UKD dOne, dTwo;

            //    dOne = aDay[i];
            //    dTwo = aDay[i + 1];

            //    FlowLayoutPanel panel = new FlowLayoutPanel();
            //    dayOneFlowPanel.Controls.Add(panel);
            //    dayOneFlowPanel.AutoScroll = true;
            //    //panel.Dock = DockStyle.Fill;
            //    foreach (var todayRack in dTwo.GetAllRacks)
            //    {
            //        var route = todayRack.Route;

            //        var yesterdayRack = dOne.GetAllRacks.Find((x) => x.Route == todayRack.Route);
            //        if (yesterdayRack != null)
            //        {
            //            var lost = yesterdayRack.TrackList.Except(todayRack.TrackList);

            //            ListBox listOfGoneMail = new ListBox();
            //            listOfGoneMail.Items.Add(route);
            //            listOfGoneMail.Size = new Size(120,650);
            //            panel.AutoSize = true;
            //            foreach (var item in lost)
            //            {
            //                listOfGoneMail.Items.Add(item);
            //            }
            //            panel.Controls.Add(listOfGoneMail);
            //        }
            //    }
            //}
        }

        private void reportclick(object sender, EventArgs e)
        {
            var groupBox = ((Label)sender).Parent.Parent;
            var mainContainer = groupBox.Parent;
            int reportIndex = mainContainer.Controls.GetChildIndex(groupBox);
            int labelIndex = groupBox.Controls[0].Controls.GetChildIndex((Label)sender);

            switch (labelIndex)
            {
                case 0:
                    dataGridView1.DataSource = reports[reportIndex].UniqueTracksRack.ConvertAll(x => new { Value = x });
                    break;
                case 2:
                    dataGridView1.DataSource = reports[reportIndex].DeliveredTracksRack.ConvertAll(x => new { Value = x });
                    break;
                case 3:
                    dataGridView1.DataSource = reports[reportIndex].MustBeOnRack.ConvertAll(x => new { Value = x });
                    break;
                case 5:
                    dataGridView1.DataSource = reports[reportIndex].UniqueTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 6:
                    dataGridView1.DataSource = reports[reportIndex].DeliveredTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 7:
                    dataGridView1.DataSource = reports[reportIndex].DifferenceTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 8:
                    dataGridView1.DataSource = reports[reportIndex].NotDeliveredTracksRun.ConvertAll(x => new { Value = x });
                    break;
                default:
                    dataGridView1.DataSource = null;
                    break;
            }
        }



        #region ExcelExportTabPage

        private List<Parcel> BalanceParseFromExcelFile(string filepath)
        {
            List<Parcel> list = new List<Parcel>();
            using (FileStream stream = File.Open(filepath, FileMode.Open, FileAccess.Read))
            {
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        if (reader.Name == "Исходные данные")
                        {
                            reader.Read();
                            ReportDate = Convert.ToDateTime(reader.GetValue(1));
                            reader.Read();

                            while (reader.Read())
                            {
                                if ((int)reader.GetDouble((int)FarEast.Index) == 690880)
                                {
                                    if (reader.GetValue((int)FarEast.TrackID) is null)
                                    {
                                        continue;
                                    }
                                    list.Add(ExcelReader.GetParcel(reader));
                                }
                            }
                            break;
                        }
                    } while (reader.NextResult());
                    reader.Dispose();
                }
                stream.Dispose();
            }
            return list;
        } //Парсер файла остатков

        private void button1_Click(object sender, EventArgs e)
        {
            /*
            * Загрузка данных из файла остатков.
            * Составление запроса для иморта данных с помощью StrinBuilder sb
            * Запись истории выполнения в historyLabel
            */

            sb.Clear();
            GoneList.Clear();
            NewList.Clear();

            historyLabel.ResetText();
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    ExcelReportMailList = BalanceParseFromExcelFile(ofd.FileName);
                    historyLabel.Text += $"Список загружен.\r\nДата отчета: {ReportDate}\r\nВ отчете записей: {ExcelReportMailList.Count}\r\n";


                    AllMailList = DB.GetAllParcelFromDataBase();
                    historyLabel.Text += "В базе записей: " + AllMailList.Count + "\r\n";

                    GoneMail = new GoneMail(AllMailList.Except(ExcelReportMailList).ToList());
                    NewMail = new NewMail(ExcelReportMailList.Except(AllMailList).ToList());

                    historyLabel.Text += $"Ушло почты: {GoneMail.Count}\r\n";
                    historyLabel.Text += $"Поступило почты: {NewMail.Count}\r\n";

                    if (GoneMail.Count > 0)
                    {
                        GoneMail.Parcels.ForEach((x) =>
                        {
                            if (x.PlannedDate != DateTime.MinValue)
                            {
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                        });
                    }
                    if (NewMail.Count > 0)
                    {
                        NewMail.Parcels.ForEach((x) =>
                        {
                            if (x.PlannedDate != DateTime.MinValue)
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                        });
                    }
                }
                button2.Enabled = true;
            }
        } //Выбор файла остатков и загрузка данных из него.

        private async void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"В базе данных произойдут следующие изменения:{Environment.NewLine}{Environment.NewLine}" +
                                $"Parcels -> Добавлено строк - {NewMail.Count}, Удалено строк - {GoneMail.Count}{Environment.NewLine}" +
                                $"Delivered -> Добавлено строк - {GoneMail.Count}", "Изменение БД", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                await Task.Run(() => //Асинхронноая запись в БД информации полученной из файла "ОСТАТКИ"
                {
                    using (var reader = Manager.ExecuteReader("SELECT ReportDate FROM DateReports")) //Проверка на попытку записи в БД уже загруженного отчета.
                    {
                        if (reader.HasRows)
                            while (reader.Read())
                            {
                                DateTime date = Convert.ToDateTime(reader.GetString(0));
                                if (date >= ReportDate.Date)
                                {
                                    MessageBox.Show("Отчет на текущую или более позднюю дату загрузить нельзя!");
                                    if (InvokeRequired)
                                    {
                                        Invoke(new Action(() =>
                                        {
                                            button2.Enabled = false;
                                        }
                                            ));
                                    }
                                    return;
                                }
                            }
                    }

                    if (NewList.Count > 0)
                        Manager.TransactionInsertToParcel(NewList); // ЗАПИСЬ ДАННЫХ В БД [Parcels] О НОВОЙ ПОЧТЕ
                    if (GoneList.Count > 0)
                    {
                        Manager.TransactionInsertToDelivered(GoneList); // ЗАПИСЬ ДАННЫХ В БД [Delivered] О ПОЧТЕ ПРОПАВШЕЙ ИЗ ОТЧЕТА ОБ ОСТАТКАХ
                        Manager.TransactionDeleteFromParcel(GoneMail.GetList());
                    }

                    Invoke(new Action(() =>
                    {
                        debugTextBox.Text = $"Добавлено - {NewList.Count}";
                        AllMailList = DB.GetAllParcelFromDataBase();
                        historyLabel.Text += "В базе записей: " + AllMailList.Count + "\r\n";
                    }
                    ));

                    SQLiteCommand dbCommand = new SQLiteCommand
                    {
                        CommandText = "INSERT INTO [DateReports] ([ImportDate], [ReportDate]) VALUES (@import, @report)" // Запись БД информации о дате в загружаемом отчете.
                    };
                    dbCommand.Parameters.AddWithValue("@import", DateTime.Now.ToShortDateString());
                    dbCommand.Parameters.AddWithValue("@report", ReportDate.ToShortDateString());
                    Manager.ExecuteNonQuery(dbCommand);
                });

            RefreshReportsDate(); //Обновление информации в GUI о датах внесенных данных.
        } //Запись информации в базу данных.

        private void button3_Click(object sender, EventArgs e)
        {
            Manager.ExecuteNonQuery("DELETE FROM Parcels");
            using (var reader = Manager.ExecuteReader("Select * FROM Parcels"))
            {
                if (!reader.HasRows)
                {
                    MessageBox.Show("Очищено");
                }
            }
            using (var reader = Manager.ExecuteReader("SELECT TrackNumber FROM Parcels"))
            {
                AllMailList = new List<Parcel>();
                while (reader.Read())
                {
                    AllMailList.Add(new Parcel());
                }
                historyLabel.Text = "В базе найдено записей: " + AllMailList.Count + "\r\n";
            }
        } //Удаление всех записей об РПО.

        private void listBox2_MouseDoubleClick(object sender, MouseEventArgs e) //Удаление записи из БД о внесении отчета за выбранную дату.
        {
            if (MessageBox.Show("Подтведите удаление информации о внесении данных.", "Удаление", MessageBoxButtons.OKCancel, MessageBoxIcon.Stop) == DialogResult.OK)
                Manager.ExecuteNonQuery($"DELETE FROM DateReports WHERE ReportDate=('{dateOfImportingReportsListBox.SelectedItem}');");
            RefreshReportsDate();
        }

        private void RefreshReportsDate() //Обновить контрол с датами загруженных отчетов.
        {
            dateOfImportingReportsListBox.Items.Clear();
            List<object> order = new List<object>();
            using (var reader = Manager.ExecuteReader($"Select reportdate FROM Datereports"))
            {
                while (reader.Read())
                {
                    order.Add(reader.GetString(0));

                }
            }
            for (int i = order.Count - 1; i >= 0; i--)
            {
                dateOfImportingReportsListBox.Items.Add(order[i]);
            }
        }

        #endregion ExcelExportTabPage

        #region SettingTabPage

        private List<object> GetCourierListFromDataBase()
        {//TODO: В DBAction перенести нужно.
            List<object> list = new List<object>();
            using (var reader = Manager.ExecuteReader($"SELECT fullName, route FROM Courier"))
            {
                while (reader.Read())
                {
                    list.Add(reader.GetString(0));
                }
            }
            return list;
        } //Достать список курьеров из БД

        private void importComboBox_Enter(object sender, EventArgs e)
        {
            (sender as ComboBox).Items.Clear();
            (sender as ComboBox).Items.AddRange(GetCourierListFromDataBase().ToArray());
        } // Выгрузка списка курьеров в контролы на вкладках рейса и настройки

        private void addRouteTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (AddRoute(addRouteTextBox.Text))
                {
                    RefreshRouteBox();
                    addRouteTextBox.ResetText();
                }
            }
        }

        private void addRouteButton_Click(object sender, EventArgs e)
        {
            if (AddRoute(addRouteTextBox.Text))
            {
                RefreshRouteBox();
                addRouteTextBox.ResetText();
            }
        }

        private void deleteRouteButton_Click(object sender, EventArgs e)
        {
            if (routBox.SelectedIndex != -1)
            {
                Manager.ExecuteNonQuery($"DELETE FROM Route WHERE route_name='{routBox.SelectedItem}'");
                RefreshRouteBox();
            }
        }

        private void courierRouteComboBox_Enter(object sender, EventArgs e)
        {
            courierRouteComboBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT route_name FROM Route"))
            {
                while (reader.Read())
                {
                    courierRouteComboBox.Items.Add(reader.GetString(0));
                }
            }
        }

        private void courierAddButton_Click(object sender, EventArgs e)
        {
            AddCourier(courierNameTextBox.Text, courierRouteComboBox.Text);
            RefreshCourierList();
        }

        private void courierDeleteButton_Click(object sender, EventArgs e)
        {
            //TODO: Удаление курьера из Базы Данных.
            throw new NotImplementedException();
        }

        private bool AddCourier(string name, string route)
        {
            if (name.Length > 0)
            {
                using (var reader = Manager.ExecuteReader($"SELECT fullName FROM Courier WHERE fullName='{name}'"))
                {
                    if (reader.HasRows)
                    {
                        MessageBox.Show("Такой курьер уже есть!");
                        return false;
                    }
                }
                Manager.ExecuteNonQuery($"INSERT INTO Courier ([fullName],[route]) VALUES ('{name}','{route}')");
                return true;
            }
            return false;
        }//Запись в БД. Добавление нового курьера на маршрут.

        private void RefreshCourierList()
        {
            courierListBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT fullName, route FROM Courier"))
            {
                while (reader.Read())
                {
                    courierListBox.Items.Add(reader.GetString(0) + " - " + reader.GetString(1));
                }
            }
        }

        private void RefreshRouteBox()
        {
            routBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT route_name FROM Route"))
            {
                while (reader.Read())
                {
                    routBox.Items.Add(reader.GetString(0));
                }
            }
        }

        private bool AddRoute(string routeName)
        {
            if (routeName.Length > 0)
            {
                using (SQLiteDataReader reader = Manager.ExecuteReader($"SELECT route_name FROM Route WHERE route_name='{routeName}'"))
                {
                    if (reader.HasRows)
                    {
                        MessageBox.Show("Такой маршрут уже есть!");
                        return false;
                    }
                }
                Manager.ExecuteNonQuery($"INSERT INTO Route (route_name) VALUES ('{routeName}')");
                return true;
            }
            return false;
        } //Запись в БД. Добавление нового маршрута.

        #endregion SettingTabPage

        private void routeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //TODO: Необходим полный реворк
            if (e.KeyCode == Keys.Enter)
            {
                var track = routeTextBox.Text.ToUpper();
                if (RouteComboBox.SelectedIndex != -1)
                {
                    if (match.IsMatch(track) || match2.IsMatch(track))
                    {
                        label7.ResetText();
                        if (CurrentRun.TracksInRun.Contains(track) || Ukd.GetAllTracksInRuns.Contains(track))
                        {
                            label7.Text = "Повторный ШПИ";
                            routeTextBox.ResetText();
                            return;
                        }

                        routeDataGrid.Rows.Add(track);
                        CurrentRun.TracksInRun.Add(track);

                        var _rpo = AllMailList.Find((item) => item.TrackID == track)?.IsPayNeed;
                        switch (_rpo)
                        {
                            case true:
                                Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{track}', '{RouteComboBox.SelectedItem}', 0, '{routeDatePicker.Value.ToShortDateString()}');");
                                break;

                            case false:
                                Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{track}', '{RouteComboBox.SelectedItem}', 0, '{routeDatePicker.Value.ToShortDateString()}');");
                                break;

                            default:
                                Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{track}', '{RouteComboBox.SelectedItem}', 1, '{routeDatePicker.Value.ToShortDateString()}');");
                                break;
                        }

                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    else
                        label7.Text = "Некорректный номер!";
                }
                else
                    label7.Text = "Необходимо выбрать курьера!";
                routeTextBox.Clear();
            }
        }

        private void routeDatePicker_ValueChanged(object sender, EventArgs e)
        {

            GetStorageReportByDay(routeDatePicker.Value);

            //TODO: Необходим полный реворк
            countInRunLabel.ResetText();
            if (routeDatePicker.Value.Date != DateTime.Now.Date || RouteComboBox.SelectedIndex == -1)
            {
                //RouteComboBox.Enabled = false;
                routeTextBox.Enabled = true;
            }
            else
            {
                // RouteComboBox.Enabled = true;
                routeTextBox.Enabled = true;
            }

            Ukd.Runs.ForEach((x) => x.TracksInRun.Clear());

            Ukd.Runs = DB.GetRunsByDate(routeDatePicker.Value);


            var count = routeDataGrid.Rows.Count - 1;
            if (RouteComboBox.SelectedIndex != -1)
            {
                for (int i = 0; i < count; i++)
                {
                    routeDataGrid.Rows.Remove(routeDataGrid.Rows[0]);
                }
                CurrentRun = Ukd.Runs.Find((x) => x.Courier == RouteComboBox.SelectedItem.ToString());
                CurrentRun?.TracksInRun.ForEach((x) =>
                {
                    routeDataGrid.Rows.Add(x);
                });
            }
            countInRunLabel.Text = $"{routeDataGrid.Rows.Count - 1}";
            UpdateStatistic();
        }

        private void RouteComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TODO: Необходим полный реворк?
            if (Ukd.Runs.Find((x) => x.Courier == RouteComboBox.Text) is null)
                Ukd.Runs.Add(new Run() { Courier = RouteComboBox.Text, TracksInRun = new List<string>() });

            countInRunLabel.ResetText();
            if (routeDatePicker.Value.Date != DateTime.Now.Date || RouteComboBox.SelectedIndex == -1)
            {
                routeTextBox.Enabled = true;
            }
            else
            {
                routeTextBox.Enabled = true;
            }
            var count = routeDataGrid.Rows.Count - 1;
            for (int i = 0; i < count; i++)
            {
                routeDataGrid.Rows.Remove(routeDataGrid.Rows[0]);
            }
            CurrentRun = Ukd.Runs.Find((x) => x.Courier == RouteComboBox.SelectedItem?.ToString());
            CurrentRun?.TracksInRun.ForEach((x) =>
            {
                routeDataGrid.Rows.Add(x);
            });
            countInRunLabel.Text = $"{routeDataGrid.Rows.Count - 1}";
            UpdateStatistic();
        }

        private void routeDataGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var track = routeDataGrid.Rows[e.RowIndex].Cells[0].Value.ToString();
            var _rpo = AllMailList.Find((item) => item.TrackID == track)?.IsPayNeed;
            switch (_rpo)
            {
                case true:
                    routeDataGrid.Rows[e.RowIndex].Cells[0].Style.BackColor = Color.Yellow;
                    break;

                case false:
                    break;

                default:
                    routeDataGrid.Rows[e.RowIndex].Cells[0].Style.BackColor = Color.OrangeRed;
                    break;
            }
            countInRunLabel.Text = $"{routeDataGrid.Rows.Count - 1}";
        }


        private void rackRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Text = "Полки";
        }

        private void routeRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Text = "Маршруты";
        }
        private void importComboBox_SelectedIndexChanged(object sender, EventArgs e)//Предварительное создание пустой полки для выбранного курьера, если полка для него отсутствует.
        {
            if (Ukd.GetRackByCourier(RouteComboBox.Text) is null)
                Ukd.AddRack(RouteComboBox.Text, Ukd.GetRoute(RouteComboBox.Text), new List<string>(), routeDatePicker.Value);
        }
        private void deleteRunButton_Click(object sender, EventArgs e)
        {
            Manager.ExecuteNonQuery($"DELETE FROM Runs WHERE Courier=('{RouteComboBox.Text}') AND Date=('{routeDatePicker.Value.ToShortDateString()}')");
            RouteComboBox.SelectedIndex = -1;
        }

























        private void button5_Click_1(object sender, EventArgs e)
        {
            SevenDays();
        }

        #region DaysSelectors      
        private void subDayButton_Click(object sender, EventArgs e)
        {
            dayOneDatePicker.Value = dayOneDatePicker.Value.AddDays(-1);
            dayTwoDatePicker.Value = dayTwoDatePicker.Value.AddDays(-1);
        }

        private void addDayButton_Click(object sender, EventArgs e)
        {
            dayOneDatePicker.Value = dayOneDatePicker.Value.AddDays(1);
            dayTwoDatePicker.Value = dayTwoDatePicker.Value.AddDays(1);
        }

        private void dayOneSubDateButton_Click(object sender, EventArgs e)
        {
            dayOneDatePicker.Value = dayOneDatePicker.Value.AddDays(-1);
        }

        private void dayOneAddDayButton_Click(object sender, EventArgs e)
        {
            dayOneDatePicker.Value = dayOneDatePicker.Value.AddDays(1);
        }

        private void dayTwoSubDayButton_Click(object sender, EventArgs e)
        {
            dayTwoDatePicker.Value = dayTwoDatePicker.Value.AddDays(-1);
        }

        private void dayTwoAddDayButton_Click(object sender, EventArgs e)
        {
            dayTwoDatePicker.Value = dayTwoDatePicker.Value.AddDays(1);
        }
        #endregion
    }
}