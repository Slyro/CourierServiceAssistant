using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourierServiceAssistant
{
    class GoneMail
    {
        public List<Parcel> Parcels { get; private set; }
        public GoneMail(List<Parcel> parcels)
        {
            Parcels = parcels;
        }

        public int Count => Parcels.Count;
        public List<Parcel> GetList() => Parcels;
    }
}
