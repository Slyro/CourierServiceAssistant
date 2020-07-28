using ExcelDataReader;
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
using CourierServiceAssistant.sklad;

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
        private readonly List<string> GoneList;
        private readonly List<string> NewList;

        private Run CurrentRun;
        private List<string> CurrentRackList;

        private readonly string reg1 = "^[a-zA-Z]{2}[0-9]{9}[a-zA-Z]{2}$";
        private readonly string reg2 = "^[0-9]{14}$";

        private readonly Regex match;
        private readonly Regex match2;
        private DBAction DB;
        List<Report> reports;
        MyTableLayoutPanel titlebox;

        public Form1()
        {
            Ukd = new UKD();
            InitializeComponent();
            Load += Form1_Load;
            ExcelReportMailList = new List<Parcel>();
            sb = new StringBuilder();
            GoneList = new List<string>();
            NewList = new List<string>();
            CurrentRun = new Run();
            CurrentRackList = new List<string>();
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

            Dictionary<string, string> NameRoutePairs = DB.GetNameRoutePairs();
            foreach (string key in NameRoutePairs.Keys)
            {
                Ukd.AddCourier(key, NameRoutePairs[key]);
            }//Заполнение экземпляра класса UKD списком курьеров и районов, полок.

            #region Двойная буферизация для DataGridView Инвентаризации.

            var dgvType = trackDataGrid.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(trackDataGrid, true, null);

            #endregion Двойная буферизация для DataGridView Инвентаризации.

            GetStorageReportByDay(dayDatePicker.Value.Date);//Выгрузка информации о пикнут отправлениях на складе на основе даты.

            RefreshRouteBox();
            RefreshReportsDate();
            RefreshCourierList();
            UpdateCourierNameComboboxData();

            button2.Enabled = false;
        } //Загрузка формы

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
            Ukd.Runs.ForEach((x) => x.TracksInRun.Clear());
            Ukd.Runs = DB.GetRunsByDate(dayDatePicker.Value);
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
                if (run.TracksInRun.Count == 0)
                    continue;

                Label label = new Label
                {
                    Text = $"{run.Courier}: {run.TracksInRun.Count}",
                    Size = new Size(200, 20)
                };
                statisticPanel2.Controls.Add(label);
            }

            label3.Text = "РПО на складе: " + Ukd.TrackListOnRacks.Count; //почты инвентаризированно.
            label11.Text = "Всего: " + Ukd.GetCountTracksInRuns;
        }//Заполнение области "Статистика" информацией о колличестве посылок на "районах" в т.ч. окно, сортировчный цех.

        private void DoReport()
        {
            // 0, 2, 3, 5, 6, 7

            if (titlebox != null)
            {
                titlebox.Dispose();
            }

            var _list = DB.GetGoneParcelFromDataBase().Select(x => x.TrackID).ToList();
            flowLayoutPanel1.Controls.Clear();

            reportLabelBase.Text = "Storage: " + AllMailList.Count;
            reportLabelGone.Text = "Delivered: " + _list.Count;

            reports = new List<Report>();

            Report.GoneByReport = _list;
            Report.CurrentList = AllMailList.Select(x => x.TrackID).ToList();
            Report.Approved = DB.SelectAllFromApproveTable();

            foreach (var route in Ukd.GetAllRoutes)
            {
                reports.Add(new Report(DB.GetRacksPerDayByRoute(route), DB.GetRunsPerDayByRoute(route)));
            }
            reports.RemoveAll((x) => string.IsNullOrEmpty(x.Route));

            string[] titles = new string[]
            {
                "Route",            //0
                "Rack total",       //1
                "Scannig avg.",     //2
                "Done by report",   //3
                "Must be on rack",  //4
                "No Delivery",      //5
                "Run total",        //6
                "Delivered",        //7
                "Check",            //8
                "Errors",           //9
                "By run avg."       //10
            };

            titlebox = new MyTableLayoutPanel()
            {
                Parent = flowLayoutPanel1,
                Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left),
                AutoSize = true,
                Dock = DockStyle.Fill,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            titlebox.ColumnCount = titles.Length;
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 7F });  //"Район",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Посылок за всё время",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Среднее кол-во",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Убрано отчётом",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 5F });  //"Остаток",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Не были в доставке",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"На доставку за всё время",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Вручено" ,
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 5F });  //"На проверку" ,
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 5F });  //"Ошибки",
            titlebox.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 6F });  //"Ср. доставка"


            titlebox.RowStyles.Add(new RowStyle() { SizeType = SizeType.Absolute, Height = 45F });
            for (int i = 0; i < titles.Length; i++)
            {
                var lbl = new Label() { Margin = new Padding(0), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Text = titles[i] };

                if (i == 0)
                    lbl.BackColor = Color.White;
                if (i > 0 && i < 4)
                    lbl.BackColor = Color.CornflowerBlue;
                if (i == 5)
                    lbl.BackColor = Color.Yellow;
                if (i > 5 && i < 8)
                    lbl.BackColor = Color.LightGreen;
                if (i == 4 || i == 9)
                    lbl.BackColor = Color.IndianRed;
                if (i == 8)
                    lbl.BackColor = Color.Yellow;

                titlebox.Controls.Add(lbl, i, 0);
            }


            var clickFont = new Font("Century Gothic", 12, FontStyle.Underline);

            Color[] colors = new Color[]
            {
                Color.Bisque,
                Color.MistyRose,
                Color.DarkKhaki,
                Color.Aquamarine,
                Color.CadetBlue,
                Color.Firebrick,
                Color.Chocolate,
                Color.Coral,
                Color.DarkGoldenrod,
                Color.DarkSalmon,
                Color.LightCoral,
                Color.DarkSlateBlue,
                Color.Gainsboro,
                Color.FloralWhite,
                Color.Snow
            };

            for (int row = 0; row < reports.Count; row++)
            {
                Report report = reports[row];
                Label[] labels;

                labels = new Label[]
                {
                    new Label() { BackColor = Color.FromArgb(245, 219, 118),Margin = new Padding(0), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                                                                                                                                             Text = report.Route },                                                          //"Район",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.AllTracksOnRacks.Count} ({report.UniqueTracksRack.Count})" },  //"Посылок за всё время",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,                                          Text = $"{report.AvarageAllRack} ({report.AvarageUniqueRack})" },               //"Среднее кол-во",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.DeliveredTracksRack.Count}" },//2                              //"Убрано отчётом",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.MustBeOnRack.Count}" },//3                                     //"Остаток",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.WithoutDelivery.Count}" },  //4                                //"Не были в доставке",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.AllTrackInRuns.Count} ({report.UniqueTracksRun.Count})" },//5  //"На доставку за всё время",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.DeliveredTracksRun.Count}" },//6                               //"Вручено" ,
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.DifferenceTracksRun.Count}" },//7                              //"На проверку" ,
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Font = clickFont, Text = $"{report.NotDeliveredTracksRun.Count}" },//8                            //"Ошибки",
                    new Label() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,                                          Text = $"{report.AvarageAllRun}" }                                              //"Ср. доставка"                                        
                };

                for (int x = 0; x < labels.Length; x++)
                {
                    labels[x].Click += reportclick;
                }

                titlebox.RowStyles.Add(new RowStyle() { SizeType = SizeType.Absolute, Height = 40F });
                for (int column = 0; column < labels.Length; column++)
                {
                    titlebox.Controls.Add(labels[column], column, row + 1);
                }

            }
            flowLayoutPanel1.Visible = false;
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.Controls.Add(titlebox);
            flowLayoutPanel1.ResumeLayout();
            flowLayoutPanel1.Visible = true;
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

        }

        private void reportclick(object sender, EventArgs e)
        {
            TableLayoutPanel tablePanel = (TableLayoutPanel)((Label)sender).Parent;
            int Y = tablePanel.GetCellPosition((Label)sender).Column;
            int X = tablePanel.GetCellPosition((Label)sender).Row - 1;
            switch (Y)
            {
                case 1:
                    dataGridView1.DataSource = reports[X].UniqueTracksRack.ConvertAll(x => new { Value = x });
                    break;
                case 3:
                    dataGridView1.DataSource = reports[X].DeliveredTracksRack.ConvertAll(x => new { Value = x });
                    break;
                case 4:
                    dataGridView1.DataSource = reports[X].MustBeOnRack.ConvertAll(x => new { Value = x });
                    break;
                case 5:
                    dataGridView1.DataSource = reports[X].WithoutDelivery.ConvertAll(x => new { Value = x });
                    break;
                case 6:
                    dataGridView1.DataSource = reports[X].UniqueTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 7:
                    dataGridView1.DataSource = reports[X].DeliveredTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 8:
                    dataGridView1.DataSource = reports[X].DifferenceTracksRun.ConvertAll(x => new { Value = x });
                    break;
                case 9:
                    dataGridView1.DataSource = reports[X].NotDeliveredTracksRun.ConvertAll(x => new { Value = x });
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
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{(int)x.IsPayNeed == 1}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{(int)x.IsPayNeed == 1}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                        });
                    }
                    if (NewMail.Count > 0)
                    {
                        NewMail.Parcels.ForEach((x) =>
                        {
                            if (x.PlannedDate != DateTime.MinValue)
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{(int)x.IsPayNeed == 1}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address.Replace('"', ' ')}\", \"{x.Category}\", \"{x.Name.Replace('"', ' ')}\", \"{(int)x.IsPayNeed == 1}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
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

        private void addRouteTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (AddRoute(settings_addRouteTextBox.Text))
                {
                    RefreshRouteBox();
                    settings_addRouteTextBox.ResetText();
                }
            }
        }

        private void addRouteButton_Click(object sender, EventArgs e)
        {
            if (AddRoute(settings_addRouteTextBox.Text))
            {
                RefreshRouteBox();
                settings_addRouteTextBox.ResetText();
            }
        }

        private void deleteRouteButton_Click(object sender, EventArgs e)
        {
            if (settings_routBox.SelectedIndex != -1)
            {
                Manager.ExecuteNonQuery($"DELETE FROM Route WHERE route_name='{settings_routBox.SelectedItem}'");
                RefreshRouteBox();
            }
        }

        private void courierRouteComboBox_Enter(object sender, EventArgs e)
        {
            settings_courierRouteComboBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT route_name FROM Route"))
            {
                while (reader.Read())
                {
                    settings_courierRouteComboBox.Items.Add(reader.GetString(0));
                }
            }
        }

        private void courierAddButton_Click(object sender, EventArgs e)
        {
            AddCourier(settings_courierNameTextBox.Text, settings_courierRouteComboBox.Text);
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
            settings_courierListBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT fullName, route FROM Courier"))
            {
                while (reader.Read())
                {
                    settings_courierListBox.Items.Add(reader.GetString(0) + " - " + reader.GetString(1));
                }
            }
        }

        private void RefreshRouteBox()
        {
            settings_routBox.Items.Clear();
            using (var reader = Manager.ExecuteReader($"SELECT route_name FROM Route"))
            {
                while (reader.Read())
                {
                    settings_routBox.Items.Add(reader.GetString(0));
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
                if (CourierNameCombobox.SelectedIndex == -1)
                {
                    MessageBox.Show("Необходимо выбрать курьера\\маршрут.");
                    return;
                }

                string track = trackTextBox.Text.ToUpper();

                if (rackRadioBtn.Checked)
                {
                    if (!IsValid(track))
                    {
                        label7.Text = "Некорректный номер";
                        trackTextBox.ResetText();
                        return;
                    }

                    label7.ResetText();
                    if (Ukd.TrackListOnRacks.Contains(track) || CurrentRackList.Contains(track))
                    {
                        label7.Text = "Повторный ШПИ";
                        trackTextBox.ResetText();
                        return;
                    }

                    CurrentRackList.Add(track);
                    trackDataGrid.Rows.Add(track);

                    trackTextBox.Clear();

                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }

                if (routeRadioBtn.Checked)
                {
                    if (IsValid(track))//TODO: Избежать дублирования кода - Поднять проверку валидности ШПИ на уровень выше. 
                    {
                        label7.ResetText();
                        if (CurrentRun.TracksInRun.Contains(track) || Ukd.GetAllTracksInRuns.Contains(track))
                        {
                            label7.Text = "Повторный ШПИ";
                            trackTextBox.ResetText();
                            return;
                        }

                        trackDataGrid.Rows.Add(track);
                        CurrentRun.TracksInRun.Add(track);

                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    else
                        label7.Text = "Некорректный номер";

                    trackTextBox.Clear();
                }
            }
        } //Текстовое поле ввода трек-номера

        private IsPayneedResult ContainsInDataBase(string track)
        {
            return AllMailList.Find((item) => item.TrackID == track)?.IsPayNeed ?? IsPayneedResult.NotFound;
        }
        private bool IsValid(string track)
        {
            return match.IsMatch(track) || match2.IsMatch(track);
        }

        private void dayDatePicker_ValueChanged(object sender, EventArgs e)
        {
            GetStorageReportByDay(dayDatePicker.Value);

            //TODO: Необходим полный реворк
            countInRunLabel.ResetText();

            IsReadyToWork();

            Ukd.Runs.ForEach((x) => x.TracksInRun.Clear());
            Ukd.Runs = DB.GetRunsByDate(dayDatePicker.Value);



            TrackDataGridClear();
            if (CourierNameCombobox.SelectedIndex != -1)
            {
                if (routeRadioBtn.Checked)
                {
                    FillRunForCurrentCourier();
                }
            }

            UpdateStatistic();
        }

        private void FillRunForCurrentCourier()
        {
            if (rackRadioBtn.Checked)
            {
                var rack = Ukd.GetRackByCourier(CourierNameCombobox.Text);
                rack?.TrackList.ForEach((x) =>
                {
                    trackDataGrid.Rows.Add(x);
                });
            }
            else
            {
                var run = Ukd.Runs.Find((x) => x.Courier == CourierNameCombobox.SelectedItem.ToString());
                run?.TracksInRun.ForEach((x) =>
                {
                    trackDataGrid.Rows.Add(x);
                });
            }
        }

        private void IsReadyToWork()
        {
            if (dayDatePicker.Value.Date != DateTime.Now.Date || CourierNameCombobox.SelectedIndex == -1)
            {
                trackTextBox.Enabled = true;
            }
            else
            {
                trackTextBox.Enabled = true;
            }
        }

        private void CourierNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        { 
            IsReadyToWork();
            TrackDataGridClear();
            FillRunForCurrentCourier();

            if (rackRadioBtn.Checked)
            {
                CurrentRackList.Clear();
            }

            if(routeRadioBtn.Checked)
            {
                CurrentRun = new Run() { Courier = CourierNameCombobox.Text, Date = dayDatePicker.Value, Route = Ukd.GetRoute(CourierNameCombobox.Text), TracksInRun = new List<string>() };
            }
            UpdateStatistic();
        }

        private void TrackDataGridClear()
        {
            int count = trackDataGrid.Rows.Count;
            for (int i = 0; i < count; i++) //Очистить routeDataGrid (Список почты в ране) после выбора курьера.
            {
                trackDataGrid.Rows.Remove(trackDataGrid.Rows[0]);
            }
        }

        private void InsertButton_Click(object sender, EventArgs e)
        {
            label7.ResetText();
            if (rackRadioBtn.Checked)
            {
                string name = CourierNameCombobox.Text;
                string date = dayDatePicker.Text;

                if (Ukd.GetRackByCourier(name) is null)
                    Ukd.AddRack(name, Ukd.GetRoute(name), new List<string>(), dayDatePicker.Value);

                Rack _rack = Ukd.GetRackByCourier(name);
                foreach (DataGridViewRow row in trackDataGrid.Rows)
                {
                    string track = row.Cells[0].Value.ToString();
                    if (!_rack.TrackList.Contains(track))
                    {
                        Ukd.AddTrackToRack(track, Ukd.GetRackByCourier(name));
                        Manager.ExecuteNonQuery($"INSERT INTO [Rack] ([courier_id], [route_id], [track], [date]) VALUES ('{name}', '{Ukd.GetRoute(name)}', '{track}', '{date}');"); //Запись в БД информации о сканировании РПО.
                    }
                }

            }

            if (routeRadioBtn.Checked)
            {
                if (CurrentRun.TracksInRun?.Count <= 0)
                {
                    label7.Text = "Пусто";
                    return;
                }

                var courier = CourierNameCombobox.Text;

                //if (Ukd.Runs.Find((x) => x.Courier == courier) == null) // Создание пустой полки выбранному курьеру для заполнения.
                //    Ukd.Runs.Add(new Run() { Courier = courier, Date = dayDatePicker.Value, Route = route, TracksInRun = new List<string>() });

                if (CurrentRun.Courier != courier)
                {
                    MessageBox.Show("Ошибка");
                    return;
                }

                CurrentRun.TracksInRun = CurrentRun.TracksInRun.Except(Ukd.GetAllTracksInRuns).ToList();
                foreach (var track in CurrentRun.TracksInRun)
                {
                    IsPayneedResult isInBase = ContainsInDataBase(track);                   
                    DB.AddParcelToRunDB(CurrentRun, track, isInBase);
                }
                Ukd.MergeRuns(CurrentRun);
                CurrentRun.TracksInRun.Clear();
            }
            UpdateStatistic();
        }

        #region Выбор: Полка или Рейс

        private void rackRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Text = "Rack";
            TrackDataGridClear();
            if (CourierNameCombobox.SelectedIndex != -1)
                FillRunForCurrentCourier();
        }

        private void routeRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Text = "Route";
            TrackDataGridClear();
            if (CourierNameCombobox.SelectedIndex != -1)
                FillRunForCurrentCourier();
        }

        #endregion

        /// <summary>
        /// Предварительное создание пустой полки для выбранного курьера, если полка для него отсутствует.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void importComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Ukd.GetRackByCourier(CourierNameCombobox.Text) is null)
                Ukd.AddRack(CourierNameCombobox.Text, Ukd.GetRoute(CourierNameCombobox.Text), new List<string>(), dayDatePicker.Value);
        }


        #region Кнопка удаления     
        /// <summary>
        /// Удаление всех Трэк-номеров из таблиц RUNS и RACKS выбранного курьера за выбранную дату.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteRunButton_Click(object sender, EventArgs e)
        {
            if(rackRadioBtn.Checked)
            {
                Manager.ExecuteNonQuery($"DELETE FROM Rack WHERE courier_id=('{CourierNameCombobox.Text}') AND date=('{dayDatePicker.Value.ToShortDateString()}')");
                trackDataGrid.Rows.Clear();
            }
            if (routeRadioBtn.Checked)
            {
                Manager.ExecuteNonQuery($"DELETE FROM Runs WHERE Courier=('{CourierNameCombobox.Text}') AND Date=('{dayDatePicker.Value.ToShortDateString()}')");
                trackDataGrid.Rows.Clear();
            }
            UpdateStatistic();
        }
        #endregion

        #region вкладка Work      
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

        #endregion

        #region События списка трэк-номеров
        private void routeDataGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var track = trackDataGrid.Rows[e.RowIndex].Cells[0].Value.ToString();
            var _rpo = AllMailList.Find((item) => item.TrackID == track)?.IsPayNeed ?? IsPayneedResult.NotFound;
            switch (_rpo)
            {
                case IsPayneedResult.Need:
                    trackDataGrid.Rows[e.RowIndex].Cells[0].Style.BackColor = Color.Yellow;
                    break;

                case IsPayneedResult.NotNeed:
                    break;

                default:
                    trackDataGrid.Rows[e.RowIndex].Cells[0].Style.BackColor = Color.OrangeRed;
                    break;
            }
            countInRunLabel.Text = trackDataGrid.Rows.Count.ToString();
        }
        private void routeDataGrid_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var datagrid = (DataGridView)sender;
            var track = datagrid.CurrentCell.Value.ToString();

            if (routeRadioBtn.Checked)
            {
                CurrentRun.TracksInRun.Remove(track);
                DB.RemoveParcelFromRun(CourierNameCombobox.Text, dayDatePicker.Text, track);
                datagrid.Rows.Remove(datagrid.CurrentRow);
                Ukd.DeleteTrack(CurrentRun, track);
            }

            if (rackRadioBtn.Checked)
            {
                CurrentRackList.Remove(track);
                DB.RemoveParcelFromRack(CourierNameCombobox.Text, dayDatePicker.Text, track);
                datagrid.Rows.Remove(datagrid.CurrentRow);
                Ukd.GetRackByCourier(CourierNameCombobox.Text)?.TrackList.Remove(track);
            }
            UpdateStatistic();
        }
        private void routeDataGrid_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            countInRunLabel.Text = trackDataGrid.Rows.Count.ToString();
        }
        #endregion

        #region Обновить список курьеров


        private void обновитьСписокToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateCourierNameComboboxData();
        }

        private void UpdateCourierNameComboboxData()
        {
            CourierNameCombobox.Items.Clear();
            CourierNameCombobox.Items.AddRange(DB.GetCourierListFromDataBase().ToArray());
        }
        #endregion

        private void CourierNameCombobox_TextChanged(object sender, EventArgs e)
        {
            if (CourierNameCombobox.Text.Length <= 0)
            {
                InsertButton.Enabled = false;
            }
            else
            {
                InsertButton.Enabled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DoReport();
        }

        #region Resize
        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            SuspendLayout();
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            ResumeLayout();
        }
        #endregion

        private void approveTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            string track = approveTextBox.Text;
            if (e.KeyCode == Keys.Enter)
            {
                if (IsValid(track))
                {
                    DB.AddToApproveTable(track);

                    approveTextBox.Clear();

                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }



    }
}