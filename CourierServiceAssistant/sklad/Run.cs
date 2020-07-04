using System;
using System.Collections.Generic;

namespace CourierServiceAssistant
{
    class Run
    {
        public string Courier { get; set; }

        public string Route { get; set; }
        public DateTime Date { get; set; }
        public List<string> TracksInRun { get; set; }

        public Run()
        {
        }

        public Run(string name, List<string> tracksInRun)
        {
            Courier = name;
            TracksInRun = tracksInRun;
        }
        public Run(string name, string route, List<string> tracksInRun, DateTime date)
        {
            Courier = name;
            Route = route;
            TracksInRun = tracksInRun;
            Date = date;
        }
    }
}
