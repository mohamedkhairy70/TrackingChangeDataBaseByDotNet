using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrackingChangeDataBaseWithDotNet
{
    public partial class Form1 : Form
    {
        string dataContext = $@"Server= .; Database=TestDB;Integrated Security=true";
        SqlConnection sqlConnection;
        SqlCommand sqlCommand;

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Remark: Run "ALTER DATABASE TestDB SET ENABLE_BROKER" from "Microsoft SQL Server Management Studio" once, before executing this code for the first time.
            // optional LINQ-to-SQL data context.
            sqlConnection = new SqlConnection(dataContext);
            sqlConnection.Open();
            SqlDependency.Start(dataContext);
            sqlCommand = new SqlCommand("SELECT Name FROM dbo.Table1", sqlConnection); // "dbo" is required for SqlDependency: http://stackoverflow.com/questions/7946885/sqldependency-notification-immediate-failure-notification-after-executing-quer
                                                                                       // The SQL command must meet special conditions: https://msdn.microsoft.com/en-us/library/aewzkxxh.aspx
            SqlDependency sqlDependency = new SqlDependency(sqlCommand); // Also sets sqlCommand.Notification.
            sqlDependency.OnChange += SqlDependecy_OnChange;
            SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count > 0)
                dataGridView1.DataSource = dt;
            // SQL command must be executed AFTER "SqlDependency sqlDependency = new SqlDependency(sqlCommand)".
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SqlDependency.Stop(dataContext);
            sqlConnection.Close();
        }
        private void SqlDependecy_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => SqlDependecy_OnChange(sender, e)));
            else
            {
                // The OnChange event of a SqlDependency fires only once. When the result of the SQL command changes for the second time, the OnChange event does not fire again.
                // http://www.codeproject.com/Articles/12335/Using-SqlDependency-for-data-change-events
                // https://msdn.microsoft.com/en-us/library/a52dhwx7.aspx
                // So, detach this event handler and create a new SqlDependency.
                SqlDependency sqlDependency = (SqlDependency)sender;
                sqlDependency.OnChange -= SqlDependecy_OnChange;
                sqlCommand.Notification = null; // Make sqlCommand forget sqlDependency. Otherwise, the next line of code throws an exception.
                sqlDependency = new SqlDependency(sqlCommand);
                sqlDependency.OnChange += SqlDependecy_OnChange;


                SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count > 0)
                    dataGridView1.DataSource = dt;
                // Execute the SQL command again AFTER "SqlDependency sqlDependency = new SqlDependency(sqlCommand)".
                //label1.Text = sqlCommand.ExecuteScalar().ToString();
            }
        }
    }
}
