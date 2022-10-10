using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Collections.ObjectModel;
using Npgsql;
using System.Linq;

// https://www.dotnetperls.com/serialize-list
// https://www.daveoncsharp.com/2009/07/xml-serialization-of-collections/


namespace Lab2Solution
{

    /// <summary>
    /// This is the database class, currently a Relational Database
    /// </summary>
    public class RelationalDatabase : IDatabase
    {


        String connectionString;
        /// <summary>
        /// A local version of the database, we *might* want to keep this in the code and merely
        /// adjust it whenever Add(), Delete() or Edit() is called
        /// Alternatively, we could delete this, meaning we will reach out to bit.io for everything
        /// What are the costs of that?
        /// There are always tradeoffs in software engineering.
        /// </summary>
        ObservableCollection<Entry> entries = new ObservableCollection<Entry>();

        JsonSerializerOptions options;


        /// <summary>
        /// Here or thereabouts initialize a connectionString that will be used in all the SQL calls
        /// </summary>
        public RelationalDatabase()
        {
            connectionString = InitializeConnectionString();
        }


        /// <summary>
        /// Adds an entry to the database
        /// </summary>
        /// <param name="entry">the entry to add</param>
        public void AddEntry(Entry entry)
        {
            try
            {
                entries.Add(entry);

                // write the SQL to INSERT entry into bit.io
                using var con = new NpgsqlConnection(connectionString);

                con.Open();
                var sql = "INSERT INTO Entries (clue, answer, difficulty, date, id) VALUES(@clue, @answer, @difficulty, @date, @id) ";

                using var cmd = new NpgsqlCommand(sql, con);

                cmd.Parameters.AddWithValue("id", entry.Id);
                cmd.Parameters.AddWithValue("clue", entry.Clue);
                cmd.Parameters.AddWithValue("answer", entry.Answer);
                cmd.Parameters.AddWithValue("difficulty", entry.Difficulty);
                cmd.Parameters.AddWithValue("date", entry.Date);

                int rowsInserted = cmd.ExecuteNonQuery();

                Console.WriteLine("The number of rows inserted was {0}", rowsInserted);
                con.Close();

               

            }
            catch (IOException ioe)
            {
                Console.WriteLine("Error while adding entry: {0}", ioe);
            }
        }
        /// <summary>
        /// Get the maximum id from the current list of entries
        /// This function is used for helping AddEntry()
        /// </summary>
        /// <returns></returns>
        public int GetNextId()
        {
            using var con = new NpgsqlConnection(connectionString);
            con.Open();
            var sql = "SELECT MAX(id) FROM Entries;";
            using var cmd = new NpgsqlCommand(sql, con);
            int id = (Int32)cmd.ExecuteScalar();
            con.Close();
            return id;
        }

        /// <summary>
        /// Finds a specific entry
        /// </summary>
        /// <param name="id">id to find</param>
        /// <returns>the Entry (if available)</returns>
        public Entry FindEntry(int id)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Deletes an entry 
        /// </summary>
        /// 
        /// <param name="entry">An entry, which is presumed to exist</param>
        /// <resturns> true if successfully deleted. Otherwise, false</resturns>
        public bool DeleteEntry(Entry entry)
        {
            try
            {
                var result = entries.Remove(entry);


                // Write the SQL to DELETE entry from bit.io. You have its id, that should be all that you need
                using var con = new NpgsqlConnection(connectionString);

                con.Open();
                var sql = "DELETE FROM Entries WHERE id = @id";

                using var cmd = new NpgsqlCommand(sql, con);

                cmd.Parameters.AddWithValue("id", entry.Id);

                int rowsDeleted = cmd.ExecuteNonQuery();

                Console.WriteLine("The number of rows inserted was {0}", rowsDeleted);
                con.Close();


                return true;
            }
            catch (IOException ioe)
            {
                Console.WriteLine("Error while deleting entry: {0}", ioe);
            }
            return false;
        }

        /// <summary>
        /// Edits an entry
        /// </summary>
        /// <param name="replacementEntry"></param>
        /// <returns>true if editing was successful. Otherwise, false</returns>
        public bool EditEntry(Entry replacementEntry)
        {

            foreach (Entry entry in entries) // iterate through entries until we find the Entry in question
            {
                if (entry.Id == replacementEntry.Id) // found it
                {
                    entry.Answer = replacementEntry.Answer;
                    entry.Clue = replacementEntry.Clue;
                    entry.Difficulty = replacementEntry.Difficulty;
                    entry.Date = replacementEntry.Date;

                    try
                    {
                        using var con = new NpgsqlConnection(connectionString);

                        con.Open();
                        var sql = "UPDATE Entries SET clue = @clue, answer = @answer, difficulty = @difficulty, date = @date WHERE id = @id";

                        using var cmd = new NpgsqlCommand(sql, con);

                        cmd.Parameters.AddWithValue("id", entry.Id);
                        cmd.Parameters.AddWithValue("clue", entry.Clue);
                        cmd.Parameters.AddWithValue("answer", entry.Answer);
                        cmd.Parameters.AddWithValue("difficulty", entry.Difficulty);
                        cmd.Parameters.AddWithValue("date", entry.Date);

                        int rowsEdited = cmd.ExecuteNonQuery();

                        Console.WriteLine("The number of rows edited was {0}", rowsEdited);
                        con.Close();

                        return true;
                    }
                    catch (IOException ioe)
                    {
                        Console.WriteLine("Error while replacing entry: {0}", ioe);
                    }

                }
            }
            return false;
        }



        /// <summary>
        /// Retrieves all the entries
        /// </summary>
        /// <returns>all of the entries</returns>
        public ObservableCollection<Entry> GetEntries()
        {
            while (entries.Count > 0)
            {
                entries.RemoveAt(0);
            }

            using var con = new NpgsqlConnection(connectionString);
            con.Open();

            var sql = "SELECT * FROM \"entries\" limit 10;";

            using var cmd = new NpgsqlCommand(sql, con);

            using NpgsqlDataReader reader = cmd.ExecuteReader();

            // Columns are clue, answer, difficulty, date, id in that order ...
            // Show all data
            while (reader.Read())
            {
                for (int colNum = 0; colNum < reader.FieldCount; colNum++)
                {
                    Console.Write(reader.GetName(colNum) + "=" + reader[colNum] + " ");
                }
                Console.Write("\n");
                entries.Add(new Entry(reader[0] as String, reader[1] as String, (int)reader[2], reader[3] as String, (int)reader[4]));
            }

            con.Close();

            return entries;
        }

        /// <summary>
        /// Creates the connection string to be utilized throughout the program
        /// </summary>
        /// <returns> The string value of host, username, password, and database </returns>
        public String InitializeConnectionString()
        {
            var bitHost = "db.bit.io";
            var bitApiKey = "v2_3ugJk_UKeLe28h3QTPZRru7r4WXcc"; // from the "Password" field of the "Connect" menu

            var bitUser = "karrin07";
            var bitDbName = "karrin07/CrosswordDB";

            return connectionString = $"Host={bitHost};Username={bitUser};Password={bitApiKey};Database={bitDbName}";
        }

        public bool SortByClue()
        {
            try
            {
                while (entries.Count > 0)
                {
                    entries.RemoveAt(0);
                }

                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                var sql = "SELECT * FROM \"entries\" ORDER BY clue;";
                using var cmd = new NpgsqlCommand(sql, con);
                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    for (int colNum = 0; colNum < reader.FieldCount; colNum++)
                    {
                        Console.Write(reader.GetName(colNum) + "=" + reader[colNum] + " ");
                    }
                    Console.Write("\n");
                    entries.Add(new Entry(reader[0] as String, reader[1] as String, (int)reader[2], reader[3]
                                           as String, (int)reader[4]));
                }
             
                con.Close();
                return true;

            }
            catch (IOException ioe)
            {
                Console.WriteLine("Error occurred during sorting entry by clue : {0}", ioe);
            }
            return false;
        }

        public bool SortByAnswer()
        {
            try
            {
                while (entries.Count > 0)
                {
                    entries.RemoveAt(0);
                }

                using var con = new NpgsqlConnection(connectionString);
                con.Open();

                var sql = "SELECT * FROM \"entries\" ORDER BY answer;";
                using var cmd = new NpgsqlCommand(sql, con);
                using NpgsqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    for (int colNum = 0; colNum < reader.FieldCount; colNum++)
                    {
                        Console.Write(reader.GetName(colNum) + "=" + reader[colNum] + " ");
                    }
                    Console.Write("\n");
                    entries.Add(new Entry(reader[0] as String, reader[1] as String, (int)reader[2], reader[3]
                                           as String, (int)reader[4]));
                }

                con.Close();
                return true;

            }
            catch (IOException ioe)
            {
                Console.WriteLine("Error occurred during sorting entry by answer : {0}", ioe);
            }
            return false;
        }
    }
}