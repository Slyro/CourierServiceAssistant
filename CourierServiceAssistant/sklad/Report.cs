using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourierServiceAssistant
{
    class Report
    {
        /// <summary>
        /// Репорт по курьерам.
        /// 
        /// 
        /// </summary>
        

        static public List<string> GoneByReport;
        static public List<string> CurrentList;
        static public List<string> Approved;

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
            AllTrackInRuns = new List<string>();
            for (int i = 0; i < runs.Count; i++)
            {
                AllTrackInRuns.AddRange(runs[i].TracksInRun);
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

        #region Racks
        public double AvarageAllRack                => AllTracksOnRacks.Count / Racks.Count;
        public double AvarageUniqueRack             => UniqueTracksRack.Count / Racks.Count;
        public List<string> MustBeOnRack            => UniqueTracksRack.Except(GoneByReport).ToList();
        public List<string> UniqueTracksRack        => AllTracksOnRacks.Distinct().ToList();
        public List<string> DeliveredTracksRack     => UniqueTracksRack.Except(MustBeOnRack).ToList();
        public List<string> WithoutDelivery         => MustBeOnRack.Except(AllTrackInRuns.Distinct()).ToList();
        public List<string> AllTracksOnRacks { get; set; }
        #endregion

        #region Runs
        public double AvarageAllRun                 => AllTrackInRuns.Count / Runs.Count;
        public List<string> UniqueTracksRun         => AllTrackInRuns.Distinct().ToList();
        public List<string> DeliveredTracksRun      => UniqueTracksRun.Where(track => GoneByReport.Contains(track) || !CurrentList.Contains(track)).ToList();
        public List<string> DifferenceTracksRun     => UniqueTracksRun.Except(DeliveredTracksRun).Except(Approved).ToList();  
        public List<string> NotDeliveredTracksRun   => UniqueTracksRun.Where((track) => !GoneByReport.Contains(track) && !CurrentList.Contains(track)).Except(Approved).ToList();
        private List<string> LastThreeDaysTrackList
        {
            get
            {
                int indexOflastElement = Runs.Count - 1;
                List<string> list = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    list.AddRange(Runs[indexOflastElement - i].TracksInRun);
                }
                return list;
            }
        }
        public List<string> AllTrackInRuns { get; set; }
        #endregion

    }
}
