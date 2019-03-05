
using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

using Util;
public class TestaConexao
{
    public static void Main(string[] args)
    {
        try
        {
            using (MySqlConnection connection = new DBConnect().Connection)
            {
                connection.Open();

                MessageBox.Show("Successfully connected!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            throw new Exception(ex.Message);
        }
    }
}