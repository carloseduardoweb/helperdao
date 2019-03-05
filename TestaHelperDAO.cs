
using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Util;
using Model;

public class TestaHelperDAO
{
    public static void Main(string[] args)
    {
        Person person = new Person()
        {
            name = "Carlos Eduardo",            
        };

        try
        {
            InsertTradicional(person);
            InsertComHelper(person);

            DeleteTradicional(person);
            DeleteComHelper(person);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            throw new Exception(ex.Message);
        }
        
    }

    private static void InsertTradicional(Person person)
    {
        using (MySqlConnection connection = new DBConnect().Connection)
        {
            string sql = "INSERT INTO people (name) VALUES ('" + person.name + "')";

            using (MySqlCommand command = new MySqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    private static void InsertComHelper(Person person)
    {
        HelperDAO<Person>.SqlBuilder sqlBuilder = delegate(HelperDAO<Person> helper)
        {
            return "INSERT INTO people (name) VALUES ('" + helper.data.name + "')";
        };

        new HelperDAO<Person>(person).Insert(sqlBuilder);
    }

    private static void DeleteTradicional(Person person)
    {
        using (MySqlConnection connection = new DBConnect().Connection)
        {
            string sql = "DELETE FROM people"; // Insecure!

            using (MySqlCommand command = new MySqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    private static void DeleteComHelper(Person person)
    {
        HelperDAO<Person>.SqlBuilder sqlBuilder = delegate(HelperDAO<Person> helper)
        {
            return "DELETE FROM people";
        };

        new HelperDAO<Person>(person).Delete(sqlBuilder, secureMode: false); // Optional argument defaults to true. Only allows insecure deletion explicitly.
    }
}
