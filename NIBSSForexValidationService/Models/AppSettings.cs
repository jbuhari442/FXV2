using System;
namespace NIBSSForexValidationService.Models
{
    public class AppSettings
    {
        public AppSettings()
        {
        }
        public string Secret { get; set; }
        public string AppName { get; set; }
    }


    public class ConnectionStrings
    {

        public string BVNMSSQLDbConnectionString { get; set; }

        public string FXMySQLDBConnectionString { get; set; }

        public string FXMSSQLDbConnectionString { get; set; }
       
    }


}
