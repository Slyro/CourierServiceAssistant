using System.Windows.Forms;
using System.Data.SQLite;
using System.Collections.Generic;
using System;

namespace CourierServiceAssistant
{

    class DBAction
    {
        private readonly string GetAllParcelsCommand = $"SELECT TrackNumber,RegistrationTime,PlannedDate,[Index],UnsuccessfulDeliveryCount,DestinationIndex,LastOperation,Address,Category,Name,IsPayNeed,Telephone,Type,Zone FROM Parcels";
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
                        list.Add(new Run() { Name = reader.GetString(0), TracksInRun = new List<string>() });
                    }
                }
            }

            using (var reader = Manager.ExecuteReader($"SELECT Track, Courier FROM Runs WHERE Date = ('{date.ToShortDateString()}')"))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        list.Find((x) => x.Name == reader.GetString(1)).TracksInRun.Add(reader.GetString(0));
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
    }
}
