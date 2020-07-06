using System.Collections.Generic;
using System.Data.SQLite;

namespace CourierServiceAssistant
{
    public class DBManager
    {
        private readonly SQLiteConnection connection;
        private readonly SQLiteCommand command;
        public DBManager(string dataBaseFilePath)
        {
            var connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = dataBaseFilePath
            };
            connection = new SQLiteConnection(connectionString.ConnectionString);
            command = new SQLiteCommand(connection);
        }
        public SQLiteDataReader ExecuteReader(string commandText)
        {
            CloseConnection();
            connection.Open();
            command.CommandText = commandText;
            SQLiteDataReader reader = command.ExecuteReader();
            return reader;
        }
        public SQLiteDataReader ExecuteReader(SQLiteCommand command)
        {
            CloseConnection();
            connection.Open();
            SQLiteDataReader reader = command.ExecuteReader();
            return reader;
        }
        public int ExecuteNonQuery(string commandText)
        {
            CloseConnection();
            connection.Open();
            command.CommandText = commandText;
            return command.ExecuteNonQuery();
        }
        public int ExecuteNonQuery(SQLiteCommand command)
        {
            CloseConnection();
            connection.Open();
            command.Connection = this.command.Connection;
            return command.ExecuteNonQuery();
        }
        private void CloseConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
        }

        public void TransactionDeleteFromParcel(List<Parcel> list)
        {
            CloseConnection();
            connection.Open();
            SQLiteTransaction transaction;
            transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            foreach (Parcel parcel in list)
            {
                command.CommandText = $"DELETE FROM Parcels WHERE TrackNumber = ('{parcel.TrackID}')";
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        public void TransactionInsertToParcel(List<string> sb)
        {
            CloseConnection();
            connection.Open();
            SQLiteTransaction transaction;
            transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            foreach (var value in sb)
            {
                command.CommandText = $"INSERT INTO Parcels(TrackNumber, RegistrationTime, PlannedDate, 'Index', UnsuccessfulDeliveryCount, DestinationIndex, LastOperation, Address, Category, Name, IsPayNeed, Telephone, Type, Zone, DateOfAdded) VALUES {value};";
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        public void TransactionInsertToDelivered(List<string> sb)
        {
            CloseConnection();
            connection.Open();
            SQLiteTransaction transaction;
            transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            foreach (var value in sb)
            {
                command.CommandText = $"INSERT INTO GoneByReport(TrackNumber, RegistrationTime, PlannedDate, 'Index', UnsuccessfulDeliveryCount, DestinationIndex, LastOperation, Address, Category, Name, IsPayNeed, Telephone, Type, Zone, DateOfReportGone) VALUES {value};";
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
}
