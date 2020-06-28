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
        static public List<string> CurrentList;
        private readonly List<Rack> Racks;
        private readonly List<Run> Runs;
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

        #region Racks
        public List<string> AllTracksOnRacks { get; set; }
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


        #endregion

        #region Runs
        public List<string> AllTracksOnRuns { get; set; }
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
                return UniqueTracksRun.Where(x => GoneByReport.Contains(x) | !CurrentList.Contains(x)).ToList();
            }
        }

        public List<string> DifferenceTracksRun
        {
            get
            {
                return UniqueTracksRun.Except(DeliveredTracksRun).ToList();
                //return DeliveredTracksRun.Where(x => !GoneByReport.Contains(x) & CurrentList.Contains(x)).ToList();
            }
        }

        public List<string> NotDeliveredTracksRun
        {
            get
            {
                return UniqueTracksRun.Where((x) => !GoneByReport.Contains(x) & !CurrentList.Contains(x)).ToList();
                //return UniqueTracksRun.Except(GoneByReport).ToList();
            }
        }
        #endregion

    }
}
