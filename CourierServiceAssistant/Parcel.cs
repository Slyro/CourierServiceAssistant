using System;
using System.Collections.Generic;

namespace CourierServiceAssistant
{
    public class Parcel
    {
        public DateTime RegistrationTime { get; set; }
        public string TrackID { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string TelephoneNumber { get; set; }
        public bool IsPayNeed { get; set; }
        public DateTime? PlannedDate { get; set; }
        public int Index { get; set; }
        public int? DestinationIndex { get; set; }
        private Operation lastOperation { get; set; }
        public string Type { get; set; } //TODO: Добавить ENUM для типа отправления
        public int UnsuccessfulDeliveryCount { get; set; }
        private Zone lastZone { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is Parcel objAsPart)) return false;
            else return Equals(objAsPart);
        }

        public bool Equals(Parcel parcel)
        {
            if (parcel == null) return false;
            return TrackID.Equals(parcel.TrackID);
        }

        public override int GetHashCode()
        {
            return 697290297 + EqualityComparer<string>.Default.GetHashCode(TrackID);
        }

        public string LastOperation
        {
            get
            {
                switch (lastOperation)
                {
                    case Operation.Registration:
                        return "Регистрация";
                    case Operation.Accept:
                        return "Прием в зону";
                    case Operation.PlannigDelivery:
                        return "Плановая доставка";
                    case Operation.Transfer:
                        return "Передача в зону";
                    case Operation.UnluckyTry:
                        return "Неудачная попытка вручения";
                    case Operation.ToCourier:
                        return "Передача курьеру";
                    case Operation.Bag:
                        return "Приписка к емкости";
                    case Operation.Document:
                        return "Приписка к документу";
                    case Operation.Warehouse:
                        return "Хранение";
                }
                return null;
            }
            set
            {
                value = value.ToLowerInvariant();
                if (value.Equals("регистрация"))
                    lastOperation = Operation.Registration;
                else if (value.Equals("передача курьеру"))
                    lastOperation = Operation.ToCourier;
                else if (value.Equals("неудачная попытка вручения"))
                    lastOperation = Operation.UnluckyTry;
                else if (value.Equals("прием"))
                    lastOperation = Operation.Accept;
                else if (value.Equals("передача"))
                    lastOperation = Operation.Transfer;
                else if (value.Equals("плановая доставка"))
                    lastOperation = Operation.PlannigDelivery;
                else if (value.Equals("приписка к документу"))
                    lastOperation = Operation.Document;
                else if (value.Equals("приписка к емкости"))
                    lastOperation = Operation.Bag;
                else
                    lastOperation = Operation.Warehouse;
            }
        }

        public string LastZone
        {
            get
            {
                switch (lastZone)
                {
                    case Zone.Warehouse:
                        return "УКД Кладовая Хранения";
                    case Zone.DetailSort:
                        return "УКД Детальная сортировка";
                    case Zone.Delivery:
                        return "УКД Доставка";
                    case Zone.Registration:
                        return "УКД Регистрация";
                    default:
                        return null;
                }
            }
            set
            {
                value = value.ToLowerInvariant();
                if (value.Contains("регистрация"))
                    lastZone = Zone.Registration;
                else if (value.Contains("доставка"))
                    lastZone = Zone.Delivery;
                else if (value.Contains("кладовая"))
                    lastZone = Zone.Warehouse;
                else
                    lastZone = Zone.DetailSort;
            }
        }

        public override string ToString()
        {
            return TrackID;
        }
    }
}