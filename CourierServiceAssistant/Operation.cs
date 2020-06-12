namespace CourierServiceAssistant
{
    public enum Operation
    {
        Registration,       //Регистрация
        Accept,             //Прием
        PlannigDelivery,    //Плановая доставка
        Transfer,           //Передача
        UnluckyTry,         //Неудачная попытка вручения
        ToCourier,          //Передача курьеру
        Bag,                //Приписка к мешку
        Document,           //Приписка к документу
        Warehouse           //Хранение
    }
}