using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;

namespace Didago.Tools.SSOConfigCmdTool
{
    /// <summary>
    /// Author  : Jean-Paul Smit - Didago IT Consultancy
    /// Date    : July 18, 2012
    /// 
    /// This tool is used as the commandline equivalent of the MMC snap-in to import an SSO application
    /// It is capable of import, export, delete, list and show details of SSO applications
    /// The MMC snap-in can be found here: http://www.microsoft.com/en-us/download/details.aspx?id=14524
    /// </summary>
   
    class Program
    {
        private static string _ssoAction = string.Empty;
        private static string _appName = string.Empty;
        private static string _encryptedFile = string.Empty;
        private static string _encryptedFileKey = string.Empty;
        private static string _databaseServer = string.Empty;
        private static string _databaseName = string.Empty;
        private static string _company = "YourCompany";

        static void Main(string[] args)
        {
            ConsoleColor currentColor = Console.ForegroundColor;

            if (CommandLineParser.ParseArguments(args))
            {
                _ssoAction = CommandLineParser.Action;
                _databaseServer = CommandLineParser.DbServer;
                _databaseName = CommandLineParser.DbName;
                _appName = CommandLineParser.AppName;
                _encryptedFileKey = CommandLineParser.Key;
                _encryptedFile = CommandLineParser.File;

                switch (_ssoAction)
                {
                    case "import":
                        if (ImportParamsValid())
                        {
                            ImportSSOApp();
                            Console.ForegroundColor = currentColor;
                            Console.WriteLine("Finished importing SSO application.");
                        }
                        break;
                    case "export":
                        if (ExportParamsValid())
                        {
                            ExportSSOApp();
                            Console.ForegroundColor = currentColor;
                            Console.WriteLine("Finished exporting SSO application.");
                        }
                        break;
                    case "delete":
                        if (DeleteParamsValid())
                        {
                            DeleteSSOApp();
                            Console.ForegroundColor = currentColor;
                            Console.WriteLine("Application {0} has been removed", _appName);
                        }
                        break;
                    case "list":
                        ListSSOApps();
                        break;
                    case "detail":
                        if (DetailParamsValid())
                        {
                            DetailSSOApp();
                        }
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unknown action, use import, export, delete, list or detail");
                        Console.ForegroundColor = currentColor;
                        return;
                };
            }
        }

        #region Validate Parameters

        /// <summary>
        /// Validation of commandline parameters for import action
        /// Necessary are:
        /// - AppName + app may not yet exist
        /// - Key
        /// - Filename
        /// </summary>
        private static bool ImportParamsValid()
        {
            StringBuilder validationErrors = new StringBuilder();

            ValidateAppName(validationErrors);
            ValidateAppNameExists(validationErrors, false);
            ValidateKey(validationErrors);
            _encryptedFile = ValidateFile(validationErrors, true);

            if (validationErrors.Length > 0)
            {
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start the import process for the SSO application because of: ");
                Console.WriteLine(validationErrors.ToString());
                Console.ForegroundColor = currentColor;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validation of commandline parameters for export action
        /// Necessary are:
        /// - AppName + needs to exist
        /// - Key
        /// - Filename
        /// </summary>
        private static bool ExportParamsValid()
        {
            StringBuilder validationErrors = new StringBuilder();

            ValidateAppName(validationErrors);
            ValidateAppNameExists(validationErrors, true);
            ValidateKey(validationErrors);
            _encryptedFile = ValidateFile(validationErrors, false);

            if (validationErrors.Length > 0)
            {
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start the export process for the SSO application because of: ");
                Console.WriteLine(validationErrors.ToString());
                Console.ForegroundColor = currentColor;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validation of commandline parameters for detail action
        /// Necessary are:
        /// - AppName
        /// </summary>
        private static bool DetailParamsValid()
        {
            StringBuilder validationErrors = new StringBuilder();

            ValidateAppName(validationErrors);
            ValidateAppNameExists(validationErrors, true);

            if (validationErrors.Length > 0)
            {
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to retrieve details of the SSO application because of: ");
                Console.WriteLine(validationErrors.ToString());
                Console.ForegroundColor = currentColor;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validation of commandline parameters for delete action
        /// Necessary are:
        /// - AppName
        /// </summary>
        private static bool DeleteParamsValid()
        {
            StringBuilder validationErrors = new StringBuilder();

            ValidateAppName(validationErrors);

            if (validationErrors.Length > 0)
            {
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to delete the SSO application because of: ");
                Console.WriteLine(validationErrors.ToString());
                Console.ForegroundColor = currentColor;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if SSO application name has been supplied
        /// </summary>
        /// <param name="errors">Add error description to stringbuilder object if validation failed</param>
        private static void ValidateAppName(StringBuilder errors)
        {
            if (string.IsNullOrEmpty(_appName))
            {
                errors.Append("- SSO application name missing." + Environment.NewLine);
            }
        }

        /// <summary>
        /// Check if SSO application exists
        /// </summary>
        /// <param name="errors">Add error description to stringbuilder object if validation failed</param>
        /// <param name="mustExist">True if SSO application should exists, false if SSO application shouldn't exist</param>
        private static void ValidateAppNameExists(StringBuilder errors, bool mustExist)
        {
            string[] appList = GetSSOAppList();
            if (mustExist)
            {
                if (appList.ToList().FindIndex(x => x.Equals(_appName, StringComparison.OrdinalIgnoreCase)) == -1)
                {
                    errors.Append("- SSO application name doesn't exist." + Environment.NewLine);
                    return;
                }
            }
            else
            {
                if (appList.ToList().FindIndex(x => x.Equals(_appName, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    errors.Append("-SSO application name already exists." + Environment.NewLine);
                    return;
                }
            }
        }

        /// <summary>
        /// Check if encryption/decryption key has been supplied
        /// </summary>
        /// <param name="errors">Add error description to stringbuilder object if validation failed</param>
        private static void ValidateKey(StringBuilder errors)
        {
            if (string.IsNullOrEmpty(_encryptedFileKey))
            {
                errors.Append("- Encryption key missing." + Environment.NewLine);
            }
        }

        /// <summary>
        /// Check if a file has been supplied, and if it exists if requested or doesn't if it should
        /// </summary>
        /// <param name="errors">Add error description to stringbuilder object if validation failed</param>
        /// <returns>Full path to file to use</returns>
        private static string ValidateFile(StringBuilder errors, bool mustExist)
        {
            if (string.IsNullOrEmpty(_encryptedFile))
            {
                errors.Append("- No file supplied." + Environment.NewLine);
                return string.Empty;
            }
            FileInfo fi = new FileInfo(_encryptedFile);
            if (!fi.Exists)
            {
                if (mustExist)
                {
                    errors.Append("- Failed to find file." + Environment.NewLine);
                    return string.Empty;
                }
                else
                {
                    if (!_encryptedFile.EndsWith(".sso"))
                    {
                        _encryptedFile += ".sso";
                    }
                    return _encryptedFile;
                }
            }
            else
            {
                if (!mustExist)
                {
                    errors.Append("- File to export to already exists." + Environment.NewLine);
                    return _encryptedFile;
                }
                return fi.FullName;
            }
        }
        #endregion

        /// <summary>
        /// Export specified SSO application with specified key to specified file
        /// This code is borrowed from the MMC Snap-in for SSO
        /// </summary>
        private static void ExportSSOApp()
        {
            SSO sSO = new SSO(_databaseServer, _databaseName, _company);
            string[] keys = sSO.GetKeys(_appName);
            string[] values = sSO.GetValues(_appName);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?><SSOApplicationExport><applicationData>");
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] != null && !(keys[i] == ""))
                {
                    stringBuilder.Append(string.Concat(new string[]
			{
				"<add key=\"",
				keys[i],
				"\" value=\"",
				values[i],
				"\" />"
			}));
                }
            }
            stringBuilder.Append("</applicationData></SSOApplicationExport>");
            StreamWriter streamWriter = new StreamWriter(_encryptedFile, false);
            try
            {
                streamWriter.Write(sSO.Encrypt(stringBuilder.ToString(), _encryptedFileKey));
                streamWriter.Flush();
            }
            catch (Exception ex)
            {
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in export process: " + ex);
                Console.ForegroundColor = currentColor;
                return;
            }
            finally
            {
                streamWriter.Close();
                streamWriter.Dispose();
            }
        }

        /// <summary>
        /// Display the keys + values for the specified SSO application
        /// </summary>
        private static void DetailSSOApp()
        {
            Console.WriteLine("Details of SSO application {0}", _appName);
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            SSO sSO = new SSO(_databaseServer, _databaseName, _company);
            string[] keys = sSO.GetKeys(_appName);
            string[] values = sSO.GetValues(_appName);
            for (int i = 0; i < keys.Length; i++)
            {
                Console.WriteLine("{0}\t - {1}", keys[i], values[i]);
            }
            Console.ForegroundColor = currentColor;
            Console.WriteLine("** End of details of SSO Application");
        }

        /// <summary>
        /// Display all SSO applications found in the store
        /// </summary>
        private static void ListSSOApps()
        {
            Console.WriteLine("** SSO applications on {0} in {1} ** ", _databaseServer, _databaseName);
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            
            foreach (string item in GetSSOAppList())
            {
                Console.WriteLine(item);
            }
            Console.ForegroundColor = currentColor;
            Console.WriteLine("** End of SSO Application list");
        }

        /// <summary>
        /// Get an array of all SSO applications in the store
        /// </summary>
        /// <returns></returns>
        private static string[] GetSSOAppList()
        {
            SSO sSO = new SSO(_databaseServer, _databaseName, _company);
            return sSO.GetApplications();
        }

        /// <summary>
        /// Delete a specified SSO application from the store
        /// </summary>
        private static void DeleteSSOApp()
        {
            SSO sSO = new SSO(_databaseServer, _databaseName, _company);
            sSO.DeleteApplication(_appName);
        }

        /// <summary>
        /// Import an SSO application with specified name, key and file
        /// This code is borrowed from the MMC Snap-in for SSO
        /// </summary>
        private static void ImportSSOApp()
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            bool flag = true;
            XmlDocument xmlDocument = new XmlDocument();
            string text = string.Empty;
            string toDecrypt = string.Empty;
            try
            {
                SSO sSO = new SSO(_databaseServer, _databaseName, _company);
                StreamReader streamReader = new StreamReader(_encryptedFile);
                toDecrypt = streamReader.ReadToEnd();
                streamReader.Dispose();
                text = _appName;
                string[] applications = sSO.GetApplications();
                for (int i = 0; i < applications.Length; i++)
                {
                    if (applications[i].ToUpper() == text.ToUpper())
                    {
                        flag = false;
                    }
                }
                byte[] bytes;
                try
                {
                    bytes = Encoding.ASCII.GetBytes(sSO.Decrypt(toDecrypt, _encryptedFileKey));
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error while decrypting the file to import, probably cause is the file hasn't been encrypted with the supplied key (error: " + exception.Message + ")");
                    Console.ForegroundColor = currentColor;
                    return;
                }
                MemoryStream memoryStream = new MemoryStream(bytes);
                try
                {
                    xmlDocument.Load(memoryStream);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error in import process: " + exception);
                    Console.ForegroundColor = currentColor;
                    return;
                }
                finally
                {
                    memoryStream.Dispose();
                }
                XmlElement documentElement = xmlDocument.DocumentElement;
                XmlNodeList xmlNodeList = documentElement.SelectNodes("applicationData/add");
                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                if (!flag)
                {
                    list.AddRange(sSO.GetKeys(text));
                    list2.AddRange(sSO.GetValues(text));
                }
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    string value = xmlNode.SelectSingleNode("@key").Value;
                    string value2 = xmlNode.SelectSingleNode("@value").Value;
                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(value2))
                    {
                        if (!list.Contains(value))
                        {
                            list.Add(value);
                            list2.Add(value2);
                        }
                    }
                }
                sSO.CreateApplicationFieldsValues(text, list.ToArray(), list2.ToArray());
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error in import process: " + exception.Message);
                Console.ForegroundColor = currentColor;
            }
        }
    }
}
