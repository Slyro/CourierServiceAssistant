using System.Windows.Forms;
using System.Data.SQLite;
using System.Collections.Generic;
using System;

namespace CourierServiceAssistant
{

    class DBAction
    {
        private readonly string GetAllParcelsCommand = $"SELECT TrackNumber,RegistrationTime,PlannedDate,[Index],UnsuccessfulDeliveryCount,DestinationIndex,LastOperation,Address,Category,Name,IsPayNeed,Telephone,Type,Zone FROM Parcels";
        private readonly string GetGoneParcelsCommand = $"SELECT TrackNumber,RegistrationTime,PlannedDate,[Index],UnsuccessfulDeliveryCount,DestinationIndex,LastOperation,Address,Category,Name,IsPayNeed,Telephone,Type,Zone FROM GoneByReport";
        private readonly DBManager Manager;

        public DBAction(DBManager manager)
        {
            Manager = manager;
        }
        public List<Parcel> GetAllParcelFromDataBase()
        {
            using (var reader = Manager.ExecuteReader(GetAllParcelsCommand))
            {
                List<Parcel> list;
                return list = new List<Parcel>(ExcelReader.GetParcel(reader));
            }//Получение Базового списка всех РПО
        }
        public List<Parcel> GetGoneParcelFromDataBase()
        {
            using (var reader = Manager.ExecuteReader(GetGoneParcelsCommand))
            {
                List<Parcel> list;
                return list = new List<Parcel>(ExcelReader.GetParcel(reader));
            }//Получение Базового списка всех РПО
        }
        public List<Rack> GetRacksPerDayByRoute(string route)
        {
            List<string> dates = new List<string>();
            List<Rack> racks = new List<Rack>();
            using (var reader = Manager.ExecuteReader($"SELECT DISTINCT date FROM Rack WHERE route_id='{route}'"))
            {
                while (reader.Read())
                {
                    racks.Add(new Rack("", route, new List<string>(), DateTime.Parse(reader.GetString(0))));
                }
            }

            using (var reader = Manager.ExecuteReader($"SELECT track, date FROM Rack WHERE route_id='{route}'"))
            {
                while (reader.Read())
                {
                    racks.Find((x) => x.Date == DateTime.Parse(reader.GetString(1)))
                        .TrackList.Add(reader.GetString(0).ToUpperInvariant());
                }
            }
            return racks;
        }
        public List<Run> GetRunsPerDayByRoute(string route)
        {
            //TODO: Подвязать курьера к рейсам
            List<string> dates = new List<string>();
            List<Run> runs = new List<Run>();

            using (var reader = Manager.ExecuteReader($"SELECT DISTINCT Runs.Date FROM Runs JOIN Courier ON Runs.Courier == Courier.fullname WHERE Courier.route == '{route}' ORDER By Runs.Date"))
            {
                while (reader.Read())
                {
                    runs.Add(new Run("", route, new List<string>(), DateTime.Parse(reader.GetString(0))));
                }
            }

            using (var reader = Manager.ExecuteReader($"SELECT Runs.Date, Runs.track FROM Runs JOIN Courier ON Runs.Courier == Courier.fullname WHERE Courier.route == '{route}' ORDER BY Runs.Date"))
            {
                while (reader.Read())
                {
                    runs.Find((x) => x.Date == DateTime.Parse(reader.GetString(0)))
                        .TracksInRun.Add(reader.GetString(1).ToUpperInvariant());
                }
            }
            return runs;
        }
        public Dictionary<string, string> GetNameRoutePairs()
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            using (var reader = Manager.ExecuteReader("SELECT fullname, route FROM Courier"))
            {
                while (reader.Read())
                {
                    pairs.Add(reader.GetString(0), reader.GetString(1));
                }
            }//Заполнение экземпляра класса UKD списком курьеров и районов, полок.
            return pairs;
        }
        public List<Rack> GetRacksByDate(DateTime date)
        {
            List<Rack> list = new List<Rack>();
            using (var reader = Manager.ExecuteReader($"SELECT DISTINCT courier_id, route_id FROM Rack WHERE date='{date.ToShortDateString()}'"))//Выгрузка из БД записей о курьере и списке треков на основании условий.
            {
                while (reader.Read())//Сбор данных о треках пикнутых курьером в текущий день на определнной полке.
                {
                    list.Add(new Rack
                    {
                        Couerier = reader.GetString(0),
                        Route = reader.GetString(1),
                        Date = date,
                        TrackList = new List<string>()
                    });
                }
            }
            using (var reader = Manager.ExecuteReader($"SELECT courier_id, track FROM Rack WHERE date='{date.ToShortDateString()}'"))//Выгрузка из БД записей о курьере и списке треков на основании условий.
            {
                while (reader.Read())//Сбор данных о треках пикнутых курьером в текущий день на определнной полке.
                {
                    list.Find(x => x.Couerier.Equals(reader.GetString(0))).TrackList.Add(reader.GetString(1));
                }
            }
            return list;
        }
        public List<Run> GetRunsByDate(DateTime date)
        {
            List<Run> list = new List<Run>();

            using (var reader = Manager.ExecuteReader($"SELECT DISTINCT Courier FROM Runs WHERE Date = ('{date.ToShortDateString()}')"))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        list.Add(new Run() { Courier = reader.GetString(0), TracksInRun = new List<string>() });
                    }
                }
            }

            using (var reader = Manager.ExecuteReader($"SELECT Track, Courier FROM Runs WHERE Date = ('{date.ToShortDateString()}')"))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        list.Find((x) => x.Courier == reader.GetString(1)).TracksInRun.Add(reader.GetString(0));
                    }
                }
            }
            return list;
        }
        public bool AddRoute(string routeName)
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
                _ = Manager.ExecuteNonQuery($"INSERT INTO Route (route_name) VALUES ('{routeName}')");
                return true;
            }
            return false;
        }

        public void AddParcelToRackDB(Rack rack, string track)
        {
            Manager.ExecuteNonQuery($"INSERT INTO [Rack] ([courier_id], [route_id], [track], [date]) VALUES ('{rack.Couerier}', '{rack.Route}', '{track}', '{rack.Date}');");
        }
        public void AddParcelToRackDB(Rack rack, string[] tracks)
        {
            foreach (var item in tracks)
            {
                AddParcelToRackDB(rack, item);
            }
        }
        public void AddParcelToRackDB(Rack rack, List<string> tracks)
        {
            foreach (var item in tracks)
            {
                AddParcelToRackDB(rack, item);
            }
        }

        public void AddParcelToRunDB(Run run, string track, bool isNew)
        {
            Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{track}', '{run.Courier}', {(isNew?1:0)}, '{run.Date.ToShortDateString()}');");
        }

        public void AddParcelToRunDB(Run run, string[] tracks, bool isNew)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{tracks[i]}', '{run.Courier}', {(isNew ? 1 : 0)}, '{run.Date.ToShortDateString()}');");
            }
        }

        public void AddParcelToRunDB(Run run, List<string> tracks, bool isNew)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                Manager.ExecuteNonQuery($"INSERT INTO [Runs] (Track, Courier, isNew, Date) VALUES ('{tracks[i]}', '{run.Courier}', {(isNew ? 1 : 0)}, '{run.Date.ToShortDateString()}');");
            }
        }

    }
}
