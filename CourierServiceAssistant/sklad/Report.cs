using System.Collections.Generic;
using System.Linq;

namespace CourierServiceAssistant
{
    class Report
    {
        static public List<string> GoneByReport;
        static public List<string> CurrentList;
        static public List<string> Approved;

        private readonly List<Rack> Racks;
        private readonly List<Run> Runs;

        public List<string> AllTrackInRuns { get; private set; }
        public List<string> AllTracksOnRacks { get; private set; }
        public string Route { get; set; }

        #region Racks
        public double AvarageAllRack { get; private set; }
        public double AvarageUniqueRack { get; private set; }
        public List<string> MustBeOnRack { get; private set; }
        public List<string> UniqueTracksRack { get; private set; }
        public List<string> DeliveredTracksRack { get; private set; }
        public List<string> WithoutDelivery { get; private set; }
        #endregion

        #region Runs
        public double AvarageAllRun { get; private set; }
        public List<string> UniqueTracksRun { get; private set; }
        public List<string> DeliveredTracksRun { get; private set; }
        public List<string> DifferenceTracksRun { get; private set; }
        public List<string> NotDeliveredTracksRun { get; private set; }
        #endregion

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

        public void Calc()
        {
            CalcRack();
            CalcRuns();
        }

        private void CalcRack()
        {
            UniqueTracksRack = AllTracksOnRacks.Distinct().ToList();
            AvarageAllRack = AllTracksOnRacks.Count / Racks.Count;
            AvarageUniqueRack = UniqueTracksRack.Count / Racks.Count;
            MustBeOnRack = UniqueTracksRack.Except(GoneByReport).ToList();
            DeliveredTracksRack = UniqueTracksRack.Except(MustBeOnRack).ToList();
            Racks.Clear();
        }

        private void CalcRuns()
        {
            AvarageAllRun = AllTrackInRuns.Count / Runs.Count;
            UniqueTracksRun = AllTrackInRuns.Distinct().ToList();

            //АД
            DeliveredTracksRun = UniqueTracksRun.AsParallel().Where(track => GoneByReport.Contains(track) || !CurrentList.Contains(track)).ToList();
            NotDeliveredTracksRun = UniqueTracksRun.AsParallel().Where((track) => !GoneByReport.Contains(track) && !CurrentList.Contains(track)).AsQueryable().Except(Approved).ToList();
            //

            DifferenceTracksRun = UniqueTracksRun.Except(DeliveredTracksRun).Except(Approved).ToList();
            WithoutDelivery = MustBeOnRack.Except(AllTrackInRuns.Distinct()).ToList();
            Runs.Clear();
        }
    }
}
