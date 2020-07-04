using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourierServiceAssistant
{
    class Rack
    {
        public string Couerier;
        public string Route;
        public List<string> TrackList;
        public DateTime Date;

        public Rack()
        {

        }
        public Rack(string couerier, string route, List<string> trackList, DateTime date)
        {
            Couerier = couerier;
            Route = route;
            TrackList = trackList;
            Date = date;
        }
    }
}
