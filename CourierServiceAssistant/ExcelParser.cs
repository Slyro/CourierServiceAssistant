using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using CourierServiceAssistant.sklad;

namespace CourierServiceAssistant
{
    public enum FarEast
    {
        TrackID = 0,
        RegistrationTime = 1,
        DestinationIndex = 4,
        LastOperation = 7,
        LastZone = 8,
        Type = 26,
        Category = 27,
        Index = 14,
        PlannedDate = 13,
        UnsuccessfulDeliveryCount = 16,
        Name = 36,
        Address = 41,
        Telephone = 37,
        TitleUKD = 44,
        IsNeedPay = 56
    }
    public static class ExcelReader
    {
        public static Parcel GetParcel(IExcelDataReader reader)
        {
            string getaddress()
            {
                return reader.GetString((int)FarEast.Address);
            }
            string getcategory() => reader.GetString((int)FarEast.Category);
            string GetTrackID() => reader.GetValue((int)FarEast.TrackID).ToString();
            DateTime getRegistrationTime() => reader.GetDateTime((int)FarEast.RegistrationTime);
            int? getDestinationIndex() => Convert.ToInt32(reader.GetValue((int)FarEast.DestinationIndex));
            string getType() => reader.GetString((int)FarEast.Type);
            int getIndex() => (int)reader.GetDouble((int)FarEast.Index);
            DateTime getPlannedDate() => Convert.ToDateTime(reader.GetValue((int)FarEast.PlannedDate));
            int getUnsuccessfulDeliveryCount() => (int)reader.GetDouble((int)FarEast.UnsuccessfulDeliveryCount);
            string getName() => reader.GetValue((int)FarEast.Name) == null ? string.Empty : reader.GetValue((int)FarEast.Name).ToString();
            string getTelephoneNumber() => reader.GetValue((int)FarEast.Telephone) == null ? string.Empty : reader.GetValue((int)FarEast.Telephone).ToString();
            IsPayneedResult getIsPayNeed()
            {
                return (int)reader.GetDouble((int)FarEast.IsNeedPay) == 1 ? IsPayneedResult.Need: IsPayneedResult.NotNeed;
            }

                

            Parcel _p = new Parcel
            {
                Address = getaddress(),
                Category = getcategory(),
                TrackID = GetTrackID(),
                RegistrationTime = getRegistrationTime(),
                DestinationIndex = getDestinationIndex(),
                Type = getType(),
                Index = getIndex(),
                PlannedDate = getPlannedDate(),
                UnsuccessfulDeliveryCount = getUnsuccessfulDeliveryCount(),
                Name = getName(),
                TelephoneNumber = getTelephoneNumber(),
                IsPayNeed = getIsPayNeed()
            };
            _p.LastOperation = (reader.GetString((int)FarEast.LastOperation));
            _p.LastZone = (reader.GetString((int)FarEast.LastZone));
            return _p;
        }
        public static List<Parcel> GetParcel(SQLiteDataReader reader)
        {
            List<Parcel> tmp = new List<Parcel>();
            while (reader.Read())
            {
                //var track = reader.GetString(1);
                //var regtime = reader.GetString(2);
                //var plandate = reader.GetString(3).Length <= 0 ? null : (DateTime?)Convert.ToDateTime(reader.GetString(3));// reader.GetString(3);
                //var Index = reader.GetString(4);
                //var count = reader.GetString(5);
                //var DestIndex = reader.GetString(6);
                //var lastOP = reader.GetString(7);
                //var address = reader.GetString(8);
                //var category = reader.GetString(9);
                //var name = reader.GetString(10);
                //var nalojka = reader.GetString(11);
                //var tel = reader.GetString(12);
                //var type = reader.GetString(13);
                //var zone = reader.GetString(14);           
                Parcel _p = new Parcel
                {
                    Address = reader.GetString(7),
                    Category = reader.GetString(8),
                    TrackID = reader.GetString(0),
                    RegistrationTime = Convert.ToDateTime(reader.GetString(1)),
                    DestinationIndex = int.Parse(reader.GetString(5)),
                    Type = reader.GetString(12),
                    Index = int.Parse(reader.GetString(3)),
                    PlannedDate = reader.GetString(2).Length <= 0 ? null : (DateTime?)Convert.ToDateTime(reader.GetString(2)),
                    UnsuccessfulDeliveryCount = int.Parse(reader.GetString(4)),
                    Name = reader.GetString(9),
                    TelephoneNumber = reader.GetString(11),
                    IsPayNeed = bool.Parse(reader.GetString(10)) ? IsPayneedResult.Need : IsPayneedResult.NotNeed
                };
                _p.LastOperation = reader.GetString(6);
                _p.LastZone = reader.GetString(13);
                tmp.Add(_p);
            }
            return tmp;
        }
    }
}