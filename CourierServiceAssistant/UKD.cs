using System;
using System.Collections.Generic;
using System.Linq;

namespace CourierServiceAssistant
{
    internal class UKD
    {
        public List<Run> Runs;
        private readonly Dictionary<string, string> CourierRouteDictionary;
        public DateTime Date;
        private readonly List<Rack> Rack;
        public UKD()
        {
            Rack = new List<Rack>();
            TrackList = new List<string>();
            Date = new DateTime();
            CourierRouteDictionary = new Dictionary<string, string>();
            Runs = new List<Run>();
        }

        public List<Rack> GetAllRacks => Rack.FindAll((x) => x.TrackList.Count != 0);
        public string[] GetAllRoutes => CourierRouteDictionary.Values.Distinct().ToArray();

        public Dictionary<string, string> GetCourierRouteDictionary() => CourierRouteDictionary;
        public List<string> TrackList { get; }
        public void AddCourier(string courier, string route)
        {
            CourierRouteDictionary.Add(courier, route);
        }

        private string AddRack(Rack rack)
        {
            Rack.Add(rack);
            if (AddTrackFromRackToTrackList(rack))
            {
                return "Найдены дубликаты!";
            }
            else
            {
                return "Дубликаты не найдены.";
            }
        }
        public void AddRacks(List<Rack> racks)
        {
            RackErase();
            foreach (var item in racks)
            {
                AddRack(item);
            }
        }


        public void AddRack(string courier, string route, List<string> tracks, DateTime date)
        {
            Rack.Add(new Rack()
            {
                Couerier = courier,
                Route = route,
                TrackList = tracks,
                Date = date.Date
            });
            AddTrackFromRackToTrackList();
        }

        public void AddTrackToRack(string track, Rack rack)
        {
            var _rack = Rack.Find((x) => x.Couerier == rack.Couerier);
            if (_rack is null)
            {
                AddRack(rack);
            }
            else
            {
                _rack.TrackList.Add(track);
                TrackList.Add(track);
            }
        }

        public Rack GetRackByCourier(string courier)
        {
            return Rack.Find((x) => x.Couerier == courier);
        }

        public Rack GetRackByRoute(string route)
        {
            return Rack.Find((x) => x.Route == route);
        }

        public string GetRoute(string courier)
        {
            return CourierRouteDictionary[courier];
        }

        public void RackErase()
        {
            Rack.Clear();
            //foreach (var item in CourierRouteDictionary)
            //{
            //    Rack.Add(new Rack(item.Key, item.Value, new List<string>(), new DateTime()));
            //}
            TrackList.Clear();
        }
        private void AddTrackFromRackToTrackList()
        {
            TrackList.Clear();
            Rack.ForEach((x) => TrackList.AddRange(x.TrackList));
        }

        /// <summary>
        /// Добавляем треки к полкам.
        /// </summary>
        /// <param name="rack"></param>
        /// <returns>Возвращает true если есть дубликаты</returns>
        private bool AddTrackFromRackToTrackList(Rack rack)
        {
            bool HaveDuplicate = false;
            rack.TrackList.ForEach((x) =>
            {
                if (!TrackList.Contains(x))
                {
                    TrackList.Add(x);
                }
                else
                {
                    HaveDuplicate = true;
                }
            });
            return HaveDuplicate;
        }

        //private void AddTrackFromRackToTrackList(List<string> tracklist)
        //{
        //    tracklist.ForEach((x) =>
        //    {
        //        if (!TrackList.Contains(x))
        //        {
        //            TrackList.Add(x);
        //        }
        //    });
        //}
    }
}