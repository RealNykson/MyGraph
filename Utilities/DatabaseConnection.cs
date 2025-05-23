using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MyGraph.Utilities
{
  public struct ProcessUnit
  {
    public int UnitId { get; set; }
    public string UnitName { get; set; }
    public int ProcessCellLink { get; set; }
    public string ProcessCellName { get; set; }
  }

  public struct ConnectionDB
  {
    public int TransferUnitId { get; set; }
    public int SourceUnitId { get; set; }
    public int DestinationUnitId { get; set; }
  }

  public class DatabaseConnection
  {
    private string _connectionString;
    private SqlConnection _connection;

    public DatabaseConnection()
    {
      LoadConnectionString();
    }

    private void LoadConnectionString()
    {
      try
      {
        string envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, ".env");
        if (!File.Exists(envPath))
        {
          throw new FileNotFoundException("Environment file (.env) not found at: " + envPath);
        }

        var envLines = File.ReadAllLines(envPath);
        _connectionString = envLines[0] ;
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to load database configuration", ex);
      }
    }

    public bool Connect()
    {
      try
      {
        _connection = new SqlConnection(_connectionString);
        _connection.Open();
        return true;
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to connect to database", ex);
      }
    }

    public void Disconnect()
    {
      if (_connection != null && _connection.State == ConnectionState.Open)
      {
        _connection.Close();
      }
    }

    public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
    {
      try
      {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
          Connect();
        }

        using (var command = new SqlCommand(query, _connection))
        {
          if (parameters != null)
          {
            foreach (var param in parameters)
            {
              command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
          }

          var dataTable = new DataTable();
          using (var adapter = new SqlDataAdapter(command))
          {
            adapter.Fill(dataTable);
          }
          return dataTable;
        }
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to execute query", ex);
      }
    }

    public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
    {
      try
      {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
          Connect();
        }

        using (var command = new SqlCommand(query, _connection))
        {
          if (parameters != null)
          {
            foreach (var param in parameters)
            {
              command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
          }

          return command.ExecuteNonQuery();
        }
      }
      catch (Exception ex)
      {
        throw new Exception("Failed to execute non-query command", ex);
      }
    }

    public List<ProcessUnit> GetProcessUnits(int processCellId)
    {

      string query = @"
                use dbIdc 
                select unit.nKey, datax.szName, unit.nProcessCellLink, x.szName
                from tblItpProcessUnit unit
                join tblCPDataX datax on datax.nKey = unit.nDataXLink
                join tblItpProcessCell cell on cell.nKey = unit.nProcessCellLink
                join tblCPDataX x on x.nKey = cell.nDataXLink";

      var parameters = new Dictionary<string, object>
            {
                { "@processCellId", processCellId }
            };

      var dataTable = ExecuteQuery(query, parameters);
      var processUnits = new List<ProcessUnit>();

      foreach (DataRow row in dataTable.Rows)
      {
        processUnits.Add(new ProcessUnit
        {
          UnitId = Convert.ToInt32(row[0]),
          UnitName = row[1].ToString(),
          ProcessCellLink = Convert.ToInt32(row[2]),
          ProcessCellName = row[3].ToString()
        });
      }

      return processUnits;
    }

    public List<ConnectionDB> GetConnections(int processCellId)
    {
      string query = @"
                use dbIdc 
                select rel.nTransferUnitLink, rel.nSourceProcessUnitLink, rel.nDestinationProcessUnitLink 
                from tblVDProcessUnitRelation rel
                join tblItpProcessUnit unit on unit.nKey = rel.nTransferUnitLink";

      var parameters = new Dictionary<string, object>
            {
                { "@processCellId", processCellId }
            };

      var dataTable = ExecuteQuery(query, parameters);
      var connections = new List<ConnectionDB>();

      foreach (DataRow row in dataTable.Rows)
      {
        connections.Add(new ConnectionDB
        {
          TransferUnitId = Convert.ToInt32(row[0]),
          SourceUnitId = Convert.ToInt32(row[1]),
          DestinationUnitId = Convert.ToInt32(row[2])
        });
      }

      return connections;
    }
  }
}