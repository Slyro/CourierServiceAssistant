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

            GetStorageReportByDay(rackDateTimePicker.Value.Date);//Выгрузка информации о пикнут отправлениях на складе на основе даты.

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

        }//Заполнение экземпляра класса UKD информацией об отправлениях лежащих на полках курьеров, операторов, склад самовывоза и взятых в доставку РПО за выбраный день.

        private void SevenDays()
        {
            UKD[] aDay = new UKD[8];
            DateTime dateOf = DateTime.Parse("30.05.2020");

            for (int i = 0; i < aDay.Length; i++)
            {
                aDay[i] = GetUKD(dateOf);
                dateOf = dateOf.AddDays(1);
            }

            UKD GetUKD(DateTime date)
            {
                UKD _ukd = new UKD { Date = date };
                Dictionary<string, string> NameRoutePairs = DB.GetNameRoutePairs();
                foreach (string key in NameRoutePairs.Keys)
                {
                    _ukd.AddCourier(key, NameRoutePairs[key]);
                }
                _ukd.AddRacks(DB.GetRacksByDate(date));
                _ukd.Runs = DB.GetRunsByDate(date);
                return _ukd;
            }
        }

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
                    Text = $"{run.Name}: {run.TracksInRun.Count}",
                    Size = new Size(200, 20)
                };
                statisticPanel2.Controls.Add(label);
            }

            label3.Text = "РПО на складе: " + Ukd.TrackList.Count; //почты инвентаризированно.
            label11.Text = "Всего: " + Ukd.GetCountTracksInRuns;
        }//Заполнение области "Статистика" информацией о колличестве посылок на "районах" в т.ч. окно, сортировчный цех.

        private void Summ_Click(object sender, EventArgs e)
        {
            //TODO: Место для реализации дополнительного функционала при клике на "Итог" в статистике.
            MessageBox.Show("Не реализовано");
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
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address}\", \"{x.Category}\", \"{x.Name.Replace("\"", string.Empty)}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                GoneList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address}\", \"{x.Category}\", \"{x.Name.Replace("\"", string.Empty)}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                        });
                    }
                    if (NewMail.Count > 0)
                    {
                        NewMail.Parcels.ForEach((x) =>
                        {
                            if (x.PlannedDate != DateTime.MinValue)
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{x.PlannedDate?.ToShortDateString()}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address}\", \"{x.Category}\", \"{x.Name.Replace("\"", string.Empty)}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
                            }
                            else
                            {
                                NewList.Add($"(\"{x.TrackID}\", \"{x.RegistrationTime}\", \"{null}\", \"{x.Index}\", \"{x.UnsuccessfulDeliveryCount}\", \"{x.DestinationIndex}\", \"{x.LastOperation}\", \"{x.Address}\", \"{x.Category}\", \"{x.Name.Replace("\"", string.Empty)}\", \"{x.IsPayNeed}\", \"{x.TelephoneNumber}\", \"{x.Type}\", \"{x.LastZone}\", \"{ReportDate}\")");
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

        private void importComboBox_Enter(object sender, EventArgs e)
        {
            (sender as ComboBox).Items.Clear();
            (sender as ComboBox).Items.AddRange(GetCourierListFromDataBase().ToArray());
            ResetImport();
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

        private void ResetImport()
        {
            ListOfImportedTracks.Clear();
            importVisorTextBox.Clear();
            importLabel.Text = ListOfImportedTracks.Count.ToString();
        }

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

        private void resetButton_Click(object sender, EventArgs e)
        {
            ResetImport();
        }

        private void importTextBox_KeyDown(object sender, KeyEventArgs e) //Форма внесения информации об РПО на полках курьеров.
        {
            //TODO: Необходим полный реворк
            var track = importTextBox.Text.ToUpper();
            if (e.KeyCode == Keys.Enter)
            {
                label2.Text = "";
                if (importComboBox.SelectedIndex != -1)
                {
                    if (match.IsMatch(track) || match2.IsMatch(track))
                    {
                        AddParcelInRack(track);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        UpdateStatistic();
                    }
                    else
                    {
                        label2.Text = "Некорректный ШПИ.";
                        textBox1.Text += track + Environment.NewLine;
                    }
                }
                else
                    label2.Text = "Нужно выбрать курьера.";
                importTextBox.Clear();
            }
        }

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

        private void AddParcelInRack(string track)
        {
            //TODO: Необходим полный реворк
            var courier = importComboBox.SelectedItem.ToString();
            var date = rackDateTimePicker.Value.ToShortDateString();

            if (track.Length > 0)
            {
                if (!Ukd.TrackList.Contains(track))
                {
                    ListOfImportedTracks.Add(importTextBox.Text); //Временное хранение списка входящих РПО
                    importVisorTextBox.Text += importTextBox.Text.ToUpper() + "\r\n"; //Отображение входящих РПО
                    importLabel.Text = ListOfImportedTracks.Count.ToString(); //Счёт РПО
                    Manager.ExecuteNonQuery($"INSERT INTO [Rack] ([courier_id], [route_id], [track], [date]) VALUES ('{courier}', '{Ukd.GetRoute(courier)}', '{importTextBox.Text}', '{date}');"); //Запись в БД информации о сканировании РПО.
                    Ukd.AddTrackToRack(importTextBox.Text, Ukd.GetRackByCourier(courier));
                }
                else
                {
                    label2.Text = "Такой номер уже был внесен";
                }
            }
        } //Добавить посылку на полку

        private List<object> GetCourierListFromDataBase()
        {
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

        private void rackTimePicker_ValueChanged(object sender, EventArgs e)
        {
            routeDatePicker.Value = rackDateTimePicker.Value;
            GetStorageReportByDay(rackDateTimePicker.Value.Date);
        } //Выбора даты на вкладке "Учет склада". Перезаполняет экземпляр Ukd данными за выбранную дату.

        private void button4_Click(object sender, EventArgs e)
        {
            //Inventarisation();
        } //Клик по кнопке "ИНВЕНТАРИЗАЦИЯ"

        private void button5_Click(object sender, EventArgs e)
        {

        }// Скрыть/Показать поле "Инвентаризация"

      
        private void routeDatePicker_ValueChanged(object sender, EventArgs e)
        {
            //TODO: Необходим полный реворк
            rackDateTimePicker.Value = routeDatePicker.Value;
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
                CurrentRun = Ukd.Runs.Find((x) => x.Name == RouteComboBox.SelectedItem.ToString());
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
            if (Ukd.Runs.Find((x)=> x.Name == RouteComboBox.Text) is null)
                Ukd.Runs.Add(new Run() { Name = RouteComboBox.Text, TracksInRun = new List<string>() });

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
            CurrentRun = Ukd.Runs.Find((x) => x.Name == RouteComboBox.SelectedItem?.ToString());
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

        private void button5_Click_1(object sender, EventArgs e)
        {
            SevenDays();
        }

        private void rackRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Visible = false;
            rackGroupBox.Visible = true;
        }

        private void routeRadioBtn_CheckedChanged(object sender, EventArgs e)
        {
            routeGroupBox.Visible = true;
            rackGroupBox.Visible = false;
        }
        private void importComboBox_SelectedIndexChanged(object sender, EventArgs e)//Предварительное создание пустой полки для выбранного курьера, если полка для него отсутствует.
        {
            if (Ukd.GetRackByCourier(importComboBox.Text) is null)
                Ukd.AddRack(importComboBox.Text, Ukd.GetRoute(importComboBox.Text), new List<string>(), rackDateTimePicker.Value);
        }
        private void deleteRunButton_Click(object sender, EventArgs e)
        {
            Manager.ExecuteNonQuery($"DELETE FROM Runs WHERE Courier=('{RouteComboBox.Text}') AND Date=('{routeDatePicker.Value.ToShortDateString()}')");
            RouteComboBox.SelectedIndex = -1;
        }
    }
}