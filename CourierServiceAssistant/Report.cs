using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourierServiceAssistant
{
    class Report
    {
        static public List<string> GoneByReport;
        private readonly List<Rack> Racks;
        private List<string> allTracksRacks;
        private List<string> allTracksRuns;
        private List<Run> Runs;
        public string Route { get; set; }
        private void FillTracks(List<Rack> racks)
        {
            AllTracksOnRacks = new List<string>();
            for (int i = 0; i < racks.Count; i++)
            {
                AllTracksOnRacks.AddRange(racks[i].TrackList);
            }
        }

        private void FillTracks(List<Run> runs)
        {
            AllTracksOnRuns = new List<string>();
            for (int i = 0; i < runs.Count; i++)
            {
                AllTracksOnRuns.AddRange(runs[i].TracksInRun);
            }
        }

        public Report(List<Rack> racks, List<Run> runs)
        {
            if (racks.Count > 0)
            {
                Route = racks[0].Route;
                Racks = racks;
                FillTracks(Racks);
            }
            if (runs.Count > 0)
            {
                Runs = runs;
                FillTracks(Runs);
            }
        }
        public List<string> UniqueTracksRack
        {
            get
            {
                return AllTracksOnRacks.Distinct().ToList();
            }
        }
        public List<string> MustBeOnRack
        {
            get
            {
                return UniqueTracksRack.Except(GoneByReport).ToList();
            }
        }
        public List<string> DeliveredTracksRack
        {
            get
            {
                return UniqueTracksRack.Except(MustBeOnRack).ToList();
            }
        }
        public double AvarageUniqueRack
        {
            get
            {
                return UniqueTracksRack.Count / Racks.Count;
            }
        }
        public double AvarageAllRack
        {
            get
            {
                return AllTracksOnRacks.Count / Racks.Count;
            }
        }
        public List<string> AllTracksOnRacks { get => allTracksRacks; set => allTracksRacks = value; }
        public List<string> AllTracksOnRuns { get => allTracksRuns; set => allTracksRuns = value; }
        public double AvarageAllRun
        {
            get
            {
                return AllTracksOnRuns.Count / Runs.Count;
            }
        }

        public List<string> UniqueTracksRun
        {
            get
            {
                return AllTracksOnRuns.Distinct().ToList();
            }
        }

        public List<string> DeliveredTracksRun
        {
            get
            {
                return UniqueTracksRun.Where((x) => (GoneByReport.Contains(x))).ToList();
            }
        }
        public List<string> NotDeliveredTracksRun
        {
            get
            {
                return UniqueTracksRun.Except(GoneByReport).ToList();
            }
        }
    }
}
