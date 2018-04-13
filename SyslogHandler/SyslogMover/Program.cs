
namespace SyslogMover
{

    class Program
    {
        //static string _databaseName = ConfigurationManager.AppSettings["DatabaseName"];

        static void Main(string[] args)
        {
            new SyslogMover().MoveSyslogs();
        }
    }
}
