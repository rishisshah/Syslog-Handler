using System;
using System.IO;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Data.SqlClient;

namespace SyslogMover
{
    internal class SyslogMover
    {
        string _sourceFolder, _archivedFolder, _incomingLogsFileName, _databaseTableName, _applicationErrorNotifEmail, _emailFromUsername, 
               _emailFromPassword, _emailExceptionFromUsername, _emailExceptionFromPassword; 

        public SyslogMover()
        {
            _sourceFolder = ConfigurationManager.AppSettings["SourceFolder"];
            _archivedFolder = ConfigurationManager.AppSettings["ArchivedFolder"];
            _incomingLogsFileName = ConfigurationManager.AppSettings["IncomingLogsFileName"];
            _databaseTableName = ConfigurationManager.AppSettings["DatabaseTableName"];
            _applicationErrorNotifEmail = ConfigurationManager.AppSettings["ApplicationErrorNotifEmail"];
            _emailFromUsername = ConfigurationManager.AppSettings["EmailFromUsername"];
            _emailFromPassword = ConfigurationManager.AppSettings["EmailFromPassword"];
            _emailExceptionFromUsername = ConfigurationManager.AppSettings["EmailExceptionFromUsername"];
            _emailExceptionFromPassword = ConfigurationManager.AppSettings["EmailExceptionFromPassword"];
            if (string.IsNullOrEmpty(_sourceFolder) || string.IsNullOrEmpty(_archivedFolder) || string.IsNullOrEmpty(_incomingLogsFileName) || string.IsNullOrEmpty(_databaseTableName) ||
                string.IsNullOrEmpty(_applicationErrorNotifEmail) || string.IsNullOrEmpty(_emailFromUsername) || string.IsNullOrEmpty(_emailFromPassword) || string.IsNullOrEmpty(_emailExceptionFromUsername) ||
                string.IsNullOrEmpty(_emailExceptionFromPassword))
            {
                sendEmailTo(_applicationErrorNotifEmail, "Important Error", "There was an error in the logging application. The error occured while retrieving data from the config file. " +
                                                          "Ensure all values in the config file are filled.");
                Environment.Exit(0);
            }
        }

        public void MoveSyslogs()
        {
            FileInfo[] Files = null;

            try
            {
                DirectoryInfo d = new DirectoryInfo(_sourceFolder);
                Files = d.GetFiles("*");
            }
            catch (Exception e)
            {
                sendEmailTo(_applicationErrorNotifEmail, "Important Error", "There was an error in the logging application. The error occured while retrieving logs from the specified source folder. " +
                                                                          "Ensure the correct source folder is provided in the config file " + $"Please review the error below and take appropriate action.\n\n{e}");
                Environment.Exit(0);
            }

            try
            {
                foreach (FileInfo file in Files)
                {
                    if (file.Name != _incomingLogsFileName)
                    {
                        using (StreamReader sr = new StreamReader(_sourceFolder + file.Name))
                        {
                            while (!sr.EndOfStream)
                            {

                                String line = sr.ReadLine();
                                var syslog = new SyslogInfo(line);

                                using (SqlConnection conn = new SqlConnection())
                                {
                                    conn.ConnectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=L3NetworksLogs;Data Source=WIN-CGJDI45BSJA\SQLEXPRESS";
                                    conn.Open();

                                    var cmdStr = @"INSERT INTO Syslogs (IPAddress, Timestamp, Hostname, Facility, Priority, AccessType, Message, Analyzed) " +
                                                    "VALUES (@IP, @Timestamp, @Hostname, @Facility, @Priority, @AccessType, @Message, @Analyzed)";

                                    var insertCmd = new SqlCommand(cmdStr, conn);
                                    insertCmd.Parameters.AddWithValue("@IP", syslog.Ip);
                                    insertCmd.Parameters.AddWithValue("@Timestamp", syslog.TimeStamp);
                                    insertCmd.Parameters.AddWithValue("@Hostname", syslog.Hostname);
                                    insertCmd.Parameters.AddWithValue("@Facility", syslog.Facility);
                                    insertCmd.Parameters.AddWithValue("@Priority", syslog.Priority);
                                    insertCmd.Parameters.AddWithValue("@AccessType", syslog.AccessType);
                                    insertCmd.Parameters.AddWithValue("@Message", syslog.Message);
                                    insertCmd.Parameters.AddWithValue("@Analyzed", "0");
                                    insertCmd.ExecuteNonQuery();
                                    conn.Close();
                                }                                
                            }
                        }
                        System.IO.File.Move(_sourceFolder + file.Name, _archivedFolder + file.Name);
                    }
                }
            }
            catch (Exception e)
            {
                sendEmailTo(_applicationErrorNotifEmail, "Important Error", "There was an error in the logging application. The error occured while reading the files in the source folder or while moving the syslogs to the database. " +
                                                                          "Ensure the source folder contains the correct log files and the database credentials are correct in the config file." + $"Please review the error below and take appropriate action.\n\n{e}");
                Environment.Exit(0);
            }
        }

        public void sendEmailTo(string ReciverMail, string emailSubject, string emailBody)
        {
            MailMessage msg = new MailMessage();

            msg.From = new MailAddress(_emailFromUsername);
            msg.To.Add(ReciverMail);
            msg.Subject = emailSubject;
            msg.Body = emailBody;
            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = true;
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.EnableSsl = true;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new NetworkCredential(_emailFromUsername, _emailFromPassword);
            client.Timeout = 20000;
            try
            {
                client.Send(msg);
            }
            catch (Exception ex)
            {
                msg.From = new MailAddress(_emailExceptionFromUsername);
                msg.To.Add(ReciverMail + "," + _emailFromUsername);
                msg.Subject = "Important Error";
                msg.Body = $"There was an error sending an email with the given email credentials. Ensure the credentials are correct in the config file and that the email allows less secure apps. More information about this error is below\n\n{ex}";
                SmtpClient clientE = new SmtpClient();
                client.UseDefaultCredentials = true;
                clientE.Host = "smtp.gmail.com";
                clientE.Port = 587;
                clientE.EnableSsl = true;
                clientE.DeliveryMethod = SmtpDeliveryMethod.Network;
                clientE.Credentials = new NetworkCredential(_emailExceptionFromUsername, _emailExceptionFromPassword);
                clientE.Timeout = 20000;
            }
            finally
            {
                msg.Dispose();
            }
        }
    }
}
